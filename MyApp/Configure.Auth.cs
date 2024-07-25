using ServiceStack.Auth;
using MyApp.Data;
using MyApp.ServiceModel;
using ServiceStack.Data;
using ServiceStack.Html;
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
                    feature.FormLayout =
                    [
                        Input.For<ApplicationUser>(x => x.UserName, c => c.FieldsPerRow(2)),
                        Input.For<ApplicationUser>(x => x.Email, c => { 
                            c.Type = Input.Types.Email;
                            c.FieldsPerRow(2); 
                        }),
                        Input.For<ApplicationUser>(x => x.Model, c =>
                        {
                            c.FieldsPerRow(2); 
                        }),
                        Input.For<ApplicationUser>(x => x.DisplayName, c =>
                        {
                            c.FieldsPerRow(2); 
                        }),
                        Input.For<ApplicationUser>(x => x.ProfilePath)
                    ];
                        
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
