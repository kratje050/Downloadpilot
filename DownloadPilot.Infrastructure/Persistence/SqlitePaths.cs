namespace DownloadPilot.Infrastructure.Persistence;

public static class SqlitePaths
{
    public static string DataDirectory
    {
        get
        {
            var basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DownloadPilot");
            Directory.CreateDirectory(basePath);
            return basePath;
        }
    }

    public static string DatabasePath => Path.Combine(DataDirectory, "downloadpilot.db");

    public static string LogPath => Path.Combine(DataDirectory, "downloadpilot.log");
}
