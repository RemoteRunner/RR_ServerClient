using System.Collections.Generic;

namespace RemoteRunner.Network.WebService
{
    public class User
    {
        public int id { get; set; }
        public string user_name { get; set; }
        public string password { get; set; }
        public bool notifications { get; set; }
        public IList<string> widgets { get; set; }
        public int port { get; set; }
        public string host { get; set; }
        public Role role { get; set; }
    }
}