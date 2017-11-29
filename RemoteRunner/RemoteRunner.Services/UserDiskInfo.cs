using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RemoteRunner.Services
{
    public class UserDiskInfo
    {
        public int user_id { get; set; }
        public List<string> data => DriveInfo.GetDrives().ToList().Select(x => x.Name).ToList();
    }
}