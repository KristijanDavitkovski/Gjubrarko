namespace Gjubrarko.Domain.Entities;

public class Authority
{
    public int Id { get; set; }
    public int AuthorityTypeId { get; set; }
    public AuthorityType? AuthorityType { get; set; }

    // Null for national bodies that serve every municipality.
    public int? MunicipalityId { get; set; }
    public Municipality? Municipality { get; set; }

    public required string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; } = true;
}
