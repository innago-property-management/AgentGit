using System.Diagnostics;

namespace AgentGit;

internal sealed class GitProcessRunner : IGitProcessRunner
{
    public string GetCurrentBranch(string repoPath)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        psi.ArgumentList.Add("rev-parse");
        psi.ArgumentList.Add("--abbrev-ref");
        psi.ArgumentList.Add("HEAD");

        using Process? process = Process.Start(psi);
        if (process is null)
        {
            return "main";
        }

        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd().Trim();
        stderrTask.GetAwaiter().GetResult();
        process.WaitForExit();
        return output;
    }

    public string GetRemoteUrl(string repoPath)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = repoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        psi.ArgumentList.Add("remote");
        psi.ArgumentList.Add("get-url");
        psi.ArgumentList.Add("origin");

        using Process? process = Process.Start(psi);
        if (process is null)
        {
            return "";
        }

        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd().Trim();
        stderrTask.GetAwaiter().GetResult();
        process.WaitForExit();
        return output;
    }

    public int Commit(string repoPath, string botName, string botEmail, string[] args)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = repoPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            EnvironmentVariables =
            {
                ["GIT_AUTHOR_NAME"] = botName,
                ["GIT_AUTHOR_EMAIL"] = botEmail,
                ["GIT_COMMITTER_NAME"] = botName,
                ["GIT_COMMITTER_EMAIL"] = botEmail,
                ["GIT_ASKPASS"] = "echo",
            },
        };

        psi.ArgumentList.Add("commit");
        psi.ArgumentList.Add("--no-gpg-sign");
        foreach (string arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using Process? process = Process.Start(psi);
        if (process is null)
        {
            return 1;
        }

        // Read stderr async to avoid deadlock when git fills the buffer
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = stderrTask.GetAwaiter().GetResult();
        process.WaitForExit();
        return process.ExitCode;
    }

    public int Push(string repoPath, string botName, string botEmail, string token, string owner, string repo, string branch)
    {
        string askPassScript = AskPassScriptManager.Create();

        try
        {
            var psi = new ProcessStartInfo("git")
            {
                WorkingDirectory = repoPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                EnvironmentVariables =
                {
                    ["GIT_AUTHOR_NAME"] = botName,
                    ["GIT_AUTHOR_EMAIL"] = botEmail,
                    ["GIT_COMMITTER_NAME"] = botName,
                    ["GIT_COMMITTER_EMAIL"] = botEmail,
                    ["AGENTGIT_TOKEN"] = token,
                    ["GIT_ASKPASS"] = askPassScript,
                    ["GIT_TERMINAL_PROMPT"] = "0",
                },
            };

            string pushUrl = $"https://x-access-token@github.com/{owner}/{repo}.git";
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add("credential.helper=");
            psi.ArgumentList.Add("push");
            psi.ArgumentList.Add(pushUrl);
            psi.ArgumentList.Add(branch);

            using Process? process = Process.Start(psi);
            if (process is null)
            {
                return 1;
            }

            // Read stderr async to avoid deadlock — git push writes progress to stderr
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = stderrTask.GetAwaiter().GetResult();
            process.WaitForExit();
            return process.ExitCode;
        }
        finally
        {
            AskPassScriptManager.Cleanup(askPassScript);
        }
    }
}
