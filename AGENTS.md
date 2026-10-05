# AGENTS.md

## Commands

- `dotnet build` is the lint: analyzers run in the build and warnings are errors.
- `dotnet test`; one class: `dotnet test -- --filter-class "*TranslateSlashCommandHandlerTests"`.
- After changing packages, commit every updated `packages.lock.json`; CI restores with `--locked-mode`.

## Architecture

- Mediator is the source-generated [martinothamar/Mediator](https://github.com/martinothamar/Mediator), not MediatR:
  handlers return `ValueTask` and the generator registers them.
- `DiscordEventListener` publishes each Discord event as a notification, and every handler for it runs concurrently. A
  handler first checks the notification is its own (e.g. command name) and returns early otherwise.

## Conventions

- Log IDs, names, and counts only; message contents stay out of logs and traces (a privacy promise in the README).
- Keep the README in sync: both config examples (user-secrets JSON, `Section__Key` env vars) with the options classes,
  and Features with the handlers' delays and limits.
- Discord API calls pass `options: new RequestOptions { CancelToken = cancellationToken }`, and bot messages pass
  `allowedMentions: AllowedMentions.None`.

## Tests

- xUnit v3 on Microsoft.Testing.Platform, NSubstitute, AwesomeAssertions (`.Should()`).
- `Method_Scenario_Expected` names, `// Arrange` / `// Act` / `// Assert`, class under test in `_sut`.
- Use `LoggerFake<T>` for loggers.
