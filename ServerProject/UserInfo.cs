using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerProject
{
    public class UserInfo
    {
        public ISocket Socket { get; private set; }
        public string? UniqueID;
        public bool Hide;
        public bool OffLine;
        public UserInfo(ISocket socket, string? uniqueID, bool hide = false, bool offLine = false)
        {
            Socket = socket;
            UniqueID = uniqueID;
            Hide = hide;
            OffLine = offLine;
        }
    }
}
