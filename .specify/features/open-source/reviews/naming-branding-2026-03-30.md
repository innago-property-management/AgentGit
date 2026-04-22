# Naming & Branding Review

**Date:** 2026-03-30
**Reviewer:** Claude Code (Opus 4.6)
**Subject:** AgentGit — open-source naming and branding readiness

---

## 1. Current Name Analysis: "AgentGit"

### Strengths

| Criterion | Assessment |
|-----------|------------|
| **Clarity** | Immediately communicates "agent" + "git" — the two core concepts |
| **Memorability** | Short, two-syllable compound word. Easy to say and type |
| **Searchability** | "AgentGit" as one word is distinctive; unlikely to collide with common phrases |
| **Technical accuracy** | Describes exactly what it does: gives agents a git identity |

### Weaknesses

| Criterion | Assessment |
|-----------|------------|
| **Genericness** | Both "agent" and "git" are extremely common terms in this space; could get lost in SEO noise for "agent git commit" queries |
| **Scope signal** | Name implies broad "agent + git" functionality (branching, merging, rebasing, etc.) but the tool only handles commit+push identity |
| **Confusion risk** | Could be mistaken for a git client for AI agents (like a full replacement for `gh` or `git`) rather than an identity/auth tool |

### Verdict

**AgentGit is a solid B+ name.** Clear and functional, but slightly generic and over-promises scope. Worth evaluating alternatives, but not a blocker.

---

## 2. Trademark / Conflict Scan

| Existing Project | Risk | Notes |
|-----------------|------|-------|
| **Git** (trademark of Software Freedom Conservancy) | **Medium** | The Git trademark policy allows descriptive use but discourages names that imply official affiliation. "AgentGit" reads as "Agent for Git" which is descriptive — likely fine, but worth noting. |
| **GitAgent** (various small repos on GitHub) | **Low** | Several abandoned repos use "GitAgent" or "git-agent" but none are established tools or have trademark claims |
| **agent-git** (npm) | **Low** | Check npmjs.com if publishing a Node wrapper. The kebab-case form may be taken |
| **Dependabot, Renovate** | **None** | These are the closest functional analogues (bot identity for commits) but their names don't conflict |

**Overall trademark risk: LOW.** No established product owns "AgentGit" in the developer tools space.

---

## 3. Name Alternatives

| # | Name | Rationale | Pros | Cons |
|---|------|-----------|------|------|
| 1 | **BotSign** | "Bot" identity + "signing" commits | Short, memorable, unique. Implies authentication | Doesn't mention git explicitly |
| 2 | **GitBotId** | Git + Bot + Identity | Very descriptive of function | Slightly clunky, three concepts jammed together |
| 3 | **CommitAs** | "Commit as [bot]" — describes the action | Verb-forward, intuitive CLI name (`commitas`) | Doesn't convey the GitHub App / auth mechanism |
| 4 | **BotCommit** | Bot that commits | Clear, short | Could imply automated commits rather than identity |
| 5 | **AppSign** | GitHub App + signing commits | Unique, short | "App" is overloaded; could mean mobile app |
| 6 | **GitIdent** | Git + Identity | Technical, precise | Sounds like an existing Unix command (`ident`) |
| 7 | **AgentGit** (keep) | Already established | Consistency, no migration cost | Slightly generic (see section 1) |
| 8 | **BotForge** | "Forging" bot identity for commits | Memorable, distinct, good logo potential | "Forge" has negative connotations (forgery); also conflicts with Gitea's Forgejo |
| 9 | **GitPersōna** | Git + persona (identity) | Evocative, unique, memorable | Diacritics are annoying in CLIs; `gitpersona` without them is fine |
| 10 | **AgentAuth** | Agent + authentication | Describes the mechanism accurately | Too generic; sounds like a general auth library |

### Recommendation

**Keep "AgentGit"** unless one of these resonates strongly. The name has zero migration cost, is already in README/docs/namespaces, and is clear enough. If renaming, **CommitAs** or **GitPersōna** (sans diacritic: `gitpersona`) are the strongest alternatives for distinctiveness.

---

## 4. Domain / Package Name Availability

| Registry | `agentgit` | Notes |
|----------|-----------|-------|
| **NuGet** | Likely available | Not a library today, but if it becomes a dotnet tool (`dotnet tool install agentgit`) this matters |
| **npm** | Check `agent-git` | Relevant only if publishing a Node wrapper or hook package |
| **PyPI** | N/A | No Python component |
| **GitHub** | `innago-property-management/AgentGit` exists | The canonical OSS home |
| **Domain** | `agentgit.dev` / `agentgit.io` | Worth checking; `.dev` domains are cheap and credible for dev tools |

