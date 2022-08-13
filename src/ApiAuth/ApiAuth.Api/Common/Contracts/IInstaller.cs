namespace ApiAuth.Api.Common.Contracts;

public interface IInstaller
{
    void InstallServices(IServiceCollection services, IConfiguration configuration);
}