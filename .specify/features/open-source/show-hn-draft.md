# Show HN Draft

## Title

```
Show HN: AgentGit – Give AI agents their own bot identity on GitHub
```

## Text (Post-Delphi revision)

```
AI agents (Claude Code, Copilot, Cursor) commit code as the human running
them. That works for solo devs but breaks down fast — you can't tell human
commits from machine commits, there's no audit trail, and your git blame
stops telling the truth.

AgentGit authenticates as a GitHub App and commits as a proper [bot] identity,
the same way Dependabot and Renovate work. Single native binary (~8MB, no
runtime needed), takes 5 minutes to set up, works across multiple GitHub orgs.

How it works:

- Create a GitHub App (free, 5 min)
- AgentGit generates a JWT, exchanges it for an installation token
- Commits as YourApp[bot] with the proper @users.noreply.github.com email
- Pushes via GIT_ASKPASS so the token never appears in process args

For Claude Code users, a hook transparently intercepts `git commit` — agents
don't even know it's there. They just run `git commit` and get bot identity
automatically.

I built this because I run autonomous AI agents across multiple repos and
orgs, and I needed to know what was human and what was machine. Nothing
purpose-built existed, so I wrote it. MIT license, built with .NET 10
Native AOT.

https://github.com/innago-property-management/AgentGit
```

## Posting Notes

- **Best time:** Tuesday 8-10 AM EST (highest HN traffic for dev tools)
- **fnox has:** YCOMBINATOR_USERNAME, YCOMBINATOR_PASSWORD
- **URL field:** Use the GitHub repo link directly (no self-post needed if HN allows URL posts for Show HN)
- **If URL post:** Title links to repo, no body text needed (but be ready to comment with the text above as the first comment)
- **First comment strategy:** Post the detailed text as the first comment immediately after submitting, then respond to questions promptly for the first 2-3 hours

## Anticipated Questions (prep answers)

**"Why .NET? Why not Go/Rust for a CLI tool?"**
> .NET 10 Native AOT produces the same kind of self-contained binary as Go or Rust — 8MB, no runtime, fast startup. I chose it because I'm a .NET developer and wanted it to exist, not because the language matters for this use case. The architecture is simple enough to port if someone wants a Go or Rust version.

**"Why not just use git config?"**
> `git config user.name "bot"` sets the author but GitHub doesn't verify it. Anyone can commit as anyone. A GitHub App [bot] identity is verified by GitHub — the commit shows the bot badge and links to the app. It's the difference between writing "Dependabot" in the author field and actually being Dependabot.

**"Does this work with GitLab/Bitbucket?"**
> Not today — it's GitHub-specific because it uses the GitHub App JWT flow. The architecture (generate credential → commit with identity → push with token) could be adapted for other forges. PRs welcome.

**"Isn't this overkill for a solo dev?"**
> If you're the only person and the only agent, maybe. But the moment you have a second agent, or a teammate, or need to answer "did a human review this?", you'll want it. The setup is 5 minutes — less than the time you'll spend explaining a mystery commit.
