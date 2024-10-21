using System.IO;
using System.Net;
using System.Net.Http;
using ASRR.Revit.Core.Http;
using RevitTemplate.Model;
using RevitTemplate.Settings;

namespace RevitTemplate.Services;

public class FileUploader(HttpService httpService)
{
    public void Upload(string configId, ExportSettings exportSettings)
    {
        if (configId == null) throw new ArgumentNullException(nameof(configId));
        if (exportSettings.ExportDirectory == null) throw new ArgumentNullException(nameof(exportSettings.ExportDirectory));
        // exportDirectory = C:\asrr\output\RevitTemplate
        // configDirectory = C:\asrr\output\RevitTemplate\AAABkbdcywLa5lfL
        var configDirectory = Path.Combine(exportSettings.ExportDirectory, configId);
        var paths = GetFilePaths(configDirectory, exportSettings);

        if (paths == null)
        {
            // _logger.Error("No files found to upload for facade configuration '{configId}'");
            return;
        }
        
        // _logger.Info($"Uploading {paths.Count} files to facade configuration '{configId}'");
        foreach (var path in paths)
        {
            var fileName = Path.GetFileName(path);
            // _logger.Info($"Uploading {fileName} to facade configuration '{configId}'");

            UploadFile(path, configId);

            // TODO: uncomment below when logging is put in place
            // if (UploadFile(path, configId))
            //     _logger.Info($"Successfully uploaded {fileName} to facade configuration '{configId}'");
            // else
            //     _logger.Error($"Failed to upload {fileName} to facade configuration '{configId}'");
        }
    }

    private bool UploadFile(string filePath, string configId)
    {
        var fileType = GetFileType(filePath);
        var uploadPath = $"/facade-configurations/{configId}/{fileType}";
        var content = GetFileContent(filePath);

        var response = httpService.Post(uploadPath, content);
        
        if (response.IsSuccessStatusCode) return true;
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // _logger.Error($"Facade configuration '{configId}' not found in database");
            return false;
        }
        // _logger.Error($"Failed to upload file '{Path.GetFileName(filePath)}' to facade configuration '{{configId}}'. Reason: {response.StatusCode} - {response.ReasonPhrase}");
        return false;
    }
    
    private static List<string> GetFilePaths(string directory, ExportSettings exportSettings)
    {
        var ext = new List<string>();
        if (exportSettings.Rvt) ext.Add(".rvt");
        if (exportSettings.Pdf) ext.Add(".pdf");

        var filePaths = Directory
            .EnumerateFiles(directory)
            .Where(p => ext.Contains(Path.GetExtension(p)))
            .ToList();

        return filePaths;
    }
    
    private static MultipartFormDataContent GetFileContent(string filePath)
    {
        if (File.Exists(@filePath))
        {
            return new MultipartFormDataContent
            {
                {new ByteArrayContent(File.ReadAllBytes(filePath)), "file", Path.GetFileName(filePath)}
            };
        }

        throw new FileNotFoundException($"Failed to upload: '{filePath}' does not exist");
    }
    
    private static string GetFileType(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        switch (extension)
        {
            case "glb":
            case "ifc":
            case "rvt": return FileType.Model;
            case "png": return FileType.Image;
            case "pdf": return FileType.Document;
        }

        // _logger.Error($"File at filepath {filePath} is not recognized as a valid extension");
        return extension;
    }
}

// /facade-configurations/{id}/{fileType}