**Action item:** Before launch, verify `agentgit` is available on NuGet (for future `dotnet tool` packaging) and grab a domain if desired.

---

## 5. Import / Namespace Ergonomics

Current state — **clean and consistent within the project**:

```
Namespace:     AgentGit
Assembly:      AgentGit
Project:       AgentGit.csproj
Tests:         AgentGit.Tests
Solution:      AgentGit.slnx
Binary output: bin/AgentGit
```

This is excellent — PascalCase C# convention, single namespace, no sub-namespaces needed for a focused tool.

If the project grows to include a library package (e.g., `AgentGit.Core` for embedding in other tools), the namespace structure scales naturally:
- `AgentGit` — CLI
- `AgentGit.Core` — library
- `AgentGit.Tests` — already exists

**No issues here.**

---

## 6. Logo / Badge Potential

"AgentGit" lends itself to visual identity:

| Concept | Description |
|---------|-------------|
| **Agent + Git branch** | A robot/agent silhouette integrated with a git branch/merge icon |
| **Bot avatar with commit hash** | The GitHub `[bot]` avatar style with a commit SHA overlay |
| **Stamp/seal** | A "certified bot identity" stamp — plays on the authentication/identity theme |
| **Spy/secret agent** | Sunglasses + git logo — playful "secret agent" riff on "agent" |

The name works well for branding. Two-syllable compound words make good logos because they can be split visually (Agent | Git) or merged into a single mark.

---

## 7. Consistency Audit

### The Problem

There are **three different names** in play:

| Context | Name | Match? |
|---------|------|--------|
| Binary / namespace / solution | `AgentGit` | Canonical |
| GitHub repo (innago) | `AgentGit` | Matches |
| GitHub repo (stand-sure) | `stand-sure-ai` | **MISMATCH** |
| Local disk directory | `stand-sure-ai` | **MISMATCH** |
| Wrapper script | `agent-git.sh` (kebab-case) | Close but different casing convention |
| Env vars | `AGENT_GIT_*` | Matches (SCREAMING_SNAKE is expected for env vars) |
| CLAUDE.md references | Both "AgentGit" and "agent-git" | Mixed |

### Recommendations

1. **Rename the `stand-sure/stand-sure-ai` repo to `stand-sure/AgentGit`** (or `stand-sure/agent-git`) to match the project name. The current name `stand-sure-ai` is the developer's personal brand, not the project name. For OSS, the repo name should match the tool.

2. **Pick one canonical kebab-case form:** `agent-git`. Use it for:
   - Wrapper script: `agent-git.sh` (already done)
   - Homebrew formula (future): `agent-git`
   - Any CLI alias: `agent-git`
   - Docker image (future): `agent-git`

3. **PascalCase for .NET artifacts:** `AgentGit` (already done)
   - Namespace: `AgentGit`
   - Assembly: `AgentGit`
   - NuGet package (future): `AgentGit`

4. **Decide on the canonical GitHub home.** Currently:
   - `innago-property-management/AgentGit` — org repo
   - `stand-sure/stand-sure-ai` — personal repo, different name

   For OSS, pick one as canonical and archive/redirect the other. If Innago is the publisher, `innago-property-management/AgentGit` is canonical and `stand-sure/stand-sure-ai` should be renamed to `stand-sure/AgentGit` and marked as a fork/mirror, or archived.

---

## 8. Summary of Findings

| Area | Status | Action Needed |
|------|--------|---------------|
| Name quality | Good | No rename required |
| Trademark risk | Low | No action needed |
| Namespace/binary consistency | Excellent | Already consistent |
| Repo name consistency | **Needs work** | Rename `stand-sure-ai` repo; decide canonical home |
| Logo readiness | Good potential | Create before launch |
| Package registry availability | Unknown | Verify NuGet/npm availability |
| Kebab/Pascal convention | Mostly consistent | Document the convention explicitly |

### Priority Actions (before OSS launch)

1. **Rename `stand-sure/stand-sure-ai`** to `stand-sure/AgentGit` (or decide it's the fork)
2. **Decide canonical repo** — one owner, one URL for all docs/links
3. **Check NuGet availability** for `AgentGit` (reserve if planning `dotnet tool` packaging)
4. **Create a simple logo/icon** for README and social preview
5. **Add a "Name" section to contributing docs** documenting PascalCase vs kebab-case conventions
