using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RemoteRunner.Network.WebService
{
    public class WebService
    {
        private static readonly HttpClient Client = new HttpClient();

        public WebService()
        {
            Client.BaseAddress = new Uri("https://rr-test-vlada.herokuapp.com/api/");
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<User> Login(string username, string password)
        {
            User user = null;
            var response = await Client.GetAsync($"login?user_name={username}&password={password}");
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStringAsync();
                user = JsonConvert.DeserializeObject<User>(stream);
            }

            return user;
        }

        public async Task<bool> Register(User user)
        {
            var json = JsonConvert.SerializeObject(user);

            var response =
                await Client.PostAsync("register", new StringContent(json, Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendCommandResult(CommandResult message)
        {
            var json = JsonConvert.SerializeObject(message);

            var response =
                await Client.PostAsync("executed-command-host",
                    new StringContent(json, Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        public async Task<List<string>> GetUncomletedCommandsAsync(int userId)
        {
            JArray commands = null;
            var response = await Client.GetAsync($"host-get-list-to-execute?user_id={userId}");
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStringAsync();
                commands = JsonConvert.DeserializeObject<JArray>(stream);
            }

            var comList = new List<string>();
            foreach (var com in commands)
                comList.Add(com.ToString());

            return comList;
        }

        public async Task<bool> SendUserData(UserDiskInfo diskInfo, UserProcessInfo processInfo)
        {
            var jsonDisk = JsonConvert.SerializeObject(diskInfo);
            var responseDisk =
                await Client.PostAsync("process-list",
                    new StringContent(jsonDisk, Encoding.UTF8, "application/json"));

            var jsonProcess = JsonConvert.SerializeObject(processInfo);
            var responseProcess =
                await Client.PostAsync("disks-list",
                    new StringContent(jsonProcess, Encoding.UTF8, "application/json"));

            return responseDisk.IsSuccessStatusCode && responseProcess.IsSuccessStatusCode;
        }

        public async Task<bool> SendHostInfo(HostInfo hostInfo)
        {
            var jsonHost = JsonConvert.SerializeObject(hostInfo);
            var responseHost =
                await Client.PostAsync("host-settings",
                    new StringContent(jsonHost, Encoding.UTF8, "application/json"));

            return responseHost.IsSuccessStatusCode;
        }
    }
}