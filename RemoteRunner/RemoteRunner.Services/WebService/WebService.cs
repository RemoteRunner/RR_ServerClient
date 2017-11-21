using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace RemoteRunner.Services.WebService
{
    public class WebService
    {
        private static readonly HttpClient Client = new HttpClient();
        public WebService()
        {
            Client.BaseAddress = new Uri("https://rr-test-vlada.herokuapp.com/");
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<User> Login(string username, string password)
        {
            User user = null;
            HttpResponseMessage response = await Client.GetAsync($"login?user_name={username}&password={password}");
            if (response.IsSuccessStatusCode)
            {
                string stream = await response.Content.ReadAsStringAsync();
                user = JsonConvert.DeserializeObject<User>(stream);
            }

            return user;
        }

        public async Task<bool> Register(User user)
        {
            string json = JsonConvert.SerializeObject(user);

            HttpResponseMessage response =
                await Client.PostAsync("register", new StringContent(json, Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendCommandResult(CommandResult message)
        {
            string json = JsonConvert.SerializeObject(message);

            HttpResponseMessage response =
                await Client.PostAsync("executed-command-host", new StringContent(json, Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        public async Task<List<string>> GetUncomletedCommandsAsync(int userId)
        {
            JArray commands = null;
            HttpResponseMessage response = await Client.GetAsync($"host-get-list-to-execute?user_id={userId}");
            if (response.IsSuccessStatusCode)
            {
                string stream = await response.Content.ReadAsStringAsync();
                commands = JsonConvert.DeserializeObject<JArray> (stream);
            }

            var comList = new List<string>();
            foreach (var com in commands)
            {
                comList.Add(com.ToString());
            }

            return comList;
        }
    }
}
