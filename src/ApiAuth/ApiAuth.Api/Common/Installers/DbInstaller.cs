using ApiAuth.Api.Common.Contracts;
using ApiAuth.Data;
using ApiAuth.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace ApiAuth.Api.Common.Installers;

public class DbInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("PostgresConnection"));
        })
            .AddIdentity<ApplicationUser, ApplicationRole>()
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
    }
}