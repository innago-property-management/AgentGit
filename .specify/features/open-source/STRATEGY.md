# Open Source Release Strategy: AgentGit

**Version:** 1.0
**Date:** 2026-03-30
**Author:** Christopher Anderson (with Claude Code)
**Status:** Ready for execution

---

## Executive Summary

AgentGit is a .NET 10 Native AOT console app that solves a genuine greenfield problem: giving AI agents their own bot identity when committing and pushing to GitHub. As autonomous AI agents become standard tooling across engineering teams, the question of "who wrote this commit?" moves from nice-to-have to compliance requirement. No purpose-built tool exists for this today.

The project is positioned as a developer tool / CLI utility targeting individual developers and small teams running AI agents (Claude Code, Copilot, Cursor, etc.) who need audit-quality commit attribution. The competitive landscape is essentially empty -- people currently use git config hacks, shared PATs, or just let the agent commit as the human. AgentGit is the first tool to provide proper GitHub App bot identity for autonomous agents, analogous to how Dependabot and Renovate get their own identity but as a self-hosted, general-purpose tool.

This strategy is designed for a solo maintainer with no marketing budget, releasing evenings/weekends. It prioritizes high-signal, low-effort channels that actually work for CLI developer tools, sets realistic metrics for a niche utility, and avoids over-promising on SLAs or community engagement. The v1.0.0 tag is already cut. The OSS readiness review is complete. This document covers the path from "code is public" to "people are actually using it."

---

## 1. Project Assessment

### Project Classification

| Dimension | Value |
|-----------|-------|
| **Type** | Developer tool / CLI utility |
| **Runtime** | Native AOT binary (~8MB, zero runtime dependency) |
| **Distribution** | Source build, GitHub Releases binary |
| **Integration** | Claude Code hook (transparent), direct invocation, wrapper script |
| **Complexity** | Low (single binary, one config file, one GitHub App) |

### Target Audience

**Primary:** Individual developers and small teams running AI coding agents who want proper commit attribution. These are early adopters of Claude Code, Copilot Workspace, Cursor, and similar tools.

**Secondary:** Platform/DevOps engineers at companies scaling autonomous AI agent usage who need audit trails, compliance, and traceability for agent-generated commits.

**Tertiary:** AI agent framework authors (LangChain, CrewAI, AutoGen) who need a reference implementation for GitHub App integration.

### Competitive Landscape

| Current approach | Drawback |
|-----------------|----------|
| `git config user.name "bot"` | No GitHub verification, no real identity, trivially spoofable |
| Shared PAT / bot token | No per-agent traceability, token rotation nightmare, exposed in process args |
| Commit as the human | No audit trail, impossible to tell human from machine work |
| Dependabot / Renovate | SaaS-only, purpose-specific, not available for arbitrary agent workflows |

**AgentGit is the only self-hosted, general-purpose tool that provides proper GitHub App bot identity for autonomous AI agents.** There is no direct competitor.

### Value Proposition

> AgentGit gives AI agents their own verified bot identity on GitHub -- proper `[bot]` commits with GitHub App authentication, token-safe push via `GIT_ASKPASS`, and transparent hook integration so agents don't even know it's there.

**Key differentiators:**
1. **Proper bot identity** -- Commits appear as `YourApp[bot]` with verified email, just like Dependabot
2. **Token-safe** -- Installation tokens passed via `GIT_ASKPASS`, never in process args or URLs
3. **Transparent** -- Claude Code hook means agents just run `git commit` and get bot identity automatically
4. **Multi-org** -- Single GitHub App works across orgs via dynamic installation lookup
5. **Zero runtime** -- Native AOT binary, no .NET runtime needed on the host machine

---

## 2. Licensing Strategy

### Recommended License: MIT (Validated)

**Status:** Already in place. No change needed.

### Rationale

MIT is the correct choice for AgentGit:

1. **Maximum adoption is the priority.** AgentGit establishes a new pattern. Friction-free licensing accelerates the "this is how you do it" moment.
2. **No patent concerns.** AgentGit uses standard JWT/RSA crypto from the .NET BCL and GitHub's documented API. There are no novel algorithms or patentable techniques to protect.
3. **Developer tool convention.** The target audience expects MIT for CLI tools. Apache 2.0's NOTICE file requirements and additional compliance steps add friction without material benefit here.
4. **Solo maintainer simplicity.** No CLA needed, no NOTICE files to maintain, no contributor patent grant complexity.
5. **Dependencies are clean.** `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.Http` are MIT-licensed. Zero copyleft risk.

