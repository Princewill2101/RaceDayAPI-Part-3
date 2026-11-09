namespace RaceDayWeb.Services;

// One place for every session key so there are no typos scattered around.
public static class SessionKeys
{
    public const string UserId     = "UserId";
    public const string UserRole   = "UserRole";
    public const string UserName   = "UserName";
    public const string ProfilePic = "ProfilePictureUrl";
    public const string ApiCookie  = "ApiSessionCookie";

    public const string Organiser   = "Organiser";
    public const string Participant = "Participant";
}
