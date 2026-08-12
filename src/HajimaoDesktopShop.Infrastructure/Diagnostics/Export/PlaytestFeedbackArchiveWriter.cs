using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using HajimaoDesktopShop.Application.Diagnostics.Export;

namespace HajimaoDesktopShop.Infrastructure.Diagnostics.Export;

public static class PlaytestFeedbackArchiveWriter
{
    private const string FilenamePrefix = "HajimaoDesktopShop-Feedback";
    private const int MaximumCollisionRetries = 32;

    public static JsonSerializerOptions JsonOptions => new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    public static string Write(string outputDirectory, PlaytestFeedbackReport report)
    {
        if (outputDirectory is null)
        {
            throw new ArgumentNullException(nameof(outputDirectory));
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
        }

        ArgumentNullException.ThrowIfNull(report);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var tempPath = Path.Combine(
            fullOutputDirectory,
            $"{FilenamePrefix}-{Guid.NewGuid():N}.tmp");

        try
        {
            using (var archive = ZipFile.Open(tempPath, ZipArchiveMode.Create))
            {
                WriteTextEntry(archive, "README.txt", CreateReadme());
                WriteTextEntry(archive, "feedback.json", JsonSerializer.Serialize(report, JsonOptions));
            }

            var destinationPath = MoveToUniqueDestination(tempPath, fullOutputDirectory);
            return destinationPath;
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    private static string MoveToUniqueDestination(string tempPath, string outputDirectory)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var destinationPath = CreateDestinationPath(outputDirectory, timestamp, suffix: null);
        if (TryMove(tempPath, destinationPath))
        {
            return destinationPath;
        }

        for (var attempt = 0; attempt < MaximumCollisionRetries; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            destinationPath = CreateDestinationPath(outputDirectory, timestamp, suffix);
            if (TryMove(tempPath, destinationPath))
            {
                return destinationPath;
            }
        }

        throw new IOException(
            $"Could not create a unique feedback archive name after {MaximumCollisionRetries} collision retries.");
    }

    private static bool TryMove(string tempPath, string destinationPath)
    {
        try
        {
            File.Move(tempPath, destinationPath, overwrite: false);
            return true;
        }
        catch (IOException) when (File.Exists(destinationPath))
        {
            return false;
        }
    }

    private static string CreateDestinationPath(string outputDirectory, string timestamp, string? suffix)
    {
        var filename = suffix is null
            ? $"{FilenamePrefix}-{timestamp}.zip"
            : $"{FilenamePrefix}-{timestamp}-{suffix}.zip";
        return Path.Combine(outputDirectory, filename);
    }

    private static void WriteTextEntry(ZipArchive archive, string entryName, string text)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        writer.Write(text);
    }

    private static string CreateReadme() =>
        """
        此反馈包包含汇总经营数据和已脱敏诊断事件，用于定位试玩期间的经营进度和应用状态。

        此反馈包不包含存档数据库、原始日志、个人或机器路径，也不包含员工、顾客或商品的明细身份信息。
        """;
}
