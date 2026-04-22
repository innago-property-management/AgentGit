# Security Review — AgentGit Open-Source Readiness

**Date:** 2026-03-30
**Reviewer:** Claude Code (security audit agent)
**Scope:** All source, config, scripts, CI workflows, .gitignore, git history
**Verdict:** **3 HIGH, 3 MEDIUM, 4 LOW findings** — HIGH items must be resolved before public release

---

## CRITICAL / HIGH Severity

### H1: `appsettings.json` committed to git with real credentials

**File:** `src/AgentGit/appsettings.json` (tracked in git)
**Also:** `bin/appsettings.json` (copy in build output, also gitignored but present locally)

The production `appsettings.json` is tracked by git and committed at HEAD, containing:
- Real GitHub App **Client ID**: `Iv23li8yGhLhlDV5cm0A`
- Real **App ID**: `3167794`
- Local filesystem path to private key: `/Users/christopheranderson/Documents/Projects/stand-sure/stand-sure-ai.2026-03-23.private-key.pem`

The `.gitignore` has `appsettings.json` listed, but the file was committed *before* the gitignore rule was added (commits `b02839f` and `46fe6a7`). Git continues tracking it because `.gitignore` only prevents *new* untracked files from being added.

**Impact:** When the repo goes public, anyone can see the Client ID and App ID. While the Client ID alone is not a secret (it's the JWT issuer, and the private key is needed to generate valid JWTs), exposing the exact App ID and filesystem path is unnecessary information leakage. More importantly, if a contributor forks and pushes, they inherit the tracked file.

**Remediation:**
1. Remove from tracking: `git rm --cached src/AgentGit/appsettings.json`
2. Commit the removal
3. Verify `.gitignore` rule `appsettings.json` is catching it (it is — line 8)
4. Consider whether git history needs rewriting (BFG or `git filter-repo`) to remove the Client ID from history, depending on your threat model. The Client ID is semi-public (visible in JWT `iss` claims anyway), so this is optional.

**Severity: HIGH** — Committed credentials in a repo about to go public.

---

### H2: Private key material kept in memory without explicit clearing

**File:** `src/AgentGit/JwtGenerator.cs:12-13`

```csharp
string pemText = File.ReadAllText(privateKeyPath);
using var rsa = RSA.Create();
rsa.ImportFromPem(pemText);
```

The PEM text is read into a managed `string`, which is immutable and cannot be zeroed. The `RSA` object is properly disposed via `using`, but the `pemText` string will remain in managed heap memory until GC collects it (and even then, the memory may not be zeroed).

**Impact:** In a long-running or compromised process, an attacker with memory access could extract the private key from the managed heap. For a short-lived CLI tool this is lower risk, but it's a defense-in-depth gap.

**Remediation:**
- Read into a `byte[]` instead of `string`, use `rsa.ImportFromPem(Encoding.UTF8.GetString(bytes))`, then `Array.Clear(bytes)` and/or use `CryptographicOperations.ZeroMemory(bytes)`.
- Alternatively, use `rsa.ImportFromEncryptedPem()` if the key is passphrase-protected.
- For Native AOT, the `string` concern is somewhat mitigated by the short process lifetime, but the fix is trivial.

**Severity: HIGH** — Private key in unzeroed memory. Low exploitability for a CLI, but a standard cryptographic hygiene requirement.

---

### H3: JWT window allows clock-skew abuse (10-minute lifetime, 60-second backdating)

**File:** `src/AgentGit/JwtGenerator.cs:16-17`

```csharp
long iat = DateTimeOffset.UtcNow.AddSeconds(-60).ToUnixTimeSeconds();
long exp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
```

The JWT is valid for ~11 minutes (60s backdating + 10m forward). GitHub's maximum is 10 minutes. The backdating is standard practice for clock skew, but the effective window is 11 minutes total.

**Impact:** If a JWT is leaked (e.g., logged, intercepted), the attacker has up to 11 minutes to use it to obtain installation tokens. This follows GitHub's own recommendation, but is noted for completeness.

**Remediation:** This is acceptable as-is. GitHub enforces the 10-minute maximum on `exp - iat`, and the 60-second backdate is their documented recommendation. No action required unless you want to tighten the window.

**Severity: HIGH (informational)** — Downgraded to informational; follows GitHub best practices. Included for completeness.

---

## MEDIUM Severity

### M1: Token passed via environment variable to child process (`AGENTGIT_TOKEN`)

**File:** `src/AgentGit/GitProcessRunner.cs:91`

```csharp
["AGENTGIT_TOKEN"] = token,
```

The installation token is passed as an environment variable to the `git push` child process. This is the correct pattern (better than CLI args), but environment variables are readable via `/proc/<pid>/environ` on Linux by processes with the same UID, and can be logged by monitoring tools.

**Impact:** On shared systems, another process running as the same user could read the token from `/proc`. The token is short-lived (1 hour), which limits the blast radius.

**Remediation:**
- The current approach (env var + `GIT_ASKPASS` script) is the industry standard for git credential passing. This is acceptable.
- For defense-in-depth, consider using a named pipe or file descriptor instead of an env var, but this adds significant complexity for marginal gain.
- Document the threat model: "tokens are passed via environment variables to child git processes and are short-lived (~1 hour)."

**Severity: MEDIUM** — Acceptable pattern, but worth documenting the threat model.

---

### M2: AskPass temp script race condition (TOCTOU)

**File:** `src/AgentGit/AskPassScriptManager.cs:15-18`

```csharp
string path = Path.Combine(Path.GetTempPath(), $"agentgit-askpass-{Environment.ProcessId}.sh");
File.WriteAllText(path, "#!/bin/sh\necho \"$AGENTGIT_TOKEN\"\n");
File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserExecute);
```

There is a window between `WriteAllText` (creates file with default umask, potentially world-readable) and `SetUnixFileMode` (restricts permissions). During this window, another process could read the script content.

The script itself doesn't contain the token (it echoes an env var), so the direct exposure is the *existence* of the askpass mechanism, not the token itself.

**Impact:** Low in practice — the script only echoes an env var, and the race window is microseconds. But on a shared system with an attacker watching `/tmp`, they could observe the askpass pattern.

**Remediation:**
- Set umask before creating the file: wrap in `umask 0077` equivalent, or use `FileStream` with explicit `UnixFileMode` parameter (available in .NET 7+):
  ```csharp
  using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write,
      FileShare.None, 4096, FileOptions.None);
  // .NET doesn't expose UnixFileMode on FileStream constructor in all versions,
  // but you can use PInvoke or create with mkstemp pattern
  ```
- Alternatively, use `Path.GetTempFileName()` which creates with 0600 on most Unix systems, then overwrite content.

**Severity: MEDIUM** — Race condition on temp file permissions, but script doesn't contain secrets directly.

---

### M3: Push URL contains no token but reveals repo structure

**File:** `src/AgentGit/GitProcessRunner.cs:97`

```csharp
string pushUrl = $"https://x-access-token@github.com/{owner}/{repo}.git";
```

The push URL uses `x-access-token` as the username with no password in the URL itself (the token comes via `GIT_ASKPASS`). This is correct — the token is NOT in the URL. However, the URL is passed as a process argument and visible in `ps aux`.

**Impact:** Minimal. The URL reveals `owner/repo` (already public information for a public repo) and the fact that `x-access-token` is used (a known GitHub pattern). No actual secret exposure.

**Remediation:** No action required. This is the correct pattern. The `ArgumentList` API prevents shell injection.

**Severity: MEDIUM (informational)** — Noted for completeness. No remediation needed.

---

## LOW Severity

### L1: `bin/` directory contains published binary and appsettings.json

**Files:** `bin/appsettings.json`, `bin/AgentGit` (native binary)

The `bin/` directory is in `.gitignore` (line 1), so these are not tracked. However, the `bin/appsettings.json` is a copy of the real config (created by `dotnet publish`).

**Impact:** If someone adds `bin/` to git accidentally (e.g., `git add -f`), the appsettings.json with real credentials would be committed.

**Remediation:**
- The `.gitignore` rule is sufficient. No action needed.
- Consider adding `bin/appsettings.json` to a `.gitignore` comment explaining why.

**Severity: LOW**

---

### L2: Gitleaks pre-commit hook uses Docker (may not run locally)

**File:** `.pre-commit-config.yaml:3-8`

```yaml
- repo: https://github.com/gitleaks/gitleaks
  rev: v8.24.2
  hooks:
    - id: gitleaks-docker
```

The Gitleaks hook uses the `docker_image` language, requiring Docker to be installed. If a contributor doesn't have Docker, the hook silently fails or errors out, and they could commit secrets.

**Impact:** The secret scanning safety net may not be active for all contributors.

**Remediation:**
- Consider using `gitleaks` (non-Docker) entry instead: `id: gitleaks` with `language: golang` or pre-built binary.
- Add a CI workflow step running Gitleaks as a backup (defense-in-depth).

**Severity: LOW**

---

### L3: No input validation on `owner`/`repo` before API calls

**File:** `src/AgentGit/GitHubAppAuthenticator.cs:22-25`

```csharp
var installation = await httpClient.GetFromJsonAsync(
    $"https://api.github.com/repos/{owner}/{repo}/installation", ...);
```

The `owner` and `repo` values come from `RemoteUrlParser.Parse()` which splits on `github.com/` or `github.com:`. While the parser does basic validation, it doesn't sanitize for path traversal (e.g., `owner` containing `../`).

**Impact:** Extremely unlikely in practice — the values come from `git remote get-url origin`, which is controlled by the repo being committed to. An attacker would need to control the git remote URL, at which point they already have repo access.

**Remediation:**
- Add a regex check: `^[a-zA-Z0-9._-]+$` for both `owner` and `repo`.
- This is defense-in-depth; the attack surface is minimal.

**Severity: LOW**

---

### L4: Error messages may reveal filesystem paths

**Files:**
- `src/AgentGit/PrivateKeyValidator.cs:9`: `$"Private key file not found: {path}"`
- `src/AgentGit/PrivateKeyValidator.cs:18`: `$"Private key {path} has overly permissive file mode {mode}. Run: chmod 600 {path}"`
- `src/AgentGit/LoggerMessages.cs:34`: `"Private key file not found: {Path}"`

Error messages include the full filesystem path to the private key file. In a local CLI tool this is expected and helpful. But if logs are shipped to a centralized system, this leaks the key's location.

**Impact:** Information disclosure of filesystem layout. Does not expose the key itself.

**Remediation:**
- Acceptable for a CLI tool. No action needed unless logs are shipped externally.

**Severity: LOW**

---

## Positive Security Findings

These are things done **correctly** that should be preserved:

| Practice | Location | Assessment |
|----------|----------|------------|
| `ArgumentList` API for all git process invocations | `GitProcessRunner.cs` | Prevents command injection. Arguments are never passed through a shell. |
| `GIT_ASKPASS` for token delivery | `GitProcessRunner.cs:92` | Token never appears in process arguments or URLs. Correct pattern. |
| Credential helper disabled | `GitProcessRunner.cs:98-99` | `-c credential.helper=` prevents keychain interference. |
| `GIT_TERMINAL_PROMPT=0` | `GitProcessRunner.cs:93` | Prevents interactive prompts that could hang the process. |
| Private key permission validation | `PrivateKeyValidator.cs` | Rejects world-readable keys. Follows SSH key permission conventions. |
| `--no-gpg-sign` on commits | `GitProcessRunner.cs:60` | Prevents GPG passphrase prompts that would hang in headless mode. |
| Source-generated JSON serialization | `AgentGit.csproj:10`, `GitHubApiJsonContext.cs`, `JwtJsonContext.cs` | No reflection, AOT-safe, no deserialization gadget attacks. |
| `appsettings.json.example` provided | `src/AgentGit/appsettings.json.example` | Correct pattern for open-source config. |
| `.gitignore` covers `*.pem`, `.env`, `appsettings.json` | `.gitignore` | Comprehensive secret exclusion rules. |
| `UseShellExecute = false` on all processes | `GitProcessRunner.cs` | No shell interpretation of arguments. |
| HTTPS-only GitHub API calls | `GitHubAppAuthenticator.cs` | All API calls use `https://api.github.com`. |
| Short-lived tokens | GitHub installation tokens | ~1 hour lifetime, scoped to specific installation. |
| `using var rsa` ensures RSA disposal | `JwtGenerator.cs:13` | Cryptographic key material disposed after use. |
| `AskPassScriptManager.Cleanup` in `finally` block | `GitProcessRunner.cs:111-113` | Temp script always cleaned up, even on failure. |
| Process ID in askpass filename | `AskPassScriptManager.cs:9,15` | Prevents collisions between concurrent runs. |
| Gitleaks pre-commit hook | `.pre-commit-config.yaml` | Secret scanning before commits. |

---

## Dependency Audit

| Package | Version | Status |
|---------|---------|--------|
| `Microsoft.Extensions.Hosting` | 10.0.5 | Current, no known CVEs |
| `Microsoft.Extensions.Http` | 10.0.5 | Current, no known CVEs |

**Test dependencies** (not shipped):

| Package | Version | Notes |
|---------|---------|-------|
| `AutoFixture` | 4.18.1 | Maintained |
| `AutoFixture.AutoMoq` | 4.18.1 | Maintained |
| `AwesomeAssertions` | 9.4.0 | Community fork of FluentAssertions |
| `Moq` | 4.20.72 | Note: Moq had a controversial SponsorLink incident (v4.20.0). Version 4.20.72 removed it. Acceptable. |
| `xunit.v3` | 3.2.2 | Current |
| `coverlet.collector` | 8.0.1 | Current |

**No vulnerable dependencies found.**

---

## Summary of Required Actions Before Public Release

| Priority | Finding | Action |
|----------|---------|--------|
| **MUST** | H1: appsettings.json committed | `git rm --cached src/AgentGit/appsettings.json` + commit |
| **SHOULD** | H2: PEM string in memory | Read into `byte[]`, zero after use |
| **SHOULD** | M2: AskPass TOCTOU | Create temp file with restrictive permissions atomically |
| **CONSIDER** | L3: owner/repo validation | Add regex sanitization |
| **CONSIDER** | L2: Gitleaks Docker dependency | Add non-Docker fallback or CI gitleaks step |

The codebase demonstrates strong security practices overall. The command injection surface is well-protected by `ArgumentList`, the token passing via `GIT_ASKPASS` is the correct pattern, and the `.gitignore` coverage is comprehensive. The primary blocker is H1 — the committed `appsettings.json` with real credentials must be removed from tracking before the repo goes public.
