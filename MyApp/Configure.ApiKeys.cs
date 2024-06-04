using MyApp.Data;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Configuration;

[assembly: HostingStartup(typeof(MyApp.ConfigureApiKeys))]

namespace MyApp;

public class ConfigureApiKeys : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services =>
        {
            services.AddPlugin(new ApiKeysFeature
            {
                // Optional: Available Scopes Admin Users can assign to any API Key
                // Features = [
                //     "Paid",
                //     "Tracking",
                // ],
                // Optional: Available Features Admin Users can assign to any API Key
                // Scopes = [
                //     "todo:read",
                //     "todo:write",
                // ],
                
                // Optional: Limit available Scopes Users can assign to their own API Keys
                // UserScopes = [
                //     "todo:read",
                // ],
                // Optional: Limit available Features Users can assign to their own API Keys
                // UserFeatures = [
                //     "Tracking",
                // ],
            });
        })
        .ConfigureAppHost(appHost =>
        {
            using var db = appHost.Resolve<IDbConnectionFactory>().Open();
            var apiKeysFeature = appHost.GetPlugin<ApiKeysFeature>();
            apiKeysFeature.InitSchema(db);
            
            // Optional: Create API Key for specified Users on Startup
            if (apiKeysFeature.ApiKeyCount(db) == 0)
            {
                var createApiKeysFor = new [] { "admin@email.com", "manager@email.com" };
                var users = db.Select<ApplicationUser>(x => createApiKeysFor.Contains(x.UserName));
                foreach (var user in users)
                {
                    List<string> scopes = user.UserName == "admin@email.com"
                        ? [RoleNames.Admin] 
                        : [];
                    apiKeysFeature.Insert(db, 
                        new() { Name = "Seed API Key", UserId = user.Id, UserName = user.UserName, Scopes = scopes });
                }
            }
        });
}