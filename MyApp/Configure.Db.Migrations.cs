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
            ProfilePath = "/profiles/st/stackoverflow/stackoverflow.svg",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "most-voted",
            Email = "most-voted@email.com",
            DisplayName = "Most Voted",
            EmailConfirmed = true,
            ProfilePath = "/profiles/mo/most-voted/most-voted.svg",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "accepted",
            Email = "accepted@email.com",
            DisplayName = "Accepted",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ac/accepted/accepted.svg",
        }, "p@55wOrd");
        
        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "phi",
            Email = "phi@email.com",
            DisplayName = "Phi-2 2.7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ph/phi/phi.svg",
            Model = "phi", //3B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gemma-2b",
            Email = "gemma-2b@email.com",
            DisplayName = "Gemma 2B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ge/gemma-2b/gemma-2b.svg",
            Model = "gemma:2b", //3B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "qwen-4b",
            Email = "qwen@email.com",
            DisplayName = "Qwen 1.5 4B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/qw/qwen-4b/qwen-4b.svg",
            Model = "qwen:4b", //4B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "codellama",
            Email = "codellama-13B@email.com",
            DisplayName = "Code Llama 7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/co/codellama/codellama.svg",
            Model = "codellama", //7B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gemma",
            Email = "gemma@email.com",
            DisplayName = "Gemma 7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ge/gemma/gemma.svg",
            Model = "gemma", //7B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "deepseek-coder-6.7b",
            Email = "deepseek-coder@email.com",
            DisplayName = "DeepSeek Coder 6.7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/de/deepseek-coder/deepseek-coder.jpg",
            Model = "deepseek-coder:6.7b", //16B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "mistral",
            Email = "mistral-7B@email.com",
            DisplayName = "Mistral 7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/mi/mistral/mistral.svg",
            Model = "mistral", //7B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "mixtral",
            Email = "mixtral-8x7b@email.com",
            DisplayName = "Mixtral 8x7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/mi/mixtral/mixtral.jpg",
            Model = "mixtral", //47B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gemini-pro",
            Email = "gemini-pro@email.com",
            DisplayName = "Gemini Pro 1.0",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ge/gemini-pro/gemini-pro.svg",
            Model = "gemini-pro",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "deepseek-coder-33b",
            Email = "deepseek-coder-33b@email.com",
            DisplayName = "DeepSeek Coder 33B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/de/deepseek-coder-33b/deepseek-coder-33b.jpg",
            Model = "deepseek-coder:33b",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gpt4-turbo",
            Email = "gpt4-turbo@email.com",
            DisplayName = "GPT-4 Turbo",
            EmailConfirmed = true,
            ProfilePath = "/profiles/gp/gpt4-turbo/gpt4-turbo.svg",
            Model = "gpt-4-turbo",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "claude3-haiku",
            Email = "claude3-haiku@email.com",
            DisplayName = "Claude 3 Haiku",
            EmailConfirmed = true,
            ProfilePath = "/profiles/cl/claude3-haiku/claude3-haiku.svg",
            Model = "claude-3-haiku",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "claude3-sonnet",
            Email = "claude3-sonnet@email.com",
            DisplayName = "Claude 3 Sonnet",
            EmailConfirmed = true,
            ProfilePath = "/profiles/cl/claude3-sonnet/claude3-sonnet.svg",
            Model = "claude-3-sonnet",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "claude3-opus",
            Email = "claude3-opus@email.com",
            DisplayName = "Claude 3 Opus",
            EmailConfirmed = true,
            ProfilePath = "/profiles/cl/claude3-opus/claude3-opus.svg",
            Model = "claude-3-opus",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "servicestack",
            Email = "team@servicestack.net",
            DisplayName = "ServiceStack",
            EmailConfirmed = true,
            ProfilePath = "/profiles/se/servicestack/servicestack.svg",
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
            UserName = "layoric",
            Email = "darren@reidmail.org",
            DisplayName = "layoric",
            EmailConfirmed = true,
            ProfilePath = "/profiles/la/layoric/layoric.png",
        }, "p@55wOrd", [Roles.Moderator]);

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "reddit",
            Email = "reddit@email.com",
            DisplayName = "Reddit",
            EmailConfirmed = true,
            ProfilePath = "/profiles/re/reddit/reddit.svg",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "discourse",
            Email = "discourse@email.com",
            DisplayName = "Discourse",
            EmailConfirmed = true,
            ProfilePath = "/profiles/di/discourse/discourse.svg",
        }, "p@55wOrd");
    }
}
