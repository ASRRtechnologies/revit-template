namespace RevitTemplate.Dto;

public class ElementDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<FileData> Files { get; set; }
}