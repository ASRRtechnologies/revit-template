namespace RevitTemplate.Dto;

public class VectorDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}

public class Position : VectorDto
{
}

public class Rotation : VectorDto
{
}