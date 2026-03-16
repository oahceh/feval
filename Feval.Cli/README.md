# Feval.Cli

A command-line tool for evaluating C# expressions, supporting both standalone and remote modes.

## Installation

```shell
dotnet tool install --global Feval.Cli
```

## Usage

### Standalone Mode

Standalone mode runs a local C# expression evaluator without connecting to a remote service.

#### Interactive REPL

```bash
feval run --standalone
```

Launches an interactive REPL with prompt (`>>`), version info, command history, and `quit()` support.

#### Evaluate Expressions (-e)

```bash
# Single expression
feval run --standalone -e "1 + 2"
# Output: 3

# Multiple expressions (share context)
feval run --standalone -e "var x = 10" -e "x * 2"
# Output: 20
```

#### Execute Script File (-f)

```bash
feval run --standalone -f script.txt
```

Executes each line in the file as a C# expression sequentially.

#### Stdin Pipe

```bash
echo "1 + 2" | feval run --standalone
```

When stdin is redirected (piped), expressions are read line by line from stdin.

### Remote Mode

Remote mode connects to a running Feval service for expression evaluation.

#### Interactive REPL

```bash
# Connect to default address (127.0.0.1:9999)
feval run

# Connect to specific address
feval run 192.168.1.100:8888

# Scan for available services
feval run -s
```

#### Non-Interactive (same as standalone)

```bash
feval run -e "1 + 2"
feval run -e "var x = 10" -e "x * 2"
feval run -f script.txt
echo "1 + 2" | feval run
```

### Options

| Option | Description |
|---|---|
| `--standalone` | Run in standalone mode (local evaluator) |
| `-e`, `--eval` | Evaluate expression(s) and exit |
| `-f`, `--file` | Execute expressions from a script file and exit |
| `-v`, `--verbose` | Show tokens and syntax tree (standalone only) |
| `-s`, `--scan` | Scan for available remote services |

### Behavior Rules

- `-e` and `-f` can be combined: `-e` expressions execute first, then `-f` file
- `-e`/`-f` take priority over stdin — when present, stdin is ignored
- Non-interactive mode outputs only expression results (no prompt, no banner)
- Errors are written to stderr with a non-zero exit code
- Default `using` namespaces (configured via `feval using`) are applied in all modes

### Meta Commands (Remote REPL)

In remote interactive mode, prefix commands with `#`:

- `#load <path>` — Load and execute a script file
- `#dpf <expr> <path>` — Dump expression result to file

## Configuration

```bash
# Manage default using namespaces
feval using -a System.Linq System.Collections.Generic
feval using -r System.Linq
feval using -c    # clear all
feval using       # list current

# Manage remote address aliases
feval alias myserver 192.168.1.100:9999

# Configuration
feval config -l                    # list all
feval config port.default 8888     # set default port
feval config history.max 100       # set max history
```
