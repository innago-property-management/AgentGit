using System.IO;

namespace AgentGit;

internal static class AskPassScriptManager
{
    internal static string Create()
    {
        if (OperatingSystem.IsWindows())
        {
            string path = Path.Combine(Path.GetTempPath(), $"agentgit-askpass-{Environment.ProcessId}.cmd");
            // Remove stale file from prior crashed run (PID reuse)
            try { File.Delete(path); } catch { }
            File.WriteAllText(path, "@echo %AGENTGIT_TOKEN%\r\n");
            return path;
        }
        else
        {
            string path = Path.Combine(Path.GetTempPath(), $"agentgit-askpass-{Environment.ProcessId}.sh");
            // Remove stale file from prior crashed run (PID reuse)
            try { File.Delete(path); } catch { }
            // Atomic create with correct permissions — no TOCTOU window
#pragma warning disable CA1416 // Platform compatibility - guarded by IsWindows check above
            using (var fs = new FileStream(path, new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserExecute,
            }))
            {
                using var writer = new StreamWriter(fs);
                writer.Write("#!/bin/sh\necho \"$AGENTGIT_TOKEN\"\n");
            }
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
