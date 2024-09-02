using System.IO;
using ASRR.Revit.Core.Http;

namespace RevitTemplate.Service;

public class FacadeConfiguratorService
{
    private readonly HttpService _httpService;
    private readonly string _modelDestinationFolder;
    private readonly string _configurationOutputFolder;

    public FacadeConfiguratorService(HttpService httpService, string modelDestinationFolder,
        string configurationOutputFolder)
    {
        _httpService = httpService ?? new HttpService();
        _modelDestinationFolder =
            modelDestinationFolder ?? throw new ArgumentNullException(nameof(modelDestinationFolder));
        _configurationOutputFolder = configurationOutputFolder ??
                                     throw new ArgumentNullException(nameof(configurationOutputFolder));
        Directory.CreateDirectory(_modelDestinationFolder);
        Directory.CreateDirectory(_configurationOutputFolder);
    }
}