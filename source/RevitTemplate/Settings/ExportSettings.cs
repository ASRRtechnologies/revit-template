namespace RevitTemplate.Settings;

public class ExportSettings
{
    public string TemplateFilePath { get; set; } = @"C:\asrr\Template1.rte";
    public string ExportDirectory { get; set; } = @"C:\asrr\output\RevitTemplate";
    public string GlbExportViewName { get; set; } = null;
    public bool Rvt { get; set; } = true;
    public bool Pdf { get; set; } = true;
    public bool Glb { get; set; } = true;
    public bool UploadToDb { get; set; } = true;
}