using ApiAuth.Api.Common.Contracts;

namespace ApiAuth.Api.Common.Extensions;

public static class InstallersExtension
{
    public static void InstallServicesInAssembly(this IServiceCollection services, IConfiguration configuration)
    {
        var installers = typeof(Startup).Assembly.ExportedTypes
            .Where(x => typeof(IInstaller).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
            .Select(Activator.CreateInstance).Cast<IInstaller>().ToList();
        
        installers.ForEach(i=>i.InstallServices(services, configuration));
    }
}