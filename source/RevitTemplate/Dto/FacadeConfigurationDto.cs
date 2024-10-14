namespace RevitTemplate.Dto;

public class FacadeConfigurationDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public VectorDto Dimensions { get; set; }
    public List<OpeningDto> Openings { get; set; }
    public List<PlaneDto> Planes { get; set; }
}

public class OpeningDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ElementId { get; set; }
    public Position Position  { get; set; }
    public double Height { get; set; }
    public double Width { get; set; }
}

public class PlaneDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string MaterialId { get; set; }
    public Position Position  { get; set; }
    public double Height { get; set; }
    public double Width { get; set; }
}

public class FileData
{
    public string Name { get; set; }
    public string Extension { get; set; }
    public string Type { get; set; }
    public string BlobId { get; set; }
    public int FileSize { get; set; }
    public DateTime Created { get; set; }
}