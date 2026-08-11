using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using HajimaoDesktopShop.Application.Diagnostics.Export;

namespace HajimaoDesktopShop.Infrastructure.Diagnostics.Export;

public static class PlaytestFeedbackArchiveWriter
{
    private const string FilenamePrefix = "HajimaoDesktopShop-Feedback";

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

            var destinationPath = CreateDestinationPath(fullOutputDirectory);
            File.Move(tempPath, destinationPath, overwrite: false);
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

    private static string CreateDestinationPath(string outputDirectory)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var destinationPath = Path.Combine(outputDirectory, $"{FilenamePrefix}-{timestamp}.zip");
        if (!File.Exists(destinationPath))
        {
            return destinationPath;
        }

        var suffix = Guid.NewGuid().ToString("N")[..8];
        return Path.Combine(outputDirectory, $"{FilenamePrefix}-{timestamp}-{suffix}.zip");
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