### Why NOT Apache 2.0

Apache 2.0 would be the next-best choice (patent grant, enterprise-friendly). However:
- AgentGit has no novel patentable technology
- The target audience is individual developers, not enterprises (yet)
- MIT has lower compliance overhead for a solo maintainer
- If enterprise adoption grows and patent concerns emerge, relicensing from MIT to Apache 2.0 is straightforward (MIT is compatible)

### Trademark Protection

**Current:** Not needed for launch. AgentGit is descriptive enough that trademark squatting is low risk.

**Future:** If the project gains traction, consider:
- Register `agentgit.dev` or similar domain
- Add a brief TRADEMARK.md if forks start causing confusion
- USPTO registration only if commercial value materializes

### Corporate Approval Considerations

AgentGit is released from the `innago-property-management` org but is a personal passion project by Christopher Anderson. Key considerations:

- [x] MIT license -- universally approved by corporate legal
- [x] No proprietary Innago code or business logic
- [x] No customer data, internal URLs, or trade secrets
- [x] Git history is clean (purpose-built for open source from the start)
- [x] CI/CD uses Innago's Oui Deliver (shared infrastructure, not proprietary)

---

## 3. Security and Maintenance Commitment

### Security Model Summary

AgentGit handles sensitive credentials (private keys, installation tokens). The security design is already solid:

- Private key permission enforcement (rejects world-readable keys, zeros memory after use)
- Token passed via `GIT_ASKPASS` + env var, never in CLI args or URLs
- Credential helpers disabled during push to prevent keychain override
- Temporary askpass scripts created with restrictive permissions, cleaned up after use

### Security Response SLAs

**Important framing:** These are solo-maintainer, best-effort commitments, not contractual SLAs. The SECURITY.md already sets appropriate expectations. Refinements below.

| Severity | CVSS | Acknowledgment | Fix Target | Communication |
|----------|------|----------------|------------|---------------|
| **Critical** | 9.0-10.0 | 48 hours | 1 week | GitHub Security Advisory + README notice |
| **High** | 7.0-8.9 | 1 week | 2 weeks | Security Advisory on release |
| **Medium** | 4.0-6.9 | 2 weeks | Next release | Release notes |
| **Low** | 0.1-3.9 | Best effort | Future release or wontfix | Optional |

**Scope in:** Token/key exposure, command injection, privilege escalation, auth bypass, insecure temp files.
**Scope out:** Local root attacks, social engineering, GitHub API DoS.

**Note:** SECURITY.md already covers this well. The only refinement is adding CVSS ranges to make severity assessment objective. Consider enabling GitHub private vulnerability reporting (Settings > Security) if not already enabled.

### CVE Monitoring Approach

| Tool | Purpose | Status |
|------|---------|--------|
| **Oui Deliver CI** | SAST, license compliance, secrets scanning on every PR | Active |
| **Dependabot** | Automated dependency vulnerability alerts | Enable if not already |
| **NuGet Audit** | `dotnet restore` vulnerability warnings (built into .NET 10) | Active (TreatWarningsAsErrors) |
| **Cosign signing** | Binary provenance and SLSA Level 3 attestation | Active |
| **packages.lock.json** | Pinned dependency graph for reproducible builds | Active |

### CI/CD Security Integration

Already in place via Oui Deliver:
- [x] Build + test on every PR
- [x] License compliance check (`check-licenses-action`)
- [x] SAST scanning
- [x] Secrets scanning
- [x] SLSA Level 3 provenance
- [x] Cosign binary signing
- [x] Merge checks workflow

### Long-Term Support Window

| Version | Support Level | Duration |
|---------|--------------|----------|
| v1.x (current) | Full: security + bugs + features | Until v2.0 release |
| v1.x (after v2.0) | Security-only | 6 months after v2.0 |
| Pre-1.0 | None | N/A (no pre-release users) |

**Honest framing for README/docs:**

