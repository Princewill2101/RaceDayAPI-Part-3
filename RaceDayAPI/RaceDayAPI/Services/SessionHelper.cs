namespace RaceDayAPI.Services;

// Simple helper so every controller can read the session the same way.
// The session stores the logged-in user's id and role after login.
public static class SessionHelper
{
    private const string UserIdKey = "UserId";
    private const string RoleKey   = "UserRole";

    // Save the user's id and role into the session on login
    public static void SetUser(ISession session, int userId, string role)
    {
        session.SetInt32(UserIdKey, userId);
        session.SetString(RoleKey, role);
    }

    // Read the user's id from the session (null = not logged in)
    public static int? GetUserId(ISession session)
        => session.GetInt32(UserIdKey);

    // Read the user's role from the session (null = not logged in)
    public static string? GetRole(ISession session)
        => session.GetString(RoleKey);

    // Remove all session data on logout
    public static void ClearUser(ISession session)
        => session.Clear();

    // True if a user is currently logged in
    public static bool IsLoggedIn(ISession session)
        => session.GetInt32(UserIdKey).HasValue;

    // True if the logged-in user is an Organiser
    public static bool IsOrganiser(ISession session)
        => session.GetString(RoleKey) == "Organiser";

    // True if the logged-in user is a Participant
    public static bool IsParticipant(ISession session)
        => session.GetString(RoleKey) == "Participant";
}
