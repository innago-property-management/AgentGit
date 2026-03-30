namespace AgentGit;

internal static class PrivateKeyValidator
{
    internal static ValidationResult Validate(string path)
    {
        if (!File.Exists(path))
        {
            return ValidationResult.Failure($"Private key file not found: {path}");
        }

        if (!OperatingSystem.IsWindows())
        {
            UnixFileMode mode = File.GetUnixFileMode(path);
            if ((mode & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead | UnixFileMode.OtherWrite)) != 0)
            {
                return ValidationResult.Failure(
                    $"Private key {path} has overly permissive file mode {mode}. Run: chmod 600 {path}");
            }
        }

        return ValidationResult.Success();
    }
}

internal readonly record struct ValidationResult(bool IsValid, string? Error)
{
    internal static ValidationResult Success() => new(true, null);
    internal static ValidationResult Failure(string error) => new(false, error);
}
