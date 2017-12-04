using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RemoteRunner.Network.WebService
{
    public class UserDiskInfo
    {
        public int user_id { get; set; }
        public List<string> data => DriveInfo.GetDrives().ToList().Select(x => x.Name).ToList();
    }
}