using System.IO;

namespace HajimaoDesktopShop.Desktop.Tests.Windows;

public sealed class NoGameTimePresentationTests
{
    [Fact]
    public void PlayerFacingSources_DoNotExposeAClockDaypartOrShiftSystem()
    {
        var root = LocateRepositoryRoot();
        var sourceRoots = new[]
        {
            Path.Combine(root, "src", "HajimaoDesktopShop.Desktop"),
            Path.Combine(root, "src", "HajimaoDesktopShop.Rendering")
        };
        var sources = sourceRoots
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var forbidden = new[]
        {
            "GameTimeText",
            "FormatGameTime",
            "DayProgress",
            "DayCountdown",
            "08:00",
            "16:00",
            "日结",
            "经营日",
            "班次",
            "排班",
            "昨日",
            "日报",
            "分钟",
            "小时",
            "天回本"
        };

        foreach (var source in sources)
        {
            var text = File.ReadAllText(source);
            foreach (var value in forbidden)
            {
                Assert.DoesNotContain(value, text, StringComparison.Ordinal);
            }
        }

        var managementXaml = File.ReadAllText(Path.Combine(
            root,
            "src",
            "HajimaoDesktopShop.Desktop",
            "Windows",
            "ManagementWindow.xaml"));
        Assert.DoesNotContain("ReportProgress", managementXaml, StringComparison.Ordinal);
        Assert.Contains("IdleFeedback.SessionDropText", managementXaml, StringComparison.Ordinal);
    }

    private static string LocateRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "HajimaoDesktopShop.slnx")))
        {
            directory = directory.Parent;
        }

        return Assert.IsType<DirectoryInfo>(directory).FullName;
    }
}
