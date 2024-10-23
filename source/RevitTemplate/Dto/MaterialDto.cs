namespace RevitTemplate.Dto;

public class MaterialDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<Texture> Textures { get; set; }
    public double TextureHeight { get; set; }
    public double TextureWidth { get; set; }
}

public class Texture
{
    public FileData File { get; set; }
    public string Type { get; set; }

    public TextureType GetTextureType()
    {
        return Type switch
        {
            "COLOR" => TextureType.Color,
            "NORMAL" => TextureType.Normal,
            "AO" => TextureType.Ao,
            "ROUGHNESS" => TextureType.Roughness,
            "BUMP" => TextureType.Bump,
            "RVT_PNG" => TextureType.RvtPng,
            _ => TextureType.Unknown
        };
    }
}

public enum TextureType
{
    Color,
    Normal,
    Ao,
    Roughness,
    Bump,
    RvtPng,
    Unknown
}

// public class TextureType
// {
//     public const string
//         Color = "COLOR",
//         Normal = "NORMAL",
//         Ao = "AO",
//         Roughness = "ROUGHNESS",
//         Bump = "BUMP",
//         RvtPng = "RVT_PNG";
// }