using MySqlConnector;

namespace PoOcr.Infrastructure.Configuration;

public static class OcrDatabaseConnectionString
{
    public static string? Resolve(string? configuredConnectionString = null)
    {
        DotEnv.Load();

        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
            return configuredConnectionString;

        var explicitConnectionString = Environment.GetEnvironmentVariable("PO_OCR_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
            return explicitConnectionString;

        var server = Environment.GetEnvironmentVariable("PO_OCR_DB_SERVER");
        var database = Environment.GetEnvironmentVariable("PO_OCR_DB_NAME");
        var user = Environment.GetEnvironmentVariable("PO_OCR_DB_USER");
        var password = Environment.GetEnvironmentVariable("PO_OCR_DB_PASSWORD");

        if (string.IsNullOrWhiteSpace(server)
            || string.IsNullOrWhiteSpace(database)
            || string.IsNullOrWhiteSpace(user)
            || password is null)
        {
            return null;
        }

        var builder = new MySqlConnectionStringBuilder
        {
            Server = server,
            Database = database,
            UserID = user,
            Password = password
        };

        return builder.ConnectionString;
    }

    private static class DotEnv
    {
        private const string FileName = ".env";
        private static bool loaded;

        public static void Load()
        {
            if (loaded)
                return;

            loaded = true;

            var path = FindFile(Directory.GetCurrentDirectory())
                ?? FindFile(AppContext.BaseDirectory);

            if (path is null)
                return;

            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                    continue;

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var key = trimmed[..separatorIndex].Trim();
                var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');

                if (string.IsNullOrWhiteSpace(key)
                    || Environment.GetEnvironmentVariable(key) is not null)
                {
                    continue;
                }

                Environment.SetEnvironmentVariable(key, value);
            }
        }

        private static string? FindFile(string startDirectory)
        {
            var directory = new DirectoryInfo(startDirectory);

            while (directory is not null)
            {
                var path = Path.Combine(directory.FullName, FileName);
                if (File.Exists(path))
                    return path;

                directory = directory.Parent;
            }

            return null;
        }
    }
}
