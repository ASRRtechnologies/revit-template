using System.IO;
using ASRR.Revit.Core.Http;
using ASRR.Revit.Core.RevitModel;
using Autodesk.Revit.UI;
using RevitTemplate.Dto;
using RevitTemplate.Exceptions;

namespace RevitTemplate.Services;

public class FacadeConfiguratorService
{
    private readonly HttpService _httpService;
    private readonly ModelFetcher _modelFetcher;
    private readonly WallPlacer _wallPlacer;
    private readonly string _modelDestinationFolder;
    private readonly string _configurationOutputFolder;

    public FacadeConfiguratorService(HttpService httpService, string modelDestinationFolder,
        string configurationOutputFolder)
    {
        _httpService = httpService ?? new HttpService();
        _modelFetcher = new ModelFetcher(_httpService);
        _wallPlacer = new WallPlacer();
        _modelDestinationFolder =
            modelDestinationFolder ?? throw new ArgumentNullException(nameof(modelDestinationFolder));
        _configurationOutputFolder = configurationOutputFolder ??
                                     throw new ArgumentNullException(nameof(configurationOutputFolder));
        Directory.CreateDirectory(_modelDestinationFolder);
        Directory.CreateDirectory(_configurationOutputFolder);
    }

    public void Configure(UIApplication uiApp, string configId)
    {
        if (configId == null)
        {
            // TODO: log
            throw new FacadeConfigurationException("Facade configuration failed. ConfigurationId is null");
        }

        var configuration =
            _httpService.GetForObject<FacadeConfigurationDto>($"/facade-configurations/find/{configId}");

        var storedElements = FetchElementModels(configuration.Openings);

        Console.WriteLine(configuration.Name);
        Console.WriteLine(configuration.Openings.First().Name);
        Console.WriteLine(configuration.Openings.First().Position.X);
        Console.WriteLine(configuration.Openings.First().Height);
        Console.WriteLine(configuration.Openings.First().Width);
    }

    private Dictionary<string, string> FetchElementModels(List<OpeningDto> openings)
    {
        var elements =
            _httpService.PostForObject<List<ElementDto>, IEnumerable<string>>("/elements/find",
                openings.Select(o => o.ElementId));

        var storedElements = new Dictionary<string, string>();
        
        foreach (var element in elements)
        {
            if (element.Files == null || !element.Files.Any())
            {
                // _logger.Error($"No files found for model '{model.Name}'. Skipping download..");
                continue;
            }
            
            var rvtFile = element.Files.FirstOrDefault(f => f.Extension == "rvt");
            
            if (rvtFile == null)
            {
                // _logger.Error($"No rvt file found for model '{model.Name}'. Skipping download..");
                continue;
            }
            
            var fileName = $"{element.Name}.rvt";
            var fetchPath = $"/blob-storage/download/{rvtFile.BlobId}/{fileName}";
            var destinationPath = Path.Combine(_modelDestinationFolder, fileName);
            
            if (File.Exists(destinationPath) && (File.GetLastWriteTime(destinationPath) > rvtFile.Created))
            {
                storedElements[element.Id] = destinationPath;
                continue;
            }
            
            if (_modelFetcher.Fetch(fetchPath, destinationPath)) storedElements[element.Id] = destinationPath;
        }

        return storedElements;
    }
}