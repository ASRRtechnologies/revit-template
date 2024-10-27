namespace RevitTemplate.Dto;

public class QueueItemDto
{
    public string Id { get; set; }
    public string ProjectId { get; set; }
    public string ProjectName { get; set; }
    public List<BlockConfiguration> Configuration { get; set; }
}

public class BlockConfiguration
{
    public string BlockId { get; set; }
    public string Name { get; set; }
    public Position Position  { get; set; }
    public Rotation Rotation  { get; set; }
}