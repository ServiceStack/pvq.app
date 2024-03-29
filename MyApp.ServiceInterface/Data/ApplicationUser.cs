using Microsoft.AspNetCore.Identity;
using ServiceStack.DataAnnotations;

namespace MyApp.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
[Alias("AspNetUsers")]
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public string? ProfilePath { get; set; }
    public string? Model { get; set; }
    public string? LastLoginIp { get; set; }
    public DateTime? LastLoginDate { get; set; }
}
