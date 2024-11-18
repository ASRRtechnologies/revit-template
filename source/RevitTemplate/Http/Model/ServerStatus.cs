using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RevitTemplate.Http.Model;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ServerStatus
{
    public volatile bool Busy;
    public volatile bool ExceptionThrown;
}