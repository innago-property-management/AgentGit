namespace AgentGit;

internal static class AskPassScriptManager
{
    internal static string Create()
    {
        if (OperatingSystem.IsWindows())
        {
            string path = Path.Combine(Path.GetTempPath(), $"agentgit-askpass-{Environment.ProcessId}.cmd");
            File.WriteAllText(path, "@echo %AGENTGIT_TOKEN%\r\n");
            return path;
        }
        else
        {
            string path = Path.Combine(Path.GetTempPath(), $"agentgit-askpass-{Environment.ProcessId}.sh");
            File.WriteAllText(path, "#!/bin/sh\necho \"$AGENTGIT_TOKEN\"\n");
#pragma warning disable CA1416 // Platform compatibility - guarded by IsWindows check above
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserExecute);
#pragma warning restore CA1416
            return path;
        }
    }

    internal static void Cleanup(string scriptPath)
    {
        try
        {
            File.Delete(scriptPath);
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}
