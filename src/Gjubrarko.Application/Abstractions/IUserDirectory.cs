namespace Gjubrarko.Application.Abstractions;

public interface IUserDirectory
{
    Task<string?> GetDisplayNameAsync(Guid userId, CancellationToken ct);
}
