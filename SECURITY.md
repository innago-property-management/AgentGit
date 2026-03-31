# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| Latest on `main` | Yes |

## Reporting a Vulnerability

If you discover a security vulnerability in AgentGit, please report it responsibly.

**Do not open a public GitHub issue for security vulnerabilities.**

Instead, please email: **security@innago.com**

Include:
- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if any)

## Response Timeline

- **Acknowledgment:** Within 48 hours
- **Assessment:** Within 1 week
- **Fix/Advisory:** Depends on severity, typically within 2 weeks for critical issues

## Security Model

AgentGit handles sensitive credentials:

- **Private keys** — RSA keys for GitHub App JWT generation. AgentGit validates file permissions (rejects world-readable keys) and zeros key material from memory after use.
- **Installation tokens** — Short-lived GitHub API tokens. Passed via `GIT_ASKPASS` environment variable, never exposed in process arguments or URLs.
- **Credential isolation** — Credential helpers are disabled during push to prevent keychain interference.

## Scope

The following are in scope for security reports:
- Token or key exposure (in logs, process args, URLs, temp files)
- Command injection via git arguments
- Privilege escalation
- Authentication bypass
- Insecure temp file handling

The following are out of scope:
- Attacks requiring local root access
- Social engineering
- Denial of service against GitHub's API
