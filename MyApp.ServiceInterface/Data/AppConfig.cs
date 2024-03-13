namespace MyApp.Data;

public class AppConfig
{
    public static AppConfig Instance { get; } = new();
    public string LocalBaseUrl { get; set; }
    public string PublicBaseUrl { get; set; }
    public string CacheDir { get; set; }
    public string? GitPagesBaseUrl { get; set; }
    public List<ApplicationUser> ModelUsers { get; set; } = [];
    public ApplicationUser DefaultUser { get; set; } = new()
    {
        Model = "unknown",
        UserName = "unknown",
        ProfileUrl = "/img/profiles/user2.svg",
    };

    public ApplicationUser GetApplicationUser(string model)
    {
        var user = ModelUsers.FirstOrDefault(x => x.Model == model);
        return user ?? DefaultUser;
    }
}
