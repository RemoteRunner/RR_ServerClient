using System.Collections;
using System.Diagnostics;
using System.Linq;

namespace RemoteRunner.Services
{
    public class UserProcessInfo
    {
        public int user_id { get; set; }

        public IList data => Process.GetProcesses().ToList()
            .Select(x => new {id = x.Id, description = x.MainWindowTitle, memory = x.PagedMemorySize64}).ToList();
    }
}