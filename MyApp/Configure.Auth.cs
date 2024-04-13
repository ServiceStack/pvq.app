using ServiceStack.Auth;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.Data;
using ServiceStack.OrmLite;

[assembly: HostingStartup(typeof(MyApp.ConfigureAuth))]

namespace MyApp;

public class ConfigureAuth : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureAppHost(appHost =>
        {
            appHost.Plugins.Add(new AuthFeature(IdentityAuth.For<ApplicationUser>(options => {
                options.SessionFactory = () => new CustomUserSession();
                options.CredentialsAuth();
                options.AdminUsersFeature(feature =>
                {
                    feature.OnBeforeDeleteUser = async (req, userId) =>
                    {
                        var dbFactory = req.TryResolve<IDbConnectionFactory>();
                        using var db = await dbFactory.OpenAsync();
                        await db.DeleteByIdAsync<UserInfo>(userId);
                    };
                });
            })));
        });
}
