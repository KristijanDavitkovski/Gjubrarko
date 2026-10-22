namespace Gjubrarko.Application.Abstractions;

public interface IPhotoStorage
{
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct);
    string GetReadUrl(string key);
}
