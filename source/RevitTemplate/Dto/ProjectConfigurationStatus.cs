using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RevitTemplate.Dto;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ProjectConfigurationStatus
{
    public string WorkerId { get; set; }
    public string Message { get; set; }
    public double Progress { get; set; }
    public bool Finished { get; set; }
    public double Duration { get; set; }
    public ConfigurationExceptionDto Exception { get; set; }
}