using NetTopologySuite.Geometries;

namespace Gjubrarko.Domain.Entities;

public class Municipality
{
    public int Id { get; set; }
    public required string Slug { get; set; }
    public required string NameMk { get; set; }
    public required string NameSq { get; set; }
    public required string NameEn { get; set; }
    public int? RegionId { get; set; }
    public Region? Region { get; set; }

    // The ten municipalities of the City of Skopje point at the City of Skopje entry.
    public int? ParentId { get; set; }
    public Municipality? Parent { get; set; }

    public Point? Centroid { get; set; }
    public MultiPolygon? Boundary { get; set; }
}
