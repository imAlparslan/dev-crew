# DevCrew

DevCrew is a cross-platform developer utility toolkit available as a desktop application, a command-line interface (`crew`), and a Model Context Protocol (MCP) server (`devcrew-mcp`). Use the same set of practical tools from a graphical interface, your terminal, or an MCP-compatible AI client.

## Features

- **GUID management** — Generate GUIDs, optionally save them with notes, search saved records, and update or delete them.
- **Base64** — Encode text or file contents and decode Base64 text or binary data.
- **JSON** — Validate, prettify, minify, and sort JSON; compare documents with configurable rules for object properties, arrays, whitespace, and null values.
- **JWT** — Decode tokens and optionally validate signatures; build and sign tokens with standard and custom claims; save and manage reusable templates.
- **Regular expressions** — Match patterns with case-insensitive and multiline options; create, search, update, and delete reusable presets.
- **Desktop experience** — Use dedicated screens for each utility, configure the interface language and fonts, and access MCP client configuration from the app.
- **Shared local data** — Saved GUIDs, JWT templates, regex presets, and application settings are stored in a local SQLite database and can be reused by the app's interfaces.

## Getting started

### Desktop app

Install the latest Windows `.msix` or macOS `.pkg` release from the project's releases. The macOS installer includes the desktop app, `crew`, and `devcrew-mcp`; the Windows package registers the `crew` command. See the [CLI guide](src/DevCrew.Cli/README.md) for installation details and command reference.

### CLI

After installing a release or building the CLI, run:

```bash
crew --help
crew --version
```

The main command groups are `guid`, `base64`, `json`, `jwt`, and `regex`. For example:

```bash
# Generate and save a GUID with a label
crew guid --save "my-service"

# Format JSON from a file
crew json format --input-path ./payload.json --prettify --sort

# Decode a JWT
crew jwt decode "<token>"

# Match a regular expression
crew regex match --pattern "\w+@\w+\.\w+" --input "dev@example.com"
```

For command options, additional examples, and release installation instructions, see the [CLI README](src/DevCrew.Cli/README.md).

### MCP server

DevCrew's MCP server uses **stdio transport** and exposes the toolkit to MCP-compatible clients. Its tools cover GUID creation and saved-record management, Base64 encoding and decoding, JSON validation/formatting/comparison, JWT decoding/building and template management, and regex matching and preset management.

The desktop app's **MCP** screen can detect the server executable, generate configuration for VS Code, Claude Desktop, or Cursor, copy the configuration, and open the VS Code MCP installation flow.

#### Configure an installed server

Add the server to your MCP client's configuration. Use the path to the installed executable on your machine:

**VS Code** (`.vscode/mcp.json`):

```json
{
  "servers": {
    "devcrew": {
      "type": "stdio",
      "command": "/absolute/path/to/devcrew-mcp",
      "args": []
    }
  }
}
```

**Claude Desktop or Cursor**:

```json
{
  "mcpServers": {
    "devcrew": {
      "command": "/absolute/path/to/devcrew-mcp",
      "args": []
    }
  }
}
```

On Windows, set `command` to the full path of `DevCrew.Mcp.exe` (for example, `C:\\path\\to\\DevCrew.Mcp.exe`). On macOS, the release installer places `devcrew-mcp` in `/usr/local/bin`.

#### Build and run the MCP server

The MCP project targets .NET 10 and publishes a self-contained, single-file executable. For example, publish for Windows:

```bash
dotnet publish src/DevCrew.Mcp/DevCrew.Mcp.csproj -c Release -r win-x64 --self-contained
```

Choose a different runtime identifier for another platform, then set the MCP client's `command` to the published executable. To run directly from source during development:

```bash
dotnet run --project src/DevCrew.Mcp/DevCrew.Mcp.csproj
```

The MCP server communicates over standard input/output; do not redirect its protocol output or configure it as an HTTP server.

## Build and test

Requirements: .NET SDK 10.0.100 or a compatible later feature release.

```bash
dotnet build DevCrew.slnx
dotnet test DevCrew.slnx
```

To run the desktop application from source:

```bash
dotnet run --project src/DevCrew.Desktop/DevCrew.Desktop.csproj
```

## Projects

| Project | Description |
| --- | --- |
| `src/DevCrew.Desktop` | Avalonia desktop application |
| `src/DevCrew.Cli` | `crew` command-line application |
| `src/DevCrew.Mcp` | `devcrew-mcp` stdio MCP server |
| `src/DevCrew.Core` | Shared business logic, persistence, and services |
| `tests/DevCrew.Core.Tests` | Core service and behavior tests |
