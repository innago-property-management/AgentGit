# Open Source Readiness: AgentGit
**Date:** 2026-03-30
**Verdict:** NEEDS WORK (2 blockers, then ready)

## Critical / Blockers (must fix before public)

1. **[SECURITY] `appsettings.json` tracked in git with real credentials**
   - `src/AgentGit/appsettings.json` contains real Client ID, App ID, and local key path
   - `.gitignore` rule exists but was added after the file was committed — git still tracks it
   - **Fix:** `git rm --cached src/AgentGit/appsettings.json` + scrub history with `git filter-repo`

2. **[SECURITY] Missing `SECURITY.md`**
   - Essential for a tool handling private keys and auth tokens
   - Must document: how to report vulnerabilities, supported versions, security model
   - **Fix:** Add `SECURITY.md` with responsible disclosure policy

## High Priority (should fix)

3. **[CODE] Process deadlock risk in GitProcessRunner**
   - `Commit()` and `Push()` read stdout then stderr sequentially
   - Can deadlock if git fills the stderr buffer before stdout is consumed
   - **Fix:** Read one stream async or use `OutputDataReceived`/`ErrorDataReceived` events

4. **[CODE] Missing `WaitForExit()` in helper methods**
   - `GetCurrentBranch()` and `GetRemoteUrl()` dispose process without waiting
   - **Fix:** Add `WaitForExit()` before reading output

5. **[CODE] `DefaultRequestHeaders` accumulation**
   - `AuthenticateAsync` appends UserAgent/Accept headers on every call
   - **Fix:** Move static headers to HttpClient DI configuration

6. **[PRACTICES] Missing community files**
   - `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, issue/PR templates
   - **Fix:** Add standard templates

7. **[PRACTICES] No version strategy**
   - `build-publish.yaml` triggers on tags but no tags exist
   - **Fix:** Tag v1.0.0 and establish SemVer workflow

8. **[DEPS] Add `packages.lock.json`**
   - Important for reproducibility in a security-sensitive tool
   - **Fix:** `dotnet restore --use-lock-file`

## Medium Priority (nice to have)

9. **[SECURITY] PEM key in managed string**
   - Key material read into immutable `string` — can't be zeroed
   - **Fix:** Use `byte[]` + `CryptographicOperations.ZeroMemory()`

10. **[SECURITY] AskPass TOCTOU window**
    - Microsecond gap between file creation and `SetUnixFileMode`
    - **Fix:** Use `FileStreamOptions` with `UnixCreateMode` in constructor

11. **[CODE] No orchestration test coverage**
    - `Program.cs` wiring and exit code paths are untested
    - **Fix:** Extract testable runner class or add integration test

12. **[PRACTICES] `minimumCoverage: 0` in CI**
    - No coverage gate — any regression passes
    - **Fix:** Set a baseline (even 50% is better than 0)

13. **[PRACTICES] Missing `.editorconfig`**
    - No code style enforcement
    - **Fix:** Add standard .NET `.editorconfig`

14. **[NAMING] Repo name inconsistency**
    - Binary/namespace = `AgentGit`, innago repo = `AgentGit`, personal repo = `stand-sure-ai`
    - **Fix:** Rename personal repo or pick a canonical home

## Low Priority (optional)

15. **[DEPS] Clean stale entries from `ignored-packages.json`**
    - Entries like `JUnitTestLogger`, `SonarAnalyzer.CSharp` aren't used
16. **[DEPS] Add Git/GitHub trademark notice to README**
17. **[CODE] Minor: 9 low-severity code quality nits (see code-review report)

## Dependency & License: PASS
- Zero non-Microsoft production dependencies
- All MIT licensed, no GPL contamination
- Test deps properly scoped
- License CI already runs on every PR

## Naming: AgentGit (B+ — keep it)
- Clear, functional, no trademark conflicts
- Good alternatives if renaming desired: CommitAs, GitPersona
- Main action: reconcile repo name inconsistency

## Estimated Effort
- Blockers (1-2): ~3 complexity points
- High priority (3-8): ~8 complexity points
- Medium priority (9-14): ~5 complexity points
- Total: ~16 points (~2 focused sessions)
