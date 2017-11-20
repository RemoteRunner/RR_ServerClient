using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
namespace RemoteRunner.Services.WebService
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Role
    {
        admin,
        user
    }
}