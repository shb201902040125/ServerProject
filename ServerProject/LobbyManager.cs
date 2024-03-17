using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerProject
{
    internal static class LobbyManager
    {
        static Dictionary<string, Lobby> _lobbys = [];
        internal static Dictionary<string, Type> _lobbyConstructor = [];
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
                if (!_params.TryGetValue("Type", out string lobbyType) || !_lobbyConstructor.TryGetValue(lobbyType, out Type type))
                {
                    lobby = null;
                    return false;
                }
                if (_params.TryGetValue("ID", out string uniqueID))
                {
                    if (_lobbys.TryGetValue(uniqueID, out lobby))
                    {
                        return false;
                    }
                }
                lobby = (Lobby?)(type.GetConstructor([typeof(Dictionary<string, string>)])?.Invoke([_params]));
                return lobby is not null;
            }
        }
        internal static bool TryFindLobby(string uniqueID, [MaybeNullWhen(false)] out Lobby lobby)
        {
            return _lobbys.TryGetValue(uniqueID, out lobby) && !(lobby?.IsClosed ?? true);
        }
    }
}
