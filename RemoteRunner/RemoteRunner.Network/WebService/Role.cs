using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RemoteRunner.Network.WebService
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Role
    {
        admin,
        user
    }
}