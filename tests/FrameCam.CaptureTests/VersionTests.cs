using System.Diagnostics;
using FrameCam.UI;

namespace FrameCam.CaptureTests;

/// <summary>The version FrameCam is built with (spec 8.5): 1.0 and the number of commits.</summary>
public class VersionTests
{
    [GitFact]
    public void The_last_number_of_the_version_is_the_number_of_commits()
    {
        var version = typeof(SettingsForm).Assembly.GetName().Version!;

        Assert.Equal((1, 0), (version.Major, version.Minor));
        Assert.Equal(CommitCount(), version.Build);
    }

    /// <summary>Commits on the checked-out branch of the repository these tests run in; null without git.</summary>
    internal static int? CommitCount()
    {
        try
        {
            using var git = Process.Start(new ProcessStartInfo("git", "rev-list --count HEAD")
            {
                WorkingDirectory = AppContext.BaseDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (git is null)
                return null;
            string output = git.StandardOutput.ReadToEnd();
            git.WaitForExit();
            return git.ExitCode == 0 && int.TryParse(output.Trim(), out int count) ? count : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null; // git is not installed
        }
    }
}

/// <summary>A [Fact] that is skipped when git is missing or the tests do not run inside the repository.</summary>
public sealed class GitFactAttribute : FactAttribute
{
    public GitFactAttribute()
    {
        if (VersionTests.CommitCount() is null)
            Skip = "git not found, or not inside the FrameCam repository";
    }
}
