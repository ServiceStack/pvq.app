[assembly: HostingStartup(typeof(MyApp.ConfigureCors))]

namespace MyApp;

public class ConfigureCors : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services =>
        {
            services.AddCors(options => {
                options.AddDefaultPolicy(policy => {
                    policy.WithOrigins([
                        "http://localhost:5000", "https://localhost:5001", 
                        "http://127.0.0.1:8787", "https://mythz.pvq.app", 
                        "https://locode.dev", "https://www.locode.dev", "https://pvq.locode.dev"
                    ])
                    .AllowCredentials()
                    .WithHeaders(["Content-Type", "Allow", "Authorization"])
                    .SetPreflightMaxAge(TimeSpan.FromHours(1));
                });
            });
            services.AddTransient<IStartupFilter, StartupFilter>();
        });

    public class StartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.UseCors();
            next(app);
        };
    }        
}
