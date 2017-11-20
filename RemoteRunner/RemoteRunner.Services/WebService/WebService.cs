using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace RemoteRunner.Services.WebService
{
    public class WebService
    {
        private static readonly HttpClient Client = new HttpClient();
        public WebService()
        {
            Client.BaseAddress = new Uri("http://rr-test-vlada.herokuapp.com/");
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
                var serializer = new DataContractJsonSerializer(typeof(User));
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

        public async Task<bool> SendCommandResult(string message)
        {
            string json = JsonConvert.SerializeObject(message);

            HttpResponseMessage response =
                await Client.PostAsync("register", new StringContent(json, Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        public List<string> GetUncomletedCommands()
        {
            return new List<string>() { "{ 'command': 'Console', 'params': [{ 'cmd': 'ipconfig /all' }]}", "{ 'command': 'GetCursorPosition', 'params': []}" };
        }
    }
}
