using ApiAuth.Api.Common.Contracts;
using ApiAuth.Data.Common.Repositories;
using ApiAuth.Data.Repositories;

namespace ApiAuth.Api.Common.Installers;

public class RepositoriesInstaller: IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(typeof(IDeletableEntityRepository<>), typeof(EfDeletableEntityRepository<>));
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
    }
}