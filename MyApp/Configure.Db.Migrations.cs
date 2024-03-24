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
            DisplayName = "Administrator",
            EmailConfirmed = true,
        }, "p@55wOrd", allRoles);

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "stackoverflow",
            Email = "stackoverflow@email.com",
            DisplayName = "StackOverflow",
            EmailConfirmed = true,
            ProfilePath = "/profiles/hu/human.svg",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "most-voted",
            Email = "most-voted@email.com",
            DisplayName = "Most Voted",
            EmailConfirmed = true,
            ProfilePath = "/profiles/hu/human.svg",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "accepted",
            Email = "accepted@email.com",
            DisplayName = "Accepted",
            EmailConfirmed = true,
            ProfilePath = "/profiles/hu/human-accepted.svg",
        }, "p@55wOrd");
        
        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "phi",
            Email = "phi@email.com",
            DisplayName = "Phi-2 2.7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ph/phi-2.svg",
            Model = "phi", //3B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gemma-2b",
            Email = "gemma-2b@email.com",
            DisplayName = "Gemma 2B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ge/gemma-2b.svg",
            Model = "gemma:2b", //3B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "qwen-4b",
            Email = "qwen@email.com",
            DisplayName = "Qwen 1.5",
            EmailConfirmed = true,
            ProfilePath = "/profiles/qw/qwen.svg",
            Model = "qwen:4b", //4B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "codellama",
            Email = "codellama-13B@email.com",
            DisplayName = "Code Llama 7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/co/codellama.svg",
            Model = "codellama", //7B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gemma",
            Email = "gemma@email.com",
            DisplayName = "Gemma 7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ge/gemma-7b.svg",
            Model = "gemma", //9B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "deepseek-coder",
            Email = "deepseek-coder@email.com",
            DisplayName = "DeepSeek Coder 6.7b",
            EmailConfirmed = true,
            ProfilePath = "/profiles/de/deepseek-coder.jpg",
            Model = "deepseek-coder:6.7b", //16B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "mistral",
            Email = "mistral-7B@email.com",
            DisplayName = "Mistral 7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/mi/mistral.svg",
            Model = "mistral", //7B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "mixtral",
            Email = "mixtral-8x7b@email.com",
            DisplayName = "Mixtral 8x7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/mi/mixtral.jpg",
            Model = "mixtral", //47B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "servicestack",
            Email = "team@servicestack.net",
            DisplayName = "ServiceStack",
            EmailConfirmed = true,
            ProfilePath = "/profiles/se/servicestack.svg",
        }, "p@55wOrd", [Roles.Moderator]);

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "mythz",
            Email = "demis.bellot@gmail.com",
            DisplayName = "mythz",
            EmailConfirmed = true,
            ProfilePath = "/profiles/my/mythz/kerrigan.png",
        }, "p@55wOrd", [Roles.Moderator]);

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "reddit",
            Email = "reddit@email.com",
            DisplayName = "Reddit",
            EmailConfirmed = true,
            ProfilePath = "/profiles/re/reddit.svg",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "discourse",
            Email = "discourse@email.com",
            DisplayName = "Discourse",
            EmailConfirmed = true,
            ProfilePath = "/profiles/di/discourse.svg",
        }, "p@55wOrd");
    }
}
