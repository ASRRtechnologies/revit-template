namespace RevitTemplate.Dto;

public class VectorDto
{
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
    public double Z { get; set; } = 0;

    public XYZ ToXyz()
    {
        return new XYZ(X, Y, Z);
    }
}

public class Position : VectorDto
{
    public new XYZ ToXyz()
    {
        return new XYZ(X, -Z, Y);
    }
}

public class Rotation : VectorDto
{
}