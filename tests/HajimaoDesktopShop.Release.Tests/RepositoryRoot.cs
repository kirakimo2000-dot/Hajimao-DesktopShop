namespace HajimaoDesktopShop.Release.Tests;

internal sealed class RepositoryRoot
{
    private RepositoryRoot(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public static RepositoryRoot Locate()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "HajimaoDesktopShop.slnx")))
            {
                return new RepositoryRoot(directory.FullName);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Hajimao DesktopShop repository root.");
    }

    public string File(params string[] segments) =>
        System.IO.Path.Combine([Path, .. segments]);
}
