using System.Net;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using ServiceStack.Blazor;
using MyApp.Components;
using MyApp.Data;
using MyApp.Components.Account;
using ServiceStack.IO;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var config = builder.Configuration;

// Add services to the container.
services.AddRazorComponents();

services.AddCascadingAuthenticationState();
services.AddScoped<IdentityUserAccessor>();
services.AddScoped<IdentityRedirectManager>();
services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

services.AddAuthorization();
services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies(options =>
    {
        options.DisableRedirectsForApis();
        options.ApplicationCookie?.Configure(x => x.ExpireTimeSpan = TimeSpan.FromDays(400));
    });
services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("App_Data"));

services.AddDatabaseDeveloperPageExceptionFilter();

services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyz0123456789-.";
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
// Uncomment to send emails with SMTP, configure SMTP with "SmtpConfig" in appsettings.json
services.AddSingleton<IEmailSender<ApplicationUser>, EmailSender>();
services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AdditionalUserClaimsPrincipalFactory>();

var baseUrl = builder.Configuration["ApiBaseUrl"] ??
    (builder.Environment.IsDevelopment() ? "https://localhost:5001" : "http://" + IPAddress.Loopback);
services.AddScoped(c => new HttpClient { BaseAddress = new Uri(baseUrl) });
services.AddBlazorServerIdentityApiClient(baseUrl);
services.AddLocalStorage();

services.AddServiceStack(typeof(MyServices).Assembly);

services.AddOutputCache(options =>
{
    // Use default cache of 60s (for non Authenticated Users)
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(60)));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions {
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});  


//TODO: Remove after bing search no longer includes these links
string[] redirectPosts = [
    "net8-blazor-template",
    "net8-identity-auth",
    "net8-docker-containers",
    "system-text-json-apis",
    "openapi-v3",
];
foreach (var slug in redirectPosts)
{
    app.MapGet($"/posts/{slug}", async ctx => ctx.Response.Redirect($"https://servicestack.net/posts/{slug}", permanent:true));
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// prerender with: `$ npm run prerender` 
AppTasks.Register("prerender", args =>
{
    var appHost = (AppHost)HostContext.AppHost;
    var distDir = appHost.ContentRootDirectory.RealPath.CombineWith("wwwroot");
    var appConfig = AppConfig.Instance;
    appHost.PrerenderSitemapAsync(appHost, distDir, appConfig.PublicBaseUrl).GetAwaiter().GetResult();
});

app.UseServiceStack(new AppHost(), options => {
    options.MapEndpoints();
});

BlazorConfig.Set(new()
{
    Services = app.Services,
    JSParseObject = JS.ParseObject,
    IsDevelopment = app.Environment.IsDevelopment(),
    EnableLogging = app.Environment.IsDevelopment(),
    EnableVerboseLogging = app.Environment.IsDevelopment(),
    AssetsBasePath = app.Environment.IsDevelopment() ? null : "https://assets.pvq.app"
});

app.Run();
