# Dependency & License Review

**Date:** 2026-03-30
**Reviewer:** Claude Code (automated)
**Project:** AgentGit (stand-sure-ai)
**Project License:** MIT

---

## Executive Summary

AgentGit has an exceptionally clean dependency profile. All production dependencies are Microsoft-owned packages under MIT license. No GPL contamination, no vendored code, no problematic transitive dependencies. A few minor improvements are recommended around supply chain hardening and NuGet metadata completeness.

**Verdict: PASS** — ready for open-source release from a dependency/license perspective.

---

## 1. License Compatibility

### Production Dependencies (Direct)

| Package | Version | License | Compatible with MIT? |
|---------|---------|---------|---------------------|
| Microsoft.Extensions.Hosting | 10.0.5 | MIT | YES |
| Microsoft.Extensions.Http | 10.0.5 | MIT | YES |

### Production Dependencies (Transitive)

All 27 transitive production dependencies are `Microsoft.Extensions.*` or `System.*` packages at version 10.0.5. These are all licensed under **MIT** by Microsoft.

Full list: Configuration, Configuration.Abstractions, Configuration.Binder, Configuration.CommandLine, Configuration.EnvironmentVariables, Configuration.FileExtensions, Configuration.Json, Configuration.UserSecrets, DependencyInjection, DependencyInjection.Abstractions, Diagnostics, Diagnostics.Abstractions, FileProviders.Abstractions, FileProviders.Physical, FileSystemGlobbing, Hosting.Abstractions, Logging, Logging.Abstractions, Logging.Configuration, Logging.Console, Logging.Debug, Logging.EventLog, Logging.EventSource, Options, Options.ConfigurationExtensions, Primitives, System.Diagnostics.EventLog.

**Result: ALL CLEAR** — zero non-Microsoft production dependencies.

### Test Dependencies (Direct)

| Package | Version | License | Compatible with MIT? |
|---------|---------|---------|---------------------|
| AutoFixture | 4.18.1 | MIT | YES |
| AutoFixture.AutoMoq | 4.18.1 | MIT | YES |
| AwesomeAssertions | 9.4.0 | Apache-2.0 | YES |
| coverlet.collector | 8.0.1 | MIT | YES |
| Microsoft.NET.Test.Sdk | 18.3.0 | MIT | YES |
| Moq | 4.20.72 | BSD-3-Clause | YES |
| xunit.v3 | 3.2.2 | Apache-2.0 | YES |
| xunit.runner.visualstudio | 3.1.5 | Apache-2.0 | YES |

### Test Dependencies (Notable Transitives)

| Package | Version | License | Notes |
|---------|---------|---------|-------|
| Castle.Core | 5.1.1 | Apache-2.0 | Moq dependency |
| Fare | 2.1.1 | MIT | AutoFixture dependency |
| Newtonsoft.Json | 13.0.3 | MIT | Test infra only |
| Microsoft.ApplicationInsights | 2.23.0 | MIT | Test SDK telemetry |

**Result: ALL CLEAR** — all test dependencies use permissive licenses (MIT, Apache-2.0, BSD-3-Clause).

---

## 2. GPL Contamination Check

**Result: NO GPL/LGPL/AGPL dependencies found** in either production or test dependency trees.

---

## 3. Vendored Code Check

- No third-party source files copied into `src/`
- No `Copyright`, `license`, `borrowed from`, or `adapted from` comments found in source files
- No vendored directories (e.g., `vendor/`, `third-party/`)

**Result: CLEAN** — no vendored code detected.

---

## 4. Trademark Concerns

| Item | Assessment |
|------|-----------|
| Project name "AgentGit" | Contains "Git" — Git is a trademark of Software Freedom Conservancy. Usage here is descriptive (a tool that uses git), not claiming to be git itself. **Low risk** but worth noting in README that Git is a trademark. |
| "GitHub App" references | Descriptive use of GitHub trademark. Acceptable under GitHub's trademark guidelines for tools that integrate with their platform. |
| Bot identity format `[bot]` | Follows GitHub's own bot naming convention. No concern. |

