namespace RevitTemplate.Dto;

public class ProjectGeometryDto
{
    public string Id { get; set; }
    public List<BlockGeometry> Blocks { get; set; } = [];
}

public class BlockGeometry
{
    public BlockConfiguration BlockConfiguration { get; set; }
    public List<HouseGeometry> Houses { get; set; } = [];
    public string filePath { get; set; }
}

public class HouseGeometry
{
    public HouseConfiguration HouseConfiguration { get; set; }
    public List<DynamicModelGeometry> DynamicModels { get; set; } = [];
    public double DistanceToNextHouse { get; set; }
    public string filePath { get; set; }
}

public class DynamicModelGeometry
{
    public string Id { get; set; }
    public Position Position { get; set; } = new();
    public Rotation Rotation { get; set; } = new();
    public VectorDto Dimensions { get; set; } = new();
    public FacadeConfigurationDto FacadeConfiguration { get; set; }
    public string filePath { get; set; }
}

public class BlockConfiguration
{
    public string BlockId { get; set; }
    public string Name { get; set; }
    public Position Position { get; set; } = new();
    public Rotation Rotation { get; set; } = new();
}

public class HouseConfiguration
{
    public string Bnr { get; set; }
    public Position Position { get; set; } = new();
    public Rotation Rotation { get; set; } = new();
}