> AgentGit is maintained by a solo developer in evenings/weekends. Security issues are taken seriously and addressed promptly. Feature development follows interest and available time. There are no commercial SLAs -- this is a passion project maintained in good faith.

---

## 4. Marketing and Promotion Strategy

### Messaging Framework

**One-liner:**
> Give AI agents their own bot identity when committing to GitHub.

**Problem statement (for blog/HN):**
> AI agents are committing code as humans. Claude Code, Copilot, Cursor -- they all `git commit` as you. That's fine for a solo developer, but when your team has 5 agents running autonomously, your git history becomes meaningless. You can't tell who wrote what. You can't audit. You can't comply.

**Solution statement:**
> AgentGit authenticates as a GitHub App and commits as a proper `[bot]` identity -- the same way Dependabot and Renovate work, but for any AI agent. One binary, one config file, five minutes to set up.

### Target Channels (Priority Order)

For a solo-maintainer CLI tool with no budget, these are the channels that actually move the needle, ranked by effort-to-impact ratio:

#### Tier 1: High Impact, Low Effort

**1. Hacker News "Show HN"**
- **Why:** Single best channel for developer tools. One good post can generate 5K-20K impressions.
- **Timing:** Tuesday-Thursday, 8-10 AM EST
- **Title format:** `Show HN: AgentGit -- Give AI agents their own bot identity on GitHub`
- **Link to:** README (not a blog post -- HN readers want to see the code)
- **Critical:** Be present in comments for the first 2-3 hours. Answer every question.

**2. Twitter/X announcement thread**
- **Why:** AI agent tooling Twitter is active and engaged. Threads get amplified.
- **Structure:**
  1. Problem: "AI agents commit as you. Your git history is a lie."
  2. Solution: "AgentGit gives agents their own [bot] identity"
  3. How it works: JWT flow diagram or screenshot of bot commit
  4. Key feature: "Claude Code hook -- agents don't even know it's there"
  5. Link + call to action
- **Tag:** People in the Claude Code / AI agent tooling space (genuine connections, not spray-and-pray)

**3. Reddit r/ClaudeAI and r/programming**
- **Why:** Direct audience overlap. Claude Code users ARE the target market.
- **Format:** "Show /r/ClaudeAI" style post explaining the problem and solution
- **Also consider:** r/dotnet (for the .NET angle), r/devops (for the audit/compliance angle)

#### Tier 2: Medium Impact, Medium Effort

**4. Dev.to technical article**
- **Title:** "How I Gave AI Agents Their Own Git Identity (and Why It Matters)"
- **Content:** Problem statement, architecture walkthrough, setup guide, screenshots of bot commits
- **SEO value:** Long-tail search for "AI agent git commit identity" will grow over time

