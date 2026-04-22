# OSS Best Practices Review — AgentGit

**Reviewer:** Claude Code (OSS Best Practices)
**Date:** 2026-03-30
**Scope:** Repository readiness for public open-source release

---

## Summary

AgentGit is well-architected with strong security practices (private key validation, GIT_ASKPASS token handling, gitleaks pre-commit hooks) and a high-quality README. However, several standard OSS artifacts are missing, and there is one **critical hygiene issue** with tracked credentials that must be resolved before public release.

**Overall:** 🟡 Good foundation, needs moderate work before public release.

| Category | Rating | Notes |
|----------|--------|-------|
| LICENSE | ✅ Pass | MIT, complete, dated 2026 |
| README.md | ✅ Pass | Excellent — badges, architecture, setup, usage |
| Security posture | ✅ Pass | Gitleaks, key validation, token hygiene |
| CI/CD | ✅ Pass | Oui Deliver, license checks, code review |
| Code quality | ✅ Pass | Native AOT, source generators, TreatWarningsAsErrors |
| Tests | ✅ Pass | 6 test files covering all non-trivial classes |
| CONTRIBUTING.md | ❌ Missing | |
| CODE_OF_CONDUCT.md | ❌ Missing | |
| SECURITY.md | ❌ Missing | |
| CHANGELOG.md | ❌ Missing | |
| Issue/PR templates | ❌ Missing | |
| .editorconfig | ❌ Missing | |
| Versioning/tags | ⚠️ Incomplete | No tags, no NEXT_VERSION, no release strategy |
| Tracked credentials | 🔴 Critical | `appsettings.json` with real config in git history |
| Cross-platform | ⚠️ Partial | Wrapper script is bash-only |

---

## Detailed Findings

### 1. 🔴 CRITICAL: `appsettings.json` Tracked with Real Credentials

**File:** `src/AgentGit/appsettings.json` (tracked in git)

Despite `.gitignore` listing `appsettings.json`, the file is already tracked and contains real configuration:

```json
{
  "GitHubApp": {
    "ClientId": "Iv23li8yGhLhlDV5cm0A",
    "AppId": 3167794,
    "PrivateKeyPath": "/Users/christopheranderson/Documents/Projects/stand-sure/stand-sure-ai.2026-03-23.private-key.pem",
    "AgentName": "stand-sure-ai"
  }
}
```

**Risk:** The Client ID is semi-public (visible on the GitHub App page), and no private key is exposed. However:
- The local filesystem path leaks the developer's username and directory structure
- Git history will preserve this even if removed from HEAD
- `.gitignore` only prevents *untracked* files — already-tracked files remain tracked

**Remediation:**
1. `git rm --cached src/AgentGit/appsettings.json` to untrack
2. Consider using `git filter-repo` or BFG to scrub from history before public release
3. The `.csproj` has `<Content Include="appsettings.json" CopyToOutputDirectory="Always" />` — this will break the build if the file isn't present. Change to copy `appsettings.json.example` or make the content include conditional
4. Update README to clarify the `appsettings.json.example` → `appsettings.json` copy step (already done, good)

### 2. ✅ LICENSE — MIT, Complete

Standard MIT license, copyright 2026 Christopher Anderson. No issues.

### 3. ✅ README.md — Excellent Quality

The README is comprehensive and well-structured:
- Clear "what it does" intro
- ASCII flow diagram of the process
- Complete GitHub App creation walkthrough (Steps 1-6)
- Prerequisites, build, configure, usage sections
- Architecture overview with security callouts
- Dependency table
- Environment variable reference

**Minor suggestions:**
- Add a badge row at the top (build status, license, .NET version)
- Add a "Contributing" section linking to CONTRIBUTING.md (once created)
- The clone URL references `innago-property-management/AgentGit` — confirm this is the intended public org/repo name

### 4. ❌ CONTRIBUTING.md — Missing

Essential for community participation. Should cover:
- How to report bugs
- How to propose features
- Development setup (prerequisites, build, test)
- Code style expectations
- PR process and review expectations
- CLA requirements (if any)

### 5. ❌ CODE_OF_CONDUCT.md — Missing

