namespace RevitTemplate.Dto;

public class MaterialDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public FileData TextureFile { get; set; }
    public double TextureHeight { get; set; }
    public double TextureWidth { get; set; }
}