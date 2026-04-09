# DevCrew CLI

`crew` is a command-line companion tool for the DevCrew desktop application. It provides quick access to productivity utilities — such as GUID generation and management — directly from your terminal.

---

## Installation

### Build from source

```bash
dotnet build src/DevCrew.Cli/DevCrew.Cli.csproj
```

### Publish as a self-contained binary (recommended)

```bash
dotnet publish src/DevCrew.Cli/DevCrew.Cli.csproj \
  -c Release -r osx-arm64 --self-contained
```

Replace `osx-arm64` with your target runtime identifier (e.g. `linux-x64`, `win-x64`).

After publishing, add the output directory to your `PATH` or copy the `crew` binary to a location already on your `PATH`.

---

## General Usage

```
USAGE:
    crew [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help       Prints help information
    -v, --version    Prints version information

COMMANDS:
    guid    Generate, list, or delete GUIDs
```

Run `crew guid --help` to see subcommand-specific options.

---

## Commands

### `crew guid` — Generate a GUID

`guid` is the default branch command. Running `crew guid` without a subcommand generates a new GUID.

```
USAGE:
    crew guid [OPTIONS]

OPTIONS:
    -c, --copy          Copy the generated GUID to the clipboard
    -s, --save [NAME]   Save the GUID, optionally associated with a name/label
    -h, --help          Prints help information
```

#### Examples

Generate a GUID and print it:

```bash
crew guid
# Generated Guid: 3f2504e0-4f89-11d3-9a0c-0305e82c3301
```

Generate and copy to clipboard:

```bash
crew guid --copy
```

Generate and save without a label:

```bash
crew guid --save
```

Generate, save with a label, and copy to clipboard:

```bash
crew guid --save "my-api-key" --copy
```

> **Note:** `--save` accepts an optional value. Omitting the value saves the GUID without an associated label.

---

### `crew guid list` — List saved GUIDs

```
USAGE:
    crew guid list [OPTIONS]

OPTIONS:
    -c, --count <N>      Number of GUIDs to display (default: 5)
    -s, --search <TEXT>  Filter results and highlight matches
    -h, --help           Prints help information
```

#### Examples

List the 5 most recent GUIDs:

```bash
crew guid list
```

List the 10 most recent GUIDs:

```bash
crew guid list --count 10
```

Search for GUIDs matching a value or note:

```bash
crew guid list --search "my-api"
```

Combine count and search:

```bash
crew guid list --count 20 --search "prod"
```

---

### `crew guid delete` — Delete a saved GUID

At least one of `--value` or `--notes` must be provided. If multiple matches are found, the command will print them and ask you to narrow the criteria.

```
USAGE:
    crew guid delete [OPTIONS]

OPTIONS:
    -v, --value [VALUE]   Match the GUID to delete by its value
    -n, --notes [NOTES]   Match the GUID to delete by its associated notes
    -h, --help            Prints help information
```

#### Examples

Delete by exact GUID value:

```bash
crew guid delete --value "3f2504e0-4f89-11d3-9a0c-0305e82c3301"
```

Delete by associated notes/label:

```bash
crew guid delete --notes "my-api-key"
```

Narrow by both value and notes:

```bash
crew guid delete --value "3f2504e0-4f89-11d3-9a0c-0305e82c3301" --notes "my-api-key"
```

---

## Notes

### Clipboard support

The `--copy` flag relies on platform-native clipboard tooling:

| Platform | Tool used                                |
|----------|------------------------------------------|
| macOS    | `pbcopy`                                 |
| Linux    | `wl-copy` → `xclip` → `xsel` (fallback) |
| Windows  | `clip` / PowerShell fallback             |

If clipboard access fails, `crew guid --copy` will display a warning but still exit successfully and print the generated GUID.

### Exit codes

| Code | Meaning                              |
|------|--------------------------------------|
| `0`  | Success                              |
| `1`  | Error (e.g. no matching GUID found, missing required options) |
