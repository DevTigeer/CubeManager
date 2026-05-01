using System.Reflection;

namespace CubeManager.Helpers;

public static class AppVersionHelper
{
    public static string CurrentVersion
    {
        get
        {
            var assembly = Assembly.GetEntryAssembly() ?? typeof(AppVersionHelper).Assembly;
            var version = assembly.GetName().Version;
            if (version == null)
                return "0.0.0";

            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
