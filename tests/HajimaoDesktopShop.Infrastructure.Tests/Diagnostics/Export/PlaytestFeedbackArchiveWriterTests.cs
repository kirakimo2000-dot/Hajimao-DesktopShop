using System.IO.Compression;
using System.Text.Json;
using HajimaoDesktopShop.Application.Diagnostics.Export;
using HajimaoDesktopShop.Infrastructure.Diagnostics.Export;

namespace HajimaoDesktopShop.Infrastructure.Tests.Diagnostics.Export;

public sealed class PlaytestFeedbackArchiveWriterTests
{
    [Fact]
    public void Write_CreatesZipWithReadmeAndCamelCaseIndentedJson()
    {
        using var directory = new TemporaryDirectory();
        var report = Report();

        var zipPath = PlaytestFeedbackArchiveWriter.Write(directory.Path, report);

        Assert.Equal(Path.GetFullPath(zipPath), zipPath);
        Assert.Matches(@"HajimaoDesktopShop-Feedback-\d{8}-\d{6}\.zip$", Path.GetFileName(zipPath));

        using var archive = ZipFile.OpenRead(zipPath);
        Assert.Equal(new[] { "feedback.json", "README.txt" }, archive.Entries.Select(entry => entry.FullName).OrderBy(name => name));

        var json = ReadEntry(archive, "feedback.json");
        Assert.Contains("\n  \"product\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Product\"", json, StringComparison.Ordinal);

        var deserialized = JsonSerializer.Deserialize<PlaytestFeedbackReport>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(deserialized);
        Assert.Equal(report.Product, deserialized.Product);
        Assert.Equal(report.Version, deserialized.Version);
        Assert.Equal(report.SaveSchemaVersion, deserialized.SaveSchemaVersion);
        Assert.Equal(report.CreatedAtUtc, deserialized.CreatedAtUtc);
        Assert.Equal(report.GameMinute, deserialized.GameMinute);
        Assert.Equal(report.PlayerLevel, deserialized.PlayerLevel);
        Assert.Equal(report.CashCents, deserialized.CashCents);
        Assert.Equal(report.OpenStoreCount, deserialized.OpenStoreCount);
        Assert.Equal(report.EmployeeCount, deserialized.EmployeeCount);
        Assert.Equal(report.LastCompletedDayNumber, deserialized.LastCompletedDayNumber);
        Assert.Equal(report.Stores, deserialized.Stores);
        Assert.Equal(report.DiagnosticEvents, deserialized.DiagnosticEvents);

        var readme = ReadEntry(archive, "README.txt");
        Assert.Contains("汇总经营数据", readme, StringComparison.Ordinal);
        Assert.Contains("已脱敏诊断事件", readme, StringComparison.Ordinal);
        Assert.Contains("不包含存档数据库", readme, StringComparison.Ordinal);
        Assert.Contains("原始日志", readme, StringComparison.Ordinal);
        Assert.Contains("个人或机器路径", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_DoesNotOverwriteExistingArchiveNameAndUsesUniqueFallback()
    {
        using var directory = new TemporaryDirectory();
        var collisionPath = ReserveCurrentSecondCollision(directory.Path);
        var originalContent = Guid.NewGuid().ToByteArray();
        File.WriteAllBytes(collisionPath, originalContent);

        var zipPath = PlaytestFeedbackArchiveWriter.Write(directory.Path, Report());

        Assert.NotEqual(collisionPath, zipPath);
        Assert.True(File.Exists(zipPath));
        Assert.Equal(originalContent, File.ReadAllBytes(collisionPath));
        Assert.Equal(2, Directory.GetFiles(directory.Path, "*.zip").Length);
    }

    [Fact]
    public void Write_CreatesOnlyFinalZipFiles()
    {
        using var directory = new TemporaryDirectory();

        PlaytestFeedbackArchiveWriter.Write(directory.Path, Report());
        PlaytestFeedbackArchiveWriter.Write(directory.Path, Report());

        Assert.All(
            Directory.EnumerateFiles(directory.Path),
            path => Assert.EndsWith(".zip", path, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Write_ValidatesArgumentsWithoutLeavingTempFiles()
    {
        using var directory = new TemporaryDirectory();

        Assert.Throws<ArgumentNullException>(() => PlaytestFeedbackArchiveWriter.Write(null!, Report()));
        Assert.Throws<ArgumentException>(() => PlaytestFeedbackArchiveWriter.Write("", Report()));
        Assert.Throws<ArgumentException>(() => PlaytestFeedbackArchiveWriter.Write("   ", Report()));
        Assert.Throws<ArgumentNullException>(() => PlaytestFeedbackArchiveWriter.Write(directory.Path, null!));

        Assert.Empty(Directory.EnumerateFiles(directory.Path));
    }

    private static string ReserveCurrentSecondCollision(string directory)
    {
        var nextSecond = DateTimeOffset.UtcNow.AddSeconds(1);
        while (DateTimeOffset.UtcNow < nextSecond)
        {
            Thread.Sleep(10);
        }

        return Path.Combine(
            directory,
            $"HajimaoDesktopShop-Feedback-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.zip");
    }

    private static string ReadEntry(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name);
        Assert.NotNull(entry);
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static PlaytestFeedbackReport Report() =>
        new(
            "Hajimao DesktopShop",
            "0.1.21",
            6,
            DateTimeOffset.Parse("2026-08-11T09:30:00Z"),
            1234,
            5,
            98765,
            1,
            2,
            7,
            Array.AsReadOnly(new[]
            {
                new PlaytestFeedbackStoreSummary(
                    "north",
                    1,
                    2,
                    3,
                    10_000,
                    3_000,
                    1_000,
                    400,
                    1_600,
                    8,
                    2,
                    900),
            }),
            Array.AsReadOnly(new[]
            {
                new SanitizedDiagnosticEvent(
                    DateTimeOffset.Parse("2026-08-11T09:00:00Z"),
                    "INF",
                    "application.started"),
            }));

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"hajimao-feedback-archive-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
