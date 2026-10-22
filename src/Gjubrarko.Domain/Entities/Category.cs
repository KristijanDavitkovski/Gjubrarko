namespace Gjubrarko.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string NameMk { get; set; }
    public required string NameSq { get; set; }
    public required string NameEn { get; set; }
    public string? Icon { get; set; }

    public int DefaultAuthorityTypeId { get; set; }
    public AuthorityType? DefaultAuthorityType { get; set; }
}
