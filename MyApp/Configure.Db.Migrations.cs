using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Migrations;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

[assembly: HostingStartup(typeof(MyApp.ConfigureDbMigrations))]

namespace MyApp;

// Code-First DB Migrations: https://docs.servicestack.net/ormlite/db-migrations
public class ConfigureDbMigrations : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureAppHost(appHost => {
            var migrator = new Migrator(appHost.Resolve<IDbConnectionFactory>(), typeof(Migration1000).Assembly);
            AppTasks.Register("migrate", _ =>
            {
                var log = appHost.GetApplicationServices().GetRequiredService<ILogger<ConfigureDbMigrations>>();

                log.LogInformation("Running EF Migrations...");
                var scopeFactory = appHost.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
                using (var scope = scopeFactory.CreateScope())
                {
                    using var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.Database.EnsureCreated();
                    db.Database.Migrate();

                    // Only seed users if DB was just created
                    if (!db.Users.Any())
                    {
                        log.LogInformation("Adding Seed Users...");
                        AddSeedUsers(scope.ServiceProvider).Wait();
                    }
                }

                log.LogInformation("Running OrmLite Migrations...");
                migrator.Run();
            });
            AppTasks.Register("migrate.revert", args => migrator.Revert(args[0]));
            AppTasks.Run();
        });

    private async Task AddSeedUsers(IServiceProvider services)
    {
        //initializing custom roles 
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        string[] allRoles = Roles.All;

        void assertResult(IdentityResult result)
        {
            if (!result.Succeeded)
                throw new Exception(result.Errors.First().Description);
        }

        async Task EnsureUserAsync(ApplicationUser user, string password, string[]? roles = null)
        {
            var existingUser = await userManager.FindByEmailAsync(user.Email!);
            if (existingUser != null) return;

            await userManager!.CreateAsync(user, password);
            if (roles?.Length > 0)
            {
                var newUser = await userManager.FindByEmailAsync(user.Email!);
                assertResult(await userManager.AddToRolesAsync(user, roles));
            }
        }

        foreach (var roleName in allRoles)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                //Create the roles and seed them to the database
                assertResult(await roleManager.CreateAsync(new IdentityRole(roleName)));
            }
        }

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "admin",
            Email = "admin@email.com",
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true,
        }, "p@55wOrd", allRoles);

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "human",
            Email = "human@email.com",
            FirstName = "Human",
            LastName = "User",
            EmailConfirmed = true,
            ProfileUrl = "/img/profiles/user1.svg",
        }, "p@55wOrd");
        
        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "phi",
            Email = "phi@email.com",
            FirstName = "Phi-2",
            LastName = "2.7B",
            EmailConfirmed = true,
            ProfileUrl = "/img/profiles/phi-2.svg",
            Model = "phi", //3B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gemma-2b",
            Email = "gemma-2b@email.com",
            FirstName = "Gemma",
            LastName = "2B",
            EmailConfirmed = true,
            ProfileUrl = "/img/profiles/gemma-2b.svg",
            Model = "gemma:2b", //3B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "starcoder2-3b",
            Email = "starcoder2-3b@email.com",
            FirstName = "StarCoder2",
            LastName = "3B",
            EmailConfirmed = true,
            ProfileUrl = "/img/profiles/starcoder-3b.png",
            Model = "starcoder2:3b", //3B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gemma",
            Email = "gemma@email.com",
            FirstName = "Gemma",
            LastName = "7B",
            EmailConfirmed = true,
            ProfileUrl = "/img/profiles/gemma-7b.svg",
            Model = "gemma", //9B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "codellama",
            Email = "codellama-13B@email.com",
            FirstName = "Code Llama",
            LastName = "7B",
            EmailConfirmed = true,
            ProfileUrl = "/img/profiles/codellama.svg",
            Model = "codellama", //7B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "mistral",
            Email = "mistral-7B@email.com",
            FirstName = "Mistral",
            LastName = "7B", 
            EmailConfirmed = true,
            ProfileUrl = "/img/profiles/mistral.svg",
            Model = "mistral", //7B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "starcoder2-15b",
            Email = "starcoder2-15b@email.com",
            FirstName = "StarCoder2",
            LastName = "15B",
            EmailConfirmed = true,
            ProfileUrl = "/img/profiles/starcoder2-15b.png",
            Model = "starcoder2:15b", //16B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "mixtral",
            Email = "mixtral-8x7b@email.com",
            FirstName = "Mixtral",
            LastName = "8x7B",
            EmailConfirmed = true,
            ProfileUrl = "/img/profiles/mixtral.jpg",
            Model = "mixtral", //47B
        }, "p@55wOrd");
    }
}
