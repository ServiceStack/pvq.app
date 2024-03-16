using ServiceStack;

namespace MyApp.ServiceModel;

[Tag(Tag.User)]
[ValidateIsAuthenticated]
public class UpdateUserProfile : IPost, IReturn<UpdateUserProfileResponse>
{
}

public class UpdateUserProfileResponse
{
    public ResponseStatus ResponseStatus { get; set; }
}

[Route("/avatar/{UserName}", "GET")]
public class GetUserAvatar : IGet, IReturn<byte[]>
{
    public string UserName { get; set; }
}
