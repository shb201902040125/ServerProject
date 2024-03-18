using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ServerProject
{
    internal static class LobbyManager
    {
        static Dictionary<string, Lobby> _lobbys = [];
        internal static Dictionary<string, Delegate> _lobbyConstructor = [];
        static Dictionary<string,string> _externalLobbyType = [];
        internal static bool TryCreate(string[] createParamters, [MaybeNullWhen(false)] out Lobby lobby)
        {
            lock (_lobbys)
            {
                Dictionary<string, string> _params = [];
                foreach (var paramter in createParamters)
                {
                    string[] pair = paramter.Split(':');
                    if (pair.Length != 2)
                    {
                        continue;
                    }
                    _params[pair[0]] = pair[1];
                }
                if (_params.TryGetValue("ID", out string uniqueID))
                {
                    if (_lobbys.TryGetValue(uniqueID, out lobby))
                    {
                        return false;
                    }
                }
                if (!_params.TryGetValue("Type", out string lobbyType) || !_lobbyConstructor.TryGetValue(lobbyType, out var createMethod))
                {
                    lobby = null;
                    return false;
                }
                _params["ID"] = Guid.NewGuid().ToString();
                lobby = createMethod.DynamicInvoke(_params) as Lobby;
                if(lobby is not null)
                {
                    if (_lobbys.TryAdd(lobby.UniqueID, lobby))
                    {
                        lobby.Init();
                        return true;
                    }
                    return false;
                }
                return false;
            }
        }
        internal static bool TryFindLobby(string uniqueID, [MaybeNullWhen(false)] out Lobby lobby)
        {
            return _lobbys.TryGetValue(uniqueID, out lobby) && !(lobby?.IsClosed ?? true);
        }
        internal static List<string> TryLoadNewLobbyTypes(string secretKey, byte[] assemblyStream)
        {
            Assembly assembly = Assembly.Load(assemblyStream);
            List<string> successLoad = [];
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(Lobby)) && !type.IsAbstract)
                {
                    var constructor = type.GetConstructor([typeof(Dictionary<string, string>)]);
                    if (constructor != null && type.FullName != null && !_lobbyConstructor.ContainsKey(type.FullName))
                    {
                        var method = type.GetMethod("TryCreate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, [typeof(Dictionary<string, string>)]);
                        if(method == null)
                        {
                            continue;
                        }
                        if (method.ReturnType != typeof(Lobby))
                        {
                            continue;
                        }
                        _lobbyConstructor.Add(type.FullName, Delegate.CreateDelegate(typeof(Func<Dictionary<string, string>, Lobby>), method));
                        _externalLobbyType.Add(type.FullName, secretKey);
                        successLoad.Add(type.FullName);
                    }
                }
            }
            return successLoad;
        }

        internal static bool TryRemove(string secretKey, string lobbyType,[MaybeNullWhen(true)] out string failReason)
        {
            if(!_externalLobbyType.TryGetValue(lobbyType,out string secretKeyCheck))
            {
                failReason = "Lobby Type  does not exist";
                return false;
            }
            if(secretKeyCheck!=secretKey)
            {
                failReason = "SecretKey mismatch";
                return false;
            }
            _lobbyConstructor.Remove(lobbyType);
            _externalLobbyType.Remove(lobbyType);
            failReason = null;
            return true;
        }
    }
}
