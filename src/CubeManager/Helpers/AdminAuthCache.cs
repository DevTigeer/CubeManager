namespace CubeManager.Helpers;

public static class AdminAuthCache
{
    private static DateTime _expiresAt = DateTime.MinValue;

    public static void SetAuthenticated(int minutes = 5) =>
        _expiresAt = DateTime.Now.AddMinutes(minutes);

    public static bool IsValid() => DateTime.Now < _expiresAt;

    public static void Clear() => _expiresAt = DateTime.MinValue;
}
