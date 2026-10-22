using Microsoft.AspNetCore.Identity;

namespace Gjubrarko.Infrastructure.Identity;

public class AppUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
    public string PreferredLanguage { get; set; } = "mk";
    // Set for AuthorityStaff accounts only.
    public int? AuthorityId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class Roles
{
    public const string Citizen = "Citizen";
    public const string AuthorityStaff = "AuthorityStaff";
    public const string Moderator = "Moderator";
    public const string Admin = "Admin";

    public static readonly string[] All = [Citizen, AuthorityStaff, Moderator, Admin];
}