**Recommendation:** Add a brief trademark notice in README or NOTICE file:
> "Git" is a registered trademark of Software Freedom Conservancy, Inc. "GitHub" is a trademark of GitHub, Inc. This project is not affiliated with or endorsed by either organization.

---

## 5. NuGet Package Metadata

AgentGit is published as a **console application binary**, not a NuGet package. The `.csproj` has no NuGet packaging metadata (`PackageId`, `Authors`, `PackageLicenseExpression`, `RepositoryUrl`, `Description`, `PackageTags`, `PackageReadmeFile`).

**Assessment:** Since AgentGit is distributed as a compiled binary (via `dotnet publish`), not a NuGet package, the absence of NuGet metadata is **acceptable**.

**If NuGet distribution is ever desired**, add to `AgentGit.csproj`:
```xml
<PackageId>AgentGit</PackageId>
<Authors>Christopher Anderson</Authors>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<RepositoryUrl>https://github.com/stand-sure/stand-sure-ai</RepositoryUrl>
<Description>Gives AI agents bot identity when committing to GitHub repos</Description>
<PackageReadmeFile>README.md</PackageReadmeFile>
```

---

## 6. License Check CI Configuration

### allowed-licenses.json
```json
["MIT", "MIT-0", "Apache-2.0", "BSD-2-Clause", "BSD-3-Clause", "ISC", "0BSD", "CC0-1.0", "Unlicense", "MS-PL"]
```

**Assessment: COMPREHENSIVE** for current dependencies. All production and test dependency licenses are covered.

**Missing but potentially needed:**
- `MS-EULA` — some Microsoft test/tooling packages use this (not currently in tree, but could appear with SDK updates)
- `CC-BY-4.0` — occasionally used for documentation assets in transitive deps
- `Zlib` — occasionally seen in compression-related transitives

These are nice-to-have additions for future-proofing; not blocking.

### ignored-packages.json
```json
["Innago.*", "Microsoft.*", "JUnitTestLogger", "NETStandard.Library", "SonarAnalyzer.CSharp", "System.*", "xunit.*"]
```

**Assessment: APPROPRIATE.** Ignoring Microsoft/System packages is reasonable since they're all MIT. The `Innago.*` pattern won't apply for an open-source repo (no Innago dependencies exist), but is harmless.

**Note:** `JUnitTestLogger` and `SonarAnalyzer.CSharp` are not in the current dependency tree. These are carry-overs from the Oui Deliver template. Harmless but could be cleaned up.

### CI Integration

License checks run via `merge-checks.yaml` using the Oui Deliver reusable workflow, triggered on PRs to `main`. This means **every PR gets license-checked before merge**. Excellentastic.

---

## 7. Test Dependency Scoping

| Package | Properly Scoped? | Notes |
|---------|:----------------:|-------|
| coverlet.collector | YES | `PrivateAssets=all` — won't leak |
| xunit.runner.visualstudio | YES | `PrivateAssets=all` — won't leak |
| All other test packages | YES | In test project only (`IsPackable=false`) |
| Test project reference | YES | `ProjectReference` to AgentGit, not reverse |

**Result: CLEAN** — test dependencies cannot leak to production. The test project is `IsPackable=false` and is not referenced by the production project.

---

## 8. SBOM Generation

**Current state:** No explicit SBOM generation configured.

**However:** The `build-publish.yaml` workflow enables `slsa: true`, which generates SLSA provenance attestations via the Oui Deliver pipeline. This provides supply chain attestation but is not a full SBOM (CycloneDX/SPDX).

**Recommendation:** Add CycloneDX SBOM generation for comprehensive dependency tracking:
```bash
dotnet tool install --global CycloneDX
dotnet CycloneDX AgentGit.slnx -o sbom/ -j
```
Or integrate into CI via the `CycloneDX` dotnet tool. This produces a machine-readable BOM that consumers can audit.

**Priority:** LOW — SLSA attestation covers supply chain integrity. Full SBOM is a nice-to-have for enterprise consumers.