**5. Claude Code community / Anthropic Discord**
- **Where:** Share in relevant channels (#showcase, #tools, #community-projects)
- **Approach:** "Built this to solve my own problem -- sharing in case others have the same issue"

**6. .NET community channels**
- **Why:** Native AOT, source-generated JSON, zero-reflection -- this is a showcase for modern .NET
- **.NET Blog / .NET Community Standups** -- Submit for consideration
- **r/dotnet** -- "Built a Native AOT CLI tool that authenticates as a GitHub App"

#### Tier 3: Longer-Term, Higher Effort

**7. LinkedIn post**
- **Angle:** Enterprise/compliance framing -- "As AI agents write more code, traceability becomes a governance requirement"
- **Audience:** Engineering managers, CTOs evaluating AI agent adoption

**8. Newsletter submissions**
- **TLDR Newsletter** (tldr.tech) -- Submit via their form
- **Console.dev** -- Developer tools focus, good fit
- **.NET Weekly** -- .NET ecosystem newsletter

**9. Conference CFPs (3-6 month horizon)**
- **Talk title:** "Who Wrote This Code? Solving AI Agent Identity for Git"
- **Target:** .NET Conf, NDC, local meetups, AI/DevOps conferences
- **Format:** Lightning talk (10 min) or standard (25 min)

### Content Plan

| Week | Content | Channel |
|------|---------|---------|
| Launch | Show HN post | Hacker News |
| Launch | Announcement thread | Twitter/X |
| Launch | Show post | r/ClaudeAI, r/programming |
| Launch +1 | Technical article | Dev.to |
| Launch +2 | "Architecture of AgentGit" post | Dev.to / personal blog |
| Launch +4 | "Native AOT + GitHub App Auth" .NET angle | r/dotnet |
| Month 2 | First user showcase / case study | Twitter thread |
| Month 3 | Conference CFP submissions | Various |
| Month 4+ | Ongoing: release notes, tips, user stories | Twitter, GitHub Discussions |

### What NOT To Do

- Do not spam multiple communities on the same day
- Do not use marketing language ("revolutionary", "game-changing")
- Do not post the same content verbatim across platforms
- Do not pay for stars, fake engagement, or promotional services
- Do not submit to HN more than once (flags as spam)

---

## 5. Success Metrics and Targets

### Calibration Note

AgentGit is a niche developer tool for a rapidly growing but still early market (autonomous AI agents). Targets are calibrated for:
- Solo maintainer, no marketing budget
- Greenfield problem space (no established search volume yet)
- .NET ecosystem (smaller than Node/Python for CLI tools)
- Quality of engagement matters more than raw numbers

### GitHub Metrics

| Metric | Week 1 | Month 1 | Month 3 | Month 6 | Year 1 |
|--------|--------|---------|---------|---------|--------|
| **Stars** | 25-75 | 100-300 | 250-600 | 500-1,200 | 1,000-2,500 |
| **Forks** | 2-5 | 5-15 | 15-30 | 30-60 | 60-150 |
| **Clones/week** | 20-50 | 30-80 | 50-150 | 100-300 | 200-500 |
| **Watchers** | 5-10 | 10-25 | 20-50 | 40-80 | 80-150 |

### Engagement Metrics

| Metric | Month 1 | Month 3 | Month 6 | Year 1 |
|--------|---------|---------|---------|--------|
| **Issues opened** | 3-8 | 5-15 | 10-25 | 25-50 |
| **External PRs** | 0-1 | 1-3 | 2-5 | 5-15 |
| **Discussions** | 2-5 | 5-10 | 10-20 | 20-40 |
| **External contributors** | 0 | 1-2 | 2-5 | 5-10 |

### Quality Metrics (Maintain, Not Grow)

| Metric | Target | Current |
|--------|--------|---------|
| **Test count** | Growing with features | 28 |
| **Build warnings** | 0 (TreatWarningsAsErrors) | 0 |
| **CI pass rate** | >95% | ~100% |
| **Issue response time** | <72 hours (weekday) | N/A |
| **PR review time** | <1 week | N/A |

### Milestone Definitions

**"It landed" (Week 1):** HN post submitted, 25+ stars, first issue from a stranger.

**"People are trying it" (Month 1):** 100+ stars, 3+ issues from real users, someone besides you has it working.

**"It has users" (Month 3):** 250+ stars, first external PR, someone mentions it in a blog/tweet you didn't prompt.

**"It's established" (Month 6):** 500+ stars, 3+ external contributors, mentioned in AI agent tooling discussions organically.

**"It's the standard" (Year 1):** 1,000+ stars, recognized as the way to do agent git identity, other tools integrate or recommend it.

### What Success Actually Looks Like

For a solo-maintainer niche tool, success is NOT 10K stars. Success is:
- People searching "AI agent git identity" find AgentGit
- Claude Code / AI agent documentation references it
- 2-3 companies use it in production
- The pattern (GitHub App for agent identity) becomes accepted practice
- You're not the only one who can fix bugs

---

## 6. Risk Mitigation

### Legal Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **IP exposure from Innago code** | Low | High | Repo was purpose-built for OSS; no internal code. Pre-release review completed. |
| **Patent infringement** | Very Low | Medium | Uses standard JWT/RSA from .NET BCL and documented GitHub APIs. No novel algorithms. |
| **License compatibility** | Very Low | Low | Only two dependencies (Microsoft.Extensions.*), both MIT. License check in CI. |
| **Trademark conflict** | Low | Low | "AgentGit" is descriptive. No known conflicts. Monitor if adoption grows. |

### Security Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **Private key exposure in repo** | Low | Critical | `.gitignore` excludes keys; `appsettings.json` is gitignored; CI uses secrets. |
| **Token leak via process args** | Very Low | High | GIT_ASKPASS pattern; credential helpers disabled; verified in code review. |
| **Supply chain attack on deps** | Low | High | Only 2 deps (Microsoft-owned); packages.lock.json pinning; Cosign signing. |
| **Malicious PR** | Low | Medium | All PRs require CI pass; TreatWarningsAsErrors; SAST scanning; manual review. |

### Operational Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **Maintainer burnout** | Medium | High | Set realistic expectations publicly. No SLA promises. Say no to scope creep. |
| **Bus factor = 1** | High | High | Document everything. Architecture is simple. Succession plan in GOVERNANCE.md. |
| **Feature creep** | Medium | Medium | Stick to core mission: agent identity for git. Reject out-of-scope requests politely. |
| **GitHub App API changes** | Low | Medium | Use stable, documented APIs only. Monitor GitHub changelog. |
| **.NET 10 EOL** | Very Low | Low | .NET 10 is an LTS candidate. Native AOT binary has no runtime dependency anyway. |

### Reputation Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **"Why .NET for a CLI tool?"** | High | Low | Lean into it: "Native AOT, 8MB binary, zero runtime, faster than Node." |
| **Negative HN reaction** | Medium | Medium | Be humble, respond to all feedback, acknowledge limitations. |
| **Abandoned project perception** | Medium | Medium | Regular releases even if small. Update README with last-updated date. |

### Mitigation Priorities

1. **Bus factor** -- Most critical. Write GOVERNANCE.md with succession plan before launch. Document the release process.
2. **Burnout** -- Set clear boundaries in public docs. "Best effort" language everywhere.
3. **Security** -- Already strong. Enable GitHub private vulnerability reporting. Add Dependabot config.
4. **Scope creep** -- Define "what AgentGit is NOT" in the README or a VISION.md.

---

## 7. Community Management Plan

### Governance Model: Benevolent Dictator (BDFL) with Exit Plan

For a solo-maintainer project, the BDFL model is honest and appropriate. Formalize it:

**GOVERNANCE.md contents:**

```
Maintainer: Christopher Anderson (@stand-sure)
Model: BDFL (Benevolent Dictator for Life) until community warrants Core Team

Decision-making:
- Minor changes (bugs, docs, small features): Maintainer discretion
- Major changes (breaking changes, architecture): RFC in GitHub Discussions, 1 week feedback period
- Security fixes: Immediate, notify community after

Path to Core Team (triggered at 3+ regular contributors):
- Contributor demonstrates sustained quality contributions (3+ months)
- Maintainer invites to triage team, then to core team
- Core Team uses lazy consensus for minor changes, majority vote for major

Succession plan:
- If maintainer becomes unavailable for 90+ days:
  1. Active contributor with 6+ months history may request maintainer access
  2. GitHub org admin (Innago) can transfer ownership
  3. If no successor: archive repo with "unmaintained" notice and link to forks
```

### Community Channels

| Channel | Purpose | Launch? |
|---------|---------|---------|
| **GitHub Issues** | Bug reports, feature requests | Yes (already exists) |
| **GitHub Discussions** | Q&A, ideas, show-and-tell | Yes (enable before launch) |
| **Discord/Slack** | Real-time chat | No (premature for solo maintainer; revisit at 500+ stars) |

**Why no Discord at launch:** Real-time chat creates an expectation of real-time response. A solo maintainer working evenings/weekends cannot sustain that. GitHub Discussions is async-friendly and searchable.

### Issue Templates

Create before launch:

1. **Bug Report** -- Steps to reproduce, expected vs actual, environment (OS, .NET version, git version)
2. **Feature Request** -- Problem description, proposed solution, alternatives considered
3. **Security Vulnerability** -- Redirect to SECURITY.md (not a public template)

### Contributor Recognition

- Credit in release notes for every contribution (code, docs, bug reports)
- All Contributors badge in README once there are external contributors
- "Good first issue" labels on appropriate issues

### Growth Strategy

| Phase | Trigger | Actions |
|-------|---------|---------|
| **Foundation** | Launch | Issues, Discussions, CONTRIBUTING.md, GOVERNANCE.md |
| **Early Growth** | 5+ issues from strangers | Label "good first issues", respond quickly, thank reporters |
| **Contributors** | First external PR | Celebrate publicly, fast review, mentor through process |
| **Core Team** | 3+ regular contributors | Invite to triage team, share maintenance load |
| **Community** | 500+ stars | Consider Discord, Hacktoberfest participation, office hours |

---

## 8. Go-to-Market Timeline

### Pre-Launch: Now through Launch Day

**Already Complete:**
- [x] MIT license
- [x] README with GitHub App setup walkthrough
- [x] SECURITY.md with scope and response timeline
- [x] CONTRIBUTING.md with dev setup, conventions, PR process
- [x] CODE_OF_CONDUCT.md
- [x] .editorconfig, packages.lock.json
- [x] Full OSS review (code, security, deps, naming)
- [x] v1.0.0 tagged
- [x] CI/CD with SLSA Level 3, Cosign, license compliance, SAST

**Pre-Launch TODO (1-2 evenings):**
- [ ] Enable GitHub Discussions (Q&A, Ideas, Show and Tell categories)
- [ ] Enable GitHub private vulnerability reporting
- [ ] Add Dependabot configuration (`.github/dependabot.yml` for NuGet)
- [ ] Create issue templates (Bug Report, Feature Request)
- [ ] Add GOVERNANCE.md with succession plan
- [ ] Add GitHub Topics: `ai-agent`, `git`, `github-app`, `dotnet`, `native-aot`, `developer-tools`, `bot-identity`, `claude-code`
- [ ] Add badges to README (CI status, license, .NET version)
- [ ] Verify `appsettings.json.example` is in repo (it is, per README)
- [ ] Write HN "Show HN" title and prepare for comment engagement
- [ ] Draft Twitter/X announcement thread
- [ ] Draft r/ClaudeAI post

### Launch Week (Pick a Tuesday-Thursday)

**Day 1 (Launch Day):**
- [ ] Submit to Hacker News (8-10 AM EST): `Show HN: AgentGit -- Give AI agents their own bot identity on GitHub`
- [ ] Post Twitter/X announcement thread
- [ ] Post to r/ClaudeAI
- [ ] Post to r/programming (if HN doesn't gain traction, wait a day)
- [ ] Monitor and respond to all comments for 2-3 hours

**Day 2-3:**
- [ ] Continue responding to HN/Reddit/Twitter comments
- [ ] Share in Claude Code / Anthropic Discord if accessible
- [ ] File any bugs found from user feedback
- [ ] Thank everyone who tries it, even if they have issues

**Day 4-5:**
- [ ] Post to r/dotnet (different angle: Native AOT showcase)
- [ ] Submit to Dev.to (longer technical article)
- [ ] LinkedIn post (enterprise/compliance angle)

### Post-Launch: Weeks 1-4

- [ ] Respond to all issues within 72 hours
- [ ] Merge or close all PRs within 1 week
- [ ] Fix any bugs reported by real users (highest priority)
- [ ] Submit to TLDR Newsletter and Console.dev
- [ ] Write "Architecture of AgentGit" Dev.to post (Week 2-3)
- [ ] Celebrate first external contributor (if any)
- [ ] Tag v1.0.1 or v1.1.0 if meaningful fixes/features land

### Month 2-3: Sustained Presence

- [ ] Monthly: Check for stale issues, update dependencies, cut a release
- [ ] Submit conference CFPs for talks in Month 4-6
- [ ] Write "Native AOT for CLI Tools" post targeting .NET community
- [ ] Add "good first issue" labels to appropriate issues
- [ ] Monitor GitHub traffic / referral sources -- double down on what works

### Month 4-6: Growth or Steady State

- [ ] If growing: Consider Discord, more content, seek co-maintainer
- [ ] If steady: Focus on quality, keep dependencies updated, respond to users
- [ ] Present at a meetup or conference (even virtual/local)
- [ ] Evaluate: Has the pattern caught on? Are other tools emerging? Adjust strategy.
- [ ] Consider GitHub Sponsors profile if there's user demand

### Month 7-12: Evaluate and Adjust

- [ ] Annual "State of AgentGit" post (blog or GitHub Discussion)
- [ ] Evaluate if scope should expand (other git hosts? GitLab? Bitbucket?)
- [ ] If contributors exist: formalize Core Team governance
- [ ] If no traction: That's fine. The tool works for you. Keep it maintained.

---

## 9. Repository Setup

### Required Files (Status)

| File | Status | Notes |
|------|--------|-------|
| `README.md` | Done | Comprehensive, includes setup walkthrough |
| `LICENSE` | Done | MIT, copyright Christopher Anderson |
| `CONTRIBUTING.md` | Done | Dev setup, conventions, PR process |
| `CODE_OF_CONDUCT.md` | Done | Contributor Covenant |
| `SECURITY.md` | Done | Scope, response timeline, contact |
| `GOVERNANCE.md` | **TODO** | BDFL model, succession plan |
| `.editorconfig` | Done | Code style enforcement |
| `packages.lock.json` | Done | Pinned dependency graph |
| `appsettings.json.example` | Done | Template config (real config is gitignored) |

### CI/CD Workflows (Status)

| Workflow | Status | Purpose |
|----------|--------|---------|
| `build-publish.yaml` | Done | Build, test, SLSA, Cosign signing via Oui Deliver |
| `auto-pr.yaml` | Done | Auto-PR on branch push |
| `claude-code-review.yml` | Done | AI-assisted code review |
| `merge-checks.yaml` | Done | Branch protection enforcement |
| `check-licenses-action/` | Done | License compliance scanning |

### TODO Before Launch

| Item | Priority | Effort |
|------|----------|--------|
| Enable GitHub Discussions | High | 5 min |
| Enable private vulnerability reporting | High | 5 min |
| Add `.github/dependabot.yml` | High | 10 min |
| Create issue templates | Medium | 20 min |
| Add GOVERNANCE.md | Medium | 30 min |
| Add GitHub Topics | Medium | 5 min |
| Add README badges | Low | 15 min |

---

## 10. Corporate Approval Path

### Context

AgentGit lives in the `innago-property-management` GitHub org but is Christopher Anderson's personal project. The corporate relationship is:

- **Code:** 100% written for this project, no Innago proprietary code
- **CI/CD:** Uses Innago's Oui Deliver (shared infrastructure, not proprietary)
- **GitHub App:** `stand-sure-ai` is a personal GitHub App, installed on both personal and Innago orgs
- **IP:** MIT license, copyright held by Christopher Anderson personally

### Approval Checklist

- [x] No proprietary Innago code in repository
- [x] No customer data, PII, or internal references
- [x] No hardcoded credentials or secrets
- [x] Git history is clean (not a fork of internal repo)
- [x] License (MIT) is universally approved
- [x] CI/CD infrastructure usage is approved (Oui Deliver is designed for this)
- [x] No competitive advantage concern (AgentGit is not Innago's business)
- [ ] Verbal/written acknowledgment from Innago leadership that OSS release is fine

### Intellectual Property Notes

- Copyright line reads `Christopher Anderson`, not `Innago Property Management`
- This is appropriate for a personal project hosted in an org for infrastructure convenience
- If ownership ever needs to transfer, MIT license means the code is perpetually available regardless

---

## Appendix A: Pre-Release Checklist

### Legal
- [x] License file present and correct (MIT)
- [x] Copyright holder identified (Christopher Anderson)
- [x] No proprietary code exposed
- [x] No customer data or PII
- [x] No internal domain references or hardcoded URLs
- [x] Git history clean
- [x] Dependency licenses compatible (all MIT)
- [ ] Corporate acknowledgment obtained (verbal or written)

### Security
- [x] No secrets in code or git history
- [x] SECURITY.md with reporting process and scope
- [x] Automated security scanning in CI (SAST, secrets, license)
- [x] Binary signing (Cosign) and provenance (SLSA Level 3)
- [x] Private key handling reviewed (permission checks, memory zeroing)
- [x] Token handling reviewed (GIT_ASKPASS, no CLI args)
- [ ] GitHub private vulnerability reporting enabled
- [ ] Dependabot configured for NuGet

### Technical
- [x] README is comprehensive and accurate
- [x] Build instructions work from clean clone
- [x] Tests pass (28 tests, xunit.v3)
- [x] CI/CD configured and passing
- [x] v1.0.0 tagged
- [x] TreatWarningsAsErrors enabled (zero warnings)
- [x] Native AOT publishes successfully
- [x] appsettings.json.example provided

### Community
- [x] CONTRIBUTING.md with dev setup, conventions, PR process
- [x] CODE_OF_CONDUCT.md (Contributor Covenant)
- [ ] GOVERNANCE.md with succession plan
- [ ] GitHub Discussions enabled
- [ ] Issue templates created (Bug Report, Feature Request)
- [ ] GitHub Topics set
- [ ] README badges added (optional but nice)

### Marketing
- [ ] HN title drafted
- [ ] Twitter/X thread drafted
- [ ] r/ClaudeAI post drafted
- [ ] Launch day selected (Tuesday-Thursday)

---

## Appendix B: Communication Templates

### Launch Announcement (Hacker News)

**Title:** `Show HN: AgentGit -- Give AI agents their own bot identity on GitHub`

**If self-post needed:**
> AI agents (Claude Code, Copilot, Cursor) commit code as the human running them. That works for solo devs but breaks down fast -- you can't tell human commits from machine commits, there's no audit trail, and your git history becomes meaningless.
>
> AgentGit authenticates as a GitHub App and commits as a proper [bot] identity, the same way Dependabot and Renovate work. It's a single Native AOT binary (~8MB), takes 5 minutes to set up, and works across multiple GitHub orgs.
>
> For Claude Code users, a hook transparently intercepts `git commit` -- agents don't even know it's there.
>
> MIT license, .NET 10, zero runtime dependencies. Happy to answer questions about the architecture, the GitHub App JWT flow, or anything else.

### Launch Announcement (Twitter/X Thread)

> 1/ AI agents are committing code as you. Your git history can't tell human from machine. That's a problem.
>
> 2/ AgentGit gives AI agents their own verified bot identity on GitHub. Commits show up as YourApp[bot] -- just like Dependabot, but for any AI agent.
>
> 3/ How it works: GitHub App JWT auth -> installation token -> git commit with bot identity -> git push via GIT_ASKPASS (token never in process args)
>
> 4/ For Claude Code users: a hook intercepts `git commit` transparently. Your agent just runs `git commit` and gets bot identity automatically. Zero friction.
>
> 5/ Single binary (~8MB), .NET 10 Native AOT, zero runtime deps, MIT license. Five minutes to set up.
>
> GitHub: [link]
>
> Built this because I needed it. Sharing in case you do too.

### Security Advisory Template

```markdown
# Security Advisory: AGENTGIT-YYYY-NNN

**Severity:** [Critical/High/Medium/Low]
**CVSS Score:** [0.0-10.0]
**Affected Versions:** [version range]
**Fixed in Version:** [version]

## Summary
[One-line description]

## Impact
[What an attacker could do]

## Mitigation
[Workaround if patch not immediately available]

## Resolution
Update to version [X.Y.Z]:
    dotnet publish src/AgentGit/AgentGit.csproj -c Release -o bin

## Timeline
- YYYY-MM-DD: Issue reported
- YYYY-MM-DD: Issue confirmed
- YYYY-MM-DD: Fix released
- YYYY-MM-DD: Public disclosure

## Credits
[Reporter name if they wish to be credited]
```

### Issue Response Templates

**Bug report acknowledgment:**
> Thanks for reporting this. I can reproduce the issue on [env]. I'll have a fix in [timeframe]. In the meantime, [workaround if any].

**Feature request (in scope):**
> Good idea. This fits the project's direction. I've added it to the roadmap. PRs welcome if you want to take a crack at it -- happy to provide guidance on the approach.

**Feature request (out of scope):**
> Appreciate the suggestion. This is outside AgentGit's core scope (bot identity for git commits). You might want to look at [alternative] for this use case. I'm going to close this, but feel free to make the case in Discussions if you think I'm wrong.

---

## Appendix C: What AgentGit Is NOT

Useful for scope management and responding to feature requests:

- **Not a git client.** AgentGit wraps `git commit` and `git push` with bot identity. It doesn't replace git.
- **Not a CI/CD tool.** It runs locally (or in CI) but doesn't manage pipelines.
- **Not a GitHub App manager.** It uses one GitHub App for authentication. It doesn't create or configure apps.
- **Not an agent framework.** It doesn't orchestrate agents. It gives them an identity for git operations.
- **Not multi-platform (yet).** It targets GitHub. GitLab/Bitbucket support is not planned for v1 but could be a community contribution.
- **Not a secret manager.** It reads a private key from disk. Key management is your responsibility.
