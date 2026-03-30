namespace AgentGit;

internal interface IGitProcessRunner
{
    string GetCurrentBranch(string repoPath);
    string GetRemoteUrl(string repoPath);
    int Commit(string repoPath, string botName, string botEmail, string[] args);
    int Push(string repoPath, string botName, string botEmail, string token, string owner, string repo, string branch);
}