---

## 9. Supply Chain Security

### Package Pinning

| Aspect | Status | Notes |
|--------|--------|-------|
| Direct package versions pinned | YES | Exact versions in `.csproj` (no floating ranges) |
| `packages.lock.json` present | **NO** | Not using NuGet lock files |
| `Directory.Build.props` | **NO** | No central package management |
| SLSA provenance | YES | Enabled in CI (`slsa: true`) |
| Cosign signing | YES | Configured in CI (cosign key/password secrets) |

### Lock File Recommendation

**Add NuGet lock files** for reproducible builds. Create `Directory.Build.props`:
```xml
<Project>
  <PropertyGroup>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>
</Project>
```
Then run `dotnet restore` to generate `packages.lock.json` files. Commit them to the repo.

**Priority:** MEDIUM — provides deterministic restore and detects supply chain tampering. Especially valuable for an open-source security tool.

### Native AOT Consideration

With `PublishAot=true`, the published binary is self-contained with no runtime dependency resolution. This significantly reduces the attack surface at runtime — dependencies are compiled in at build time, not resolved from a package feed.

---

## 10. Findings Summary

### Blockers (must fix before release)

None.

### Recommendations (should fix)

| # | Item | Priority | Effort |
|---|------|----------|--------|
| R1 | Add `packages.lock.json` via `RestorePackagesWithLockFile` | MEDIUM | 1 point |
| R2 | Add trademark notice for Git/GitHub in README or NOTICE | LOW | 1 point |
| R3 | Clean up stale entries in `ignored-packages.json` (`JUnitTestLogger`, `SonarAnalyzer.CSharp`) | LOW | 1 point |

### Nice-to-haves

| # | Item | Priority | Effort |
|---|------|----------|--------|
| N1 | Add CycloneDX SBOM generation to CI | LOW | 2 points |
| N2 | Add `MS-EULA`, `CC-BY-4.0`, `Zlib` to allowed-licenses.json for future-proofing | LOW | 1 point |
| N3 | Add NuGet metadata to .csproj if package distribution is ever planned | LOW | 1 point |

---

## Appendix: Dependency Tree Visualization

```
AgentGit (MIT) — PRODUCTION
├── Microsoft.Extensions.Hosting 10.0.5 (MIT)
│   ├── Microsoft.Extensions.Configuration.* 10.0.5 (MIT)
│   ├── Microsoft.Extensions.DependencyInjection.* 10.0.5 (MIT)
│   ├── Microsoft.Extensions.Diagnostics.* 10.0.5 (MIT)
│   ├── Microsoft.Extensions.FileProviders.* 10.0.5 (MIT)
│   ├── Microsoft.Extensions.Hosting.Abstractions 10.0.5 (MIT)
│   ├── Microsoft.Extensions.Logging.* 10.0.5 (MIT)
│   ├── Microsoft.Extensions.Options.* 10.0.5 (MIT)
│   └── System.Diagnostics.EventLog 10.0.5 (MIT)
└── Microsoft.Extensions.Http 10.0.5 (MIT)

AgentGit.Tests (not published) — TEST ONLY
├── AutoFixture 4.18.1 (MIT)
│   └── Fare 2.1.1 (MIT)
├── AutoFixture.AutoMoq 4.18.1 (MIT)
├── AwesomeAssertions 9.4.0 (Apache-2.0)
├── coverlet.collector 8.0.1 (MIT) [PrivateAssets=all]
├── Microsoft.NET.Test.Sdk 18.3.0 (MIT)
│   ├── Microsoft.ApplicationInsights 2.23.0 (MIT)
│   └── Newtonsoft.Json 13.0.3 (MIT)
├── Moq 4.20.72 (BSD-3-Clause)
│   └── Castle.Core 5.1.1 (Apache-2.0)
├── xunit.v3 3.2.2 (Apache-2.0)
└── xunit.runner.visualstudio 3.1.5 (Apache-2.0) [PrivateAssets=all]
```
