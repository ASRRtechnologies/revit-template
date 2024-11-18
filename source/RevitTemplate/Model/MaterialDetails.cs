using RevitTemplate.Dto;

namespace RevitTemplate.Model;

public class MaterialDetails
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Dictionary<TextureType, string> Textures { get; set; } = new();
}