using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Migrations;
using MyApp.ServiceModel;
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
            Email = "servicestack.mail+admin@gmail.com",
            DisplayName = "Administrator",
            EmailConfirmed = true,
        }, "p@55wOrd", allRoles);

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "pvq",
            Email = "servicestack.mail+pvq@gmail.com",
            DisplayName = "pvq",
            EmailConfirmed = true,
            ProfilePath = "/profiles/pv/pvq/pvq.svg",
        }, "p@55wOrd", [Roles.Admin, Roles.Moderator]);

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "stackoverflow",
            Email = "servicestack.mail+stackoverflow@gmail.com",
            DisplayName = "StackOverflow",
            EmailConfirmed = true,
            ProfilePath = "/profiles/st/stackoverflow/stackoverflow.svg",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "most-voted",
            Email = "servicestack.mail+most-voted@gmail.com",
            DisplayName = "Most Voted",
            EmailConfirmed = true,
            ProfilePath = "/profiles/mo/most-voted/most-voted.svg",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "accepted",
            Email = "servicestack.mail+accepted@gmail.com",
            DisplayName = "Accepted",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ac/accepted/accepted.svg",
        }, "p@55wOrd");
        
        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "phi",
            Email = "servicestack.mail+phi@gmail.com",
            DisplayName = "Phi 3 4B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ph/phi/phi.svg",
            Model = "phi3", //3B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "phi4",
            Email = "servicestack.mail+phi4@gmail.com",
            DisplayName = "Phi 4 14B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ph/phi4/phi4.svg",
            Model = "phi4",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gemma-2b",
            Email = "servicestack.mail+gemma-2b@gmail.com",
            DisplayName = "Gemma 2B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ge/gemma-2b/gemma-2b.svg",
            Model = "gemma:2b", //3B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "qwen-4b",
            Email = "servicestack.mail+qwen@gmail.com",
            DisplayName = "Qwen 1.5 4B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/qw/qwen-4b/qwen-4b.svg",
            Model = "qwen:4b", //4B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "qwen2-72b",
            Email = "servicestack.mail+qwen2-72b@gmail.com",
            DisplayName = "Qwen2 72B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/qw/qwen2-72b/qwen2-72b.svg",
            Model = "qwen2:72b", //72B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "qwen2.5-72b",
            Email = "servicestack.mail+qwen2.5-72b@gmail.com",
            DisplayName = "Qwen2.5 72B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/qw/qwen2-72b/qwen2-72b.svg",
            Model = "qwen2.5:72b", //72B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "qwq-32b",
            Email = "servicestack.mail+qwq-32b@gmail.com",
            DisplayName = "QWQ 32b",
            EmailConfirmed = true,
            ProfilePath = "/profiles/qw/qwq-32b/qwq-32b.svg",
            Model = "qwq:32b",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "codellama",
            Email = "servicestack.mail+codellama-13B@gmail.com",
            DisplayName = "Code Llama 7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/co/codellama/codellama.svg",
            Model = "codellama", //7B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "llama3-8b",
            Email = "servicestack.mail+llama3-8b@gmail.com",
            DisplayName = "Llama 3 8B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ll/llama3-8b/llama3-8b.svg",
            Model = "llama3:8b", //8B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "llama3.1-8b",
            Email = "servicestack.mail+llama3.1-8b@gmail.com",
            DisplayName = "Llama 3.1 8B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ll/llama3.1-8b/llama3.1-8b.svg",
            Model = "llama3.1:8b", //8B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "llama3-70b",
            Email = "servicestack.mail+llama3-70b@gmail.com",
            DisplayName = "Llama 3 70B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ll/llama3-70b/llama3-70b.svg",
            Model = "llama3:70b", //70B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "llama3.3-70b",
            Email = "servicestack.mail+llama3.3-70b@gmail.com",
            DisplayName = "Llama 3.3 70B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ll/llama3-70b/llama3-70b.svg",
            Model = "llama3.3:70b",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gemma",
            Email = "servicestack.mail+gemma@gmail.com",
            DisplayName = "Gemma 7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ge/gemma/gemma.svg",
            Model = "gemma", //7B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gemma2-27b",
            Email = "servicestack.mail+gemma2-27b@gmail.com",
            DisplayName = "Gemma2 27B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ge/gemma2-27b/gemma2-27b.svg",
            Model = "gemma2:27b", //27B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "deepseek-coder",
            Email = "servicestack.mail+deepseek-coder@gmail.com",
            DisplayName = "DeepSeek Coder 6.7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/de/deepseek-coder/deepseek-coder.jpg",
            Model = "deepseek-coder:6.7b", //16B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "mistral",
            Email = "servicestack.mail+mistral-7B@gmail.com",
            DisplayName = "Mistral 7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/mi/mistral/mistral.svg",
            Model = "mistral", //7B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "mistral-nemo",
            Email = "servicestack.mail+mistral-nemo@gmail.com",
            DisplayName = "Mistral NeMo",
            EmailConfirmed = true,
            ProfilePath = "/profiles/mi/mistral-nemo/mistral-nemo.svg",
            Model = "mistral-nemo", //12
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "mixtral",
            Email = "servicestack.mail+mixtral-8x7b@gmail.com",
            DisplayName = "Mixtral 8x7B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/mi/mixtral/mixtral.jpg",
            Model = "mixtral", //47B
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "deepseek-coder2-236b",
            Email = "servicestack.mail+deepseek-coder2-236b@gmail.com",
            DisplayName = "DeepSeek Coder2 236B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/de/deepseek-coder2-236b/deepseek-coder2-236b.jpg",
            Model = "deepseek-coder-v2:236b",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "deepseek-coder-33b",
            Email = "servicestack.mail+deepseek-coder-33b@gmail.com",
            DisplayName = "DeepSeek Coder 33B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/de/deepseek-coder-33b/deepseek-coder-33b.jpg",
            Model = "deepseek-coder:33b",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "deepseek-v3-671b",
            Email = "servicestack.mail+deepseek-v3-671b@gmail.com",
            DisplayName = "DeepSeek v3",
            EmailConfirmed = true,
            ProfilePath = "/profiles/de/deepseek-v3-671b/deepseek-v3-671b.jpg",
            Model = "deepseek-v3:671b",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gpt4-turbo",
            Email = "servicestack.mail+gpt4-turbo@gmail.com",
            DisplayName = "GPT-4 Turbo",
            EmailConfirmed = true,
            ProfilePath = "/profiles/gp/gpt4-turbo/gpt4-turbo.svg",
            Model = "gpt-4-turbo",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gpt3.5-turbo",
            Email = "servicestack.mail+gpt3.5-turbo@gmail.com",
            DisplayName = "GPT-3.5 Turbo",
            EmailConfirmed = true,
            ProfilePath = "/profiles/gp/gpt3.5-turbo/gpt3.5-turbo.svg",
            Model = "gpt-3.5-turbo",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gpt-4o-mini",
            Email = "servicestack.mail+gpt-4o-mini@gmail.com",
            DisplayName = "GPT-4o mini",
            EmailConfirmed = true,
            ProfilePath = "/profiles/gp/gpt-4o-mini/gpt-4o-mini.svg",
            Model = "gpt-4o-mini",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "claude3-haiku",
            Email = "servicestack.mail+claude3-haiku@gmail.com",
            DisplayName = "Claude 3 Haiku",
            EmailConfirmed = true,
            ProfilePath = "/profiles/cl/claude3-haiku/claude3-haiku.svg",
            Model = "claude-3-haiku",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "claude3-sonnet",
            Email = "servicestack.mail+claude3-sonnet@gmail.com",
            DisplayName = "Claude 3 Sonnet",
            EmailConfirmed = true,
            ProfilePath = "/profiles/cl/claude3-sonnet/claude3-sonnet.svg",
            Model = "claude-3-sonnet",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "claude3-5-sonnet",
            Email = "servicestack.mail+claude3-5-sonnet@gmail.com",
            DisplayName = "Claude 3.5 Sonnet",
            EmailConfirmed = true,
            ProfilePath = "/profiles/cl/claude3-5-sonnet/claude3-5-sonnet.svg",
            Model = "claude-3-5-sonnet",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "claude3-7-sonnet",
            Email = "servicestack.mail+claude3-7-sonnet@gmail.com",
            DisplayName = "Claude 3.7 Sonnet",
            EmailConfirmed = true,
            ProfilePath = "/profiles/cl/claude3-sonnet/claude3-sonnet.svg",
            Model = "claude-3-7-sonnet",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "claude3-opus",
            Email = "servicestack.mail+claude3-opus@gmail.com",
            DisplayName = "Claude 3 Opus",
            EmailConfirmed = true,
            ProfilePath = "/profiles/cl/claude3-opus/claude3-opus.svg",
            Model = "claude-3-opus",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "command-r",
            Email = "servicestack.mail+command-r@gmail.com",
            DisplayName = "Command R 35B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/co/command-r/command-r.svg",
            Model = "command-r",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "command-r-plus",
            Email = "servicestack.mail+command-r-plus@gmail.com",
            DisplayName = "Command R+ 104B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/co/command-r-plus/command-r-plus.svg",
            Model = "command-r-plus",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "wizardlm",
            Email = "servicestack.mail+wizardlm@gmail.com",
            DisplayName = "WizardLM 8x22B",
            EmailConfirmed = true,
            ProfilePath = "/profiles/wi/wizardlm/wizardlm.png",
            Model = "wizardlm2:8x22b",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gemini-flash",
            Email = "servicestack.mail+gemini-flash@gmail.com",
            DisplayName = "Gemini Flash 2.0",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ge/gemini-flash/gemini-flash.svg",
            Model = "gemini-flash-2.0",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gemini-pro",
            Email = "servicestack.mail+gemini-pro@gmail.com",
            DisplayName = "Gemini Pro 2.0",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ge/gemini-pro/gemini-pro.svg",
            Model = "gemini-pro-2.0",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "gemini-pro-1.5",
            Email = "servicestack.mail+gemini-pro-1.5@gmail.com",
            DisplayName = "Gemini Pro 1.5",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ge/gemini-pro-1.5/gemini-pro-1.5.svg",
            Model = "gemini-pro-1.5",
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
            Email = "servicestack.mail+reddit@gmail.com",
            DisplayName = "Reddit",
            EmailConfirmed = true,
            ProfilePath = "/profiles/re/reddit/reddit.svg",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "discourse",
            Email = "servicestack.mail+discourse@gmail.com",
            DisplayName = "Discourse",
            EmailConfirmed = true,
            ProfilePath = "/profiles/di/discourse/discourse.svg",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "twitter",
            Email = "servicestack.mail+twitter@gmail.com",
            DisplayName = "Twitter",
            EmailConfirmed = true,
            ProfilePath = "/profiles/tw/twitter/twitter.svg",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "threads",
            Email = "servicestack.mail+threads@gmail.com",
            DisplayName = "threads",
            EmailConfirmed = true,
            ProfilePath = "/profiles/th/threads/threads.svg",
        }, "p@55wOrd");

        await EnsureUserAsync(new ApplicationUser
        {
            UserName = "mastodon",
            Email = "servicestack.mail+mastodon@gmail.com",
            DisplayName = "mastodon",
            EmailConfirmed = true,
            ProfilePath = "/profiles/ma/mastodon/mastodon.svg",
        }, "p@55wOrd");
    }
}
