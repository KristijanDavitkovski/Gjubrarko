using Gjubrarko.Domain.Enums;

namespace Gjubrarko.Domain.Entities;

public class AuthorityType
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string NameMk { get; set; }
    public required string NameSq { get; set; }
    public required string NameEn { get; set; }
    public AuthorityScope Scope { get; set; }
}
