# Feval — Notes for Claude

## Layout
- `Feval.Core/` — evaluator library. **Targets .NET Framework 4.7.1**, `LangVersion` pinned to 9. Building requires the v4.7.1 targeting pack (MSB3644 otherwise).
- `Feval.Cli/` — CLI, packaged as a `dotnet tool` (command name `feval`). Multi-targets `net6.0;net7.0;net8.0;net9.0`. `ImplicitUsings` is on.
- `Feval.UnitTests/` — xUnit tests against `Feval.Core`.

## Common commands
- Build: `dotnet build Feval.sln`
- Test: `dotnet test Feval.UnitTests/Feval.UnitTests.csproj`
- Run CLI: `dotnet run --project Feval.Cli -f net9.0 -- <args>`

## CLI shape
- Verbs (see `Feval.Cli/Options.cs`): `run` (default), `using`, `alias`, `config`.
- `run` modes: standalone (`--standalone`), remote client (default), non-REPL (`-e`, `-f`, or piped stdin), LAN scan (`-s`).
- Interactive input goes through the `ReadLine` NuGet package (tonerdo/readline 2.0.1).
- Ctrl+C is handled via `Console.CancelKeyPress` in `Program.cs`. The handler must set `e.Cancel = true` and call `Environment.Exit(0)` explicitly — relying on .NET's default terminate-after-handler silently breaks under PowerShell 7.x + ConPTY, so do not revert to that path.
- Remote protocol is MsgPack over TCP; framing / AES / RSA / LZ4 / ZLib helpers live in `Feval.Cli/Network/`. The client negotiates protocol version on connect (`NegotiateProtocolAsync`).

## Options and state
- User options JSON: `%APPDATA%\Feval.Cli\options.json` (`Environment.SpecialFolder.ApplicationData` + `Feval.Cli`).
- History is trimmed to `history.max` (default 50) on write. Default remote port is `9999`.
- The history-save path in each runner's `Quit()` must block on the write (`.GetAwaiter().GetResult()`) — otherwise `Environment.Exit(0)` tears the process down before the async file write lands.

## Things to be careful of in `Feval.Core`
- `netfx 4.7.1` + C# 9 only. No records, file-scoped namespaces, `System.Text.Json`, or other C# 10+ / netstandard 2.1+-only APIs.
- Compile items are declared explicitly in the csproj (`<Compile Include="..." />`), not globbed. Adding a new source file there requires editing the csproj.

## Commit conventions
- Subject prefixes are lower-case: `feature:`, `fix:`, `chore:`. Imperative mood, under ~72 chars.
- **Do not append a `Co-Authored-By` trailer.**
- Version bumps go in their own `chore: Bump ...` commit — never mixed into a feature/fix commit.
