using Gjubrarko.Application.Abstractions;
using Gjubrarko.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gjubrarko.Infrastructure.Identity;

public class UserDirectory(AppDbContext db) : IUserDirectory
{
    public Task<string?> GetDisplayNameAsync(Guid userId, CancellationToken ct) =>
        db.Users.Where(u => u.Id == userId).Select(u => (string?)u.DisplayName).FirstOrDefaultAsync(ct);
}
