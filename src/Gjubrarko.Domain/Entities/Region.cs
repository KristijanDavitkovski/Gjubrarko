namespace Gjubrarko.Domain.Entities;

public class Region
{
    public int Id { get; set; }
    public required string NameMk { get; set; }
    public required string NameSq { get; set; }
    public required string NameEn { get; set; }

    public List<Municipality> Municipalities { get; set; } = [];
}
