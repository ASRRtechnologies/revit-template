namespace RevitTemplate.Settings;

public class ExportSettings
{
    public string TemplateFilePath { get; set; } = @"C:\asrr\Template1.rte";
    public string ExportDirectory { get; set; } = @"C:\asrr\output\RevitTemplate";

    public bool Rvt { get; set; } = true;
    public bool Pdf { get; set; } = true;
    public bool UploadToDb { get; set; } = true;
}