using ApiAuth.Api.Common;
using ApiAuth.Api.Common.Extensions;
using ApiAuth.Api.Options;
using ApiAuth.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;

namespace ApiAuth.Api;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.InstallServicesInAssembly(Configuration);
        services.AddControllers();
        services.AddSwaggerGen(x =>
        {
            x.SwaggerDoc("v1",new OpenApiInfo {Title = "ApiAuth.Api", Version = "v1"});

            // Authentication
            var security = new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header,

                    },
                    new List<string>()
                }
            };

            x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });

            x.AddSecurityRequirement(security);
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            var swaggerOptions = new SwaggerOptions();
            Configuration.GetSection(nameof(SwaggerOptions)).Bind(swaggerOptions);
            app.UseSwagger(options => { options.RouteTemplate = swaggerOptions.JsonRoute; });
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(swaggerOptions.UiEndpoint, swaggerOptions.Description);
            });
        }
        
        using (var serviceScope = app.ApplicationServices.CreateScope())
        {
            var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                
            var defaultUser = new DefaultUser();
            Configuration.GetSection(nameof(DefaultUser)).Bind(defaultUser);
            if (!roleManager.RoleExistsAsync(GlobalConstants.Roles.Admin).GetAwaiter().GetResult())
            {
                var adminRole = new ApplicationRole(GlobalConstants.Roles.Admin);
                roleManager.CreateAsync(adminRole).GetAwaiter().GetResult();
            }

            if (userManager.FindByNameAsync(defaultUser.Username).GetAwaiter().GetResult() == null)
            {
                var newUser = new ApplicationUser
                {
                    UserName = defaultUser.Username,
                    Email = defaultUser.Email
                };
                userManager.CreateAsync(newUser, defaultUser.Password).GetAwaiter().GetResult();
                userManager.AddToRoleAsync(newUser, GlobalConstants.Roles.Admin).GetAwaiter().GetResult();
            }
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAuthentication();
        
        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}