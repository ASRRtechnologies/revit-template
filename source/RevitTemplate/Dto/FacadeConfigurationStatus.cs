using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RevitTemplate.Dto;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class FacadeConfigurationStatus
{
    public string Message { get; set; }
    public double Progress { get; set; }
    public bool Finished { get; set; }
    public ConfigurationExceptionDto Exception { get; set; }
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ConfigurationExceptionDto
{
    public string Type { get; set; }
    public string Message { get; set; }
    public string StackTrace { get; set; }
}