using Amazon.S3;
using Gjubrarko.Application.Abstractions;
using Gjubrarko.Infrastructure.Identity;
using Gjubrarko.Infrastructure.Images;
using Gjubrarko.Infrastructure.Persistence;
using Gjubrarko.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Gjubrarko.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(
            config.GetConnectionString("Database"),
            npgsql => npgsql.UseNetTopologySuite()));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<DataSeeder>();

        services.Configure<StorageOptions>(config.GetSection(StorageOptions.Section));
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var o = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            return new AmazonS3Client(o.AccessKey, o.SecretKey, new AmazonS3Config { ServiceURL = o.ServiceUrl, ForcePathStyle = true });
        });
        services.AddSingleton<S3PhotoStorage>();
        services.AddSingleton<IPhotoStorage>(sp => sp.GetRequiredService<S3PhotoStorage>());
        services.AddSingleton<IImageProcessor, MagickImageProcessor>();
        return services;
    }
}
