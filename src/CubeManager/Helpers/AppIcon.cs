using System.Reflection;

namespace CubeManager.Helpers;

public static class AppIcon
{
    private static Icon? _cached;

    public static Icon? Get()
    {
        if (_cached is not null) return _cached;
        try
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("CubeManager.Resources.cube.ico");
            if (stream is null) return null;
            _cached = new Icon(stream);
            return _cached;
        }
        catch { return null; }
    }

    public static void Apply(Form form)
    {
        var icon = Get();
        if (icon is not null)
        {
            form.Icon = icon;
            form.ShowIcon = true;
        }
    }
}
