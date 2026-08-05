using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HajimaoDesktopShop.Release.Tests;

public sealed class GitHubPublishingWorkflowTests
{
    private readonly RepositoryRoot _root = RepositoryRoot.Locate();

    [Theory]
    [InlineData("Available", "git")]
    [InlineData("Unavailable", "api")]
    public void Plan_UsesOneBoundedProbeAndSelectsExpectedTransport(
        string probeOverride,
        string expectedTransport)
    {
        using var plan = RunPlan(probeOverride);
        var root = plan.RootElement;

        Assert.Equal(expectedTransport, root.GetProperty("transport").GetString());
        Assert.Equal(5, root.GetProperty("probeSeconds").GetInt32());
        Assert.Equal(1, root.GetProperty("normalPushAttempts").GetInt32());
    }

    [Fact]
    public void Script_VerifiesExactGitObjectsAndUsesBinarySafeJsonInput()
    {
        var script = File.ReadAllText(_root.File("scripts", "publish-github-branch.ps1"));

        Assert.Contains("cat-file blob", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("StandardInput.BaseStream", script, StringComparison.Ordinal);
        Assert.Contains("base_tree", script, StringComparison.Ordinal);
        Assert.Contains("tree = $treeEntries.ToArray()", script, StringComparison.Ordinal);
        Assert.DoesNotContain("tree = @($treeEntries)", script, StringComparison.Ordinal);
        Assert.Contains("Local blob SHA", script, StringComparison.Ordinal);
        Assert.Contains("Local tree SHA", script, StringComparison.Ordinal);
        Assert.DoesNotContain("gh auth token", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(new Regex(@"(?im)&\s*git\s+push\b"), script);

        var pushFunctionStart = script.IndexOf(
            "function Invoke-GitPushOnce",
            StringComparison.Ordinal);
        var nextFunctionStart = script.IndexOf(
            "function ",
            pushFunctionStart + 1,
            StringComparison.Ordinal);
        Assert.True(pushFunctionStart >= 0);
        Assert.True(nextFunctionStart > pushFunctionStart);
        var pushFunction = script[pushFunctionStart..nextFunctionStart];
        Assert.Contains("ProcessStartInfo", pushFunction, StringComparison.Ordinal);
        Assert.Contains("RedirectStandardError", pushFunction, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(pushFunction, @"push --set-upstream"));
    }

    private JsonDocument RunPlan(string probeOverride)
    {
        var scriptPath = _root.File("scripts", "publish-github-branch.ps1");
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-PlanOnly");
        startInfo.ArgumentList.Add("-ProbeOverride");
        startInfo.ArgumentList.Add(probeOverride);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start PowerShell.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(
            process.ExitCode == 0,
            $"Publishing plan failed with exit code {process.ExitCode}: {error}");
        return JsonDocument.Parse(output);
    }
}