Standard for OSS projects. Recommend adopting the [Contributor Covenant v2.1](https://www.contributor-covenant.org/).

### 6. ❌ SECURITY.md — Missing

**Important for a security-sensitive tool.** AgentGit handles private keys and authentication tokens — a security policy is essential. Should cover:
- How to report vulnerabilities (private disclosure process)
- Supported versions
- Security design principles (key validation, token handling)
- Expected response timeline

### 7. ❌ CHANGELOG.md — Missing

No changelog and no git tags exist. The project has meaningful history (OSS readiness, Native AOT refactor, CI/CD setup) but no versioned releases.

**Recommendation:**
- Adopt [Keep a Changelog](https://keepachangelog.com/) format
- Create an initial `v1.0.0` tag (or `v0.1.0` if pre-release)
- Consider a `NEXT_VERSION` file for Oui Deliver semver automation

### 8. ❌ Issue and PR Templates — Missing

No `.github/ISSUE_TEMPLATE/` directory or `pull_request_template.md`.

**Recommendation:** Create at minimum:
- `.github/ISSUE_TEMPLATE/bug_report.yml` — structured bug reports
- `.github/ISSUE_TEMPLATE/feature_request.yml` — feature proposals
- `.github/pull_request_template.md` — PR checklist

### 9. ❌ .editorconfig — Missing

No `.editorconfig` file. For a .NET project, this is important for:
- Consistent indentation (tabs vs spaces)
- Line endings (LF for cross-platform)
- C# code style enforcement (naming conventions, `var` usage, etc.)
- IDE-agnostic formatting

**Recommendation:** Add a standard .NET `.editorconfig` at the repo root.

### 10. ✅ Pre-commit Hooks — Good

`.pre-commit-config.yaml` includes:
- **Gitleaks v8.24.2** — secret detection (Docker-based)
- **Ledger hooks** — commit attestation (ledger-add pre-commit, ledger-finalize post-commit)

The gitleaks integration is a strong security practice. The ledger hooks support the agent commit attestation feature.

### 11. ✅ CI/CD — Comprehensive

Four GitHub Actions workflows using Oui Deliver reusable workflows:

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `build-publish.yaml` | push to main, tags, PRs | Build, test, SLSA attestation, cosign |
| `merge-checks.yaml` | PRs to main | License compliance check |
| `auto-pr.yaml` | push to non-main branches | Auto-create PRs |
| `claude-code-review.yml` | PR events | AI-powered code review |

**Strengths:**
- SLSA provenance (`slsa: true`)
- Cosign signing (`cosignKey`, `cosignPassword`)
- License allowlist (`.github/actions/check-licenses-action/`)
- Automated code review

**Concerns:**
- `minimumCoverage: 0` — no code coverage gate. Consider raising to 70-80%
- Build-publish permissions are broad (`contents: write`, `packages: write`). Review if all are needed for the public repo

### 12. ✅ Code Quality — Strong

- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in both projects
- `<Nullable>enable</Nullable>` — full nullable reference types
- `<PublishAot>true</PublishAot>` with `JsonSerializerIsReflectionEnabledByDefault=false`
- Source-generated JSON serialization (AOT-compatible)
- `LoggerMessages.cs` with source-generated logging
- Clean single-responsibility class design (11 source files)
- Test coverage for all non-trivial classes (6 test files)

### 13. ⚠️ Versioning Strategy — Incomplete

- **No git tags** exist
- **No `NEXT_VERSION`** file for Oui Deliver semver automation
- **No version in `.csproj`** (`<Version>` property not set)
- The `build-publish.yaml` triggers on tags (`tags: [ "*" ]`) but no tags have been created

**Recommendation:**
- Add `<Version>1.0.0</Version>` to `AgentGit.csproj` (or use `NEXT_VERSION` for auto-versioning)
- Create initial release tag after addressing review findings
- Document release process in CONTRIBUTING.md

### 14. ⚠️ Cross-Platform Support — Partial

- The .NET code itself is cross-platform (Native AOT builds per-RID)
- `scripts/agent-git.sh` is bash-only — no Windows equivalent
- The `fnox` integration in the wrapper is platform-specific
- README mentions `chmod 600` (Unix-only) for key permissions without Windows alternative

**Recommendation:**
- Document supported platforms explicitly
- Consider a PowerShell wrapper or .NET global tool approach for Windows
- Or document Windows as "unsupported" with a note about WSL

### 15. ⚠️ NuGet/Package Metadata — Not Applicable but Noteworthy

This is a console app, not a library, so NuGet packaging isn't needed. However, the `.csproj` lacks:
- `<Description>` — useful for binary distribution
- `<Authors>`
- `<PackageProjectUrl>`
- `<RepositoryUrl>`

These are helpful for `dotnet tool` distribution if that becomes a goal.

### 16. ✅ Git Hygiene — Good

- Clean commit history with conventional-style messages (`feat:`, `fix:`, `docs:`, `ci:`, `chore:`)
- PRs used for feature work
- Agent commit ledger (`.claude/ledger/`, `ledger-*.sh` scripts) for attestation
- `.gitignore` properly excludes `*.pem`, `.env`, build artifacts, IDE files

### 17. ⚠️ Repo Root Clutter

Several files at the repo root appear to be development tooling that might confuse contributors:
- `ledger-add.sh`, `ledger-finalize.sh`, `ledger-rewrite.sh`, `ledger-seal.sh` — commit attestation scripts
- `.last-thought-timestamp` — internal tooling artifact
- `.qodo/` — IDE-specific directory

**Recommendation:**
- Move ledger scripts to `scripts/` or `.claude/`
- Add `.last-thought-timestamp` and `.qodo/` to `.gitignore`
- Verify `.idea/` is properly gitignored (it's in `.gitignore` but the directory exists)

---

## Priority Action Items

### P0 — Must Fix Before Public Release

1. **Untrack `appsettings.json`** — Remove from git index, scrub from history
2. **Add `SECURITY.md`** — Critical for a security-sensitive tool
3. **Audit git history** for any other credential leaks before making public

### P1 — Should Fix Before Public Release

4. **Add `CONTRIBUTING.md`** — Development setup, code style, PR process
5. **Add `CODE_OF_CONDUCT.md`** — Adopt Contributor Covenant
6. **Add `.editorconfig`** — .NET code style enforcement
7. **Add issue and PR templates** — Structured community interaction
8. **Create initial version tag** (`v1.0.0` or `v0.1.0`)
9. **Set `minimumCoverage`** to a meaningful threshold (e.g., 70%)
10. **Add README badges** — Build status, license, .NET version

### P2 — Nice to Have

11. **Add `CHANGELOG.md`** — Document notable changes per version
12. **Clean repo root** — Move ledger scripts, gitignore IDE artifacts
13. **Document platform support** — Clarify Windows/Linux/macOS status
14. **Add `<Version>` to `.csproj`** — Binary version metadata
15. **Consider `FUNDING.yml`** — If sponsorship is desired

---

## Strengths Worth Preserving

- Excellent README with complete GitHub App setup walkthrough
- Strong security practices (gitleaks, key validation, GIT_ASKPASS)
- SLSA provenance and cosign signing in CI
- License compliance checking
- Clean, well-factored codebase with full nullable annotations
- Native AOT with zero reflection — modern .NET best practices
- Comprehensive test coverage for core logic
- Agent commit attestation via ledger system
