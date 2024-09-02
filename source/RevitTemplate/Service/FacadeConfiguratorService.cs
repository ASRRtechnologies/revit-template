using System.IO;
using ASRR.Revit.Core.Http;
using Autodesk.Revit.UI;
using RevitTemplate.Dto;

namespace RevitTemplate.Service;

public class FacadeConfiguratorService
{
    private readonly HttpService _httpService;
    private readonly WallPlacer _wallPlacer;
    private readonly string _modelDestinationFolder;
    private readonly string _configurationOutputFolder;

    public FacadeConfiguratorService(HttpService httpService, string modelDestinationFolder,
        string configurationOutputFolder)
    {
        _httpService = httpService ?? new HttpService();
        _wallPlacer = new WallPlacer();
        _modelDestinationFolder =
            modelDestinationFolder ?? throw new ArgumentNullException(nameof(modelDestinationFolder));
        _configurationOutputFolder = configurationOutputFolder ??
                                     throw new ArgumentNullException(nameof(configurationOutputFolder));
        Directory.CreateDirectory(_modelDestinationFolder);
        Directory.CreateDirectory(_configurationOutputFolder);
    }

    public void Configure(UIApplication uiApp, string facadeConfigId)
    {
        var configuration =
            _httpService.GetForObject<FacadeConfigurationDto>($"/facade-configurations/find/{facadeConfigId}");
        
        Console.WriteLine(configuration.Name);
    }
}