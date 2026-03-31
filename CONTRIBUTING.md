# Contributing to AgentGit

Thanks for your interest in contributing! Here's how to get started.

## Development Setup

1. Install [.NET 10 SDK](https://dotnet.microsoft.com/download)
2. Clone the repo and build:
   ```bash
   git clone https://github.com/innago-property-management/AgentGit.git
   cd AgentGit
   dotnet build AgentGit.slnx
   ```
3. Run tests:
   ```bash
   dotnet test AgentGit.slnx
   ```

## Making Changes

1. Fork the repo and create a feature branch from `main`
2. Write tests first (TDD preferred)
3. Make your changes
4. Ensure all tests pass and build has zero warnings:
   ```bash
   dotnet build AgentGit.slnx -c Release
   dotnet test AgentGit.slnx
   ```
5. Open a pull request against `main`

## Code Conventions

- **C#/.NET 10** with nullable reference types enabled
- `TreatWarningsAsErrors` is on — fix all warnings
- **Native AOT compatible** — no reflection, use `JsonSerializerContext` for JSON
- **LoggerMessage source generator** for structured logging (see `LoggerMessages.cs`)
- All new classes should be `internal` unless there's a specific reason for `public`
- Interfaces for testability (`IGitProcessRunner`, `IGitHubAppAuthenticator`)
- Prefer `ArgumentList` over string concatenation for process arguments

## Testing

- **xunit.v3** for test framework
- **Moq** for mocking interfaces
- **AwesomeAssertions** for fluent assertions
- **AutoFixture** for test data generation
- Tests live in `test/AgentGit.Tests/`

## Pull Request Process

1. PRs trigger automated CI (build, test, license check, SAST, secrets scan)
2. All checks must pass before merge
3. Squash merge to `main`

## Security

If you discover a security vulnerability, do **not** open a public issue. See [SECURITY.md](SECURITY.md) for responsible disclosure instructions.
