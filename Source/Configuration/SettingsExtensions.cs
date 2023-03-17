namespace FastBuild.Dashboard.Configuration;

internal static class SettingsExtensions
{
    public static void Save(this SettingsBase settings)
    {
        SettingsBase.Save(settings);
    }
}