# DevCrew CLI

`crew` is a command-line companion tool for the DevCrew desktop application. It provides quick access to productivity utilities — such as GUID generation, JWT decoding, and token validation — directly from your terminal.

---

## Installation

### macOS release installer

Tagged macOS releases now ship a `DevCrew.pkg` installer.

Running the installer places:

- `DevCrew.app` in `/Applications`
- `crew` in `/usr/local/bin`
- `devcrew-mcp` in `/usr/local/bin`

After installation, open a new terminal session and run `crew --version` to verify the CLI is available on your `PATH`.

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
    base64  Encode/decode Base64 payloads
    guid    Generate, list, update, or delete GUIDs
    json    Format and process JSON payloads
    jwt     Decode, build, and manage JWT templates
    regex   Run regex matching and preset utilities
```

Run `crew base64 --help` to see Base64-specific options.
Run `crew guid --help` to see subcommand-specific options.
Run `crew json --help` to see JSON-specific options.
Run `crew jwt --help` to see JWT-specific options.
Run `crew regex --help` to see Regex-specific options.

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

### `crew base64 encode` — Encode To Base64

Encode inline text or file bytes to Base64 output.

```
USAGE:
    crew base64 encode (--input <TEXT> | --input-path <PATH>) [OPTIONS]

OPTIONS:
    -i, --input <TEXT>          Inline text input
    --input-path <PATH>         Read input bytes from file path
    -c, --copy                  Copy encoded output to clipboard
    --save <PATH>               Save encoded output to file
    -h, --help                  Prints help information
```

#### Examples

Encode inline text:

```bash
crew base64 encode -i "hello world"
```

Encode file contents and save result:

```bash
crew base64 encode --input-path ./payload.bin --save ./payload.b64
```

---

### `crew base64 decode` — Decode Base64

Decode Base64 input to text output or binary file output.

```
USAGE:
    crew base64 decode (--input <BASE64> | --input-path <PATH>) [OPTIONS]

OPTIONS:
    -i, --input <BASE64>        Inline Base64 input
    --input-path <PATH>         Read Base64 input from file path
    --output-path <PATH>        Save decoded bytes to file
    -c, --copy                  Copy decoded text output to clipboard (without --output-path)
    -h, --help                  Prints help information
```

#### Examples

Decode inline Base64 to text:

```bash
crew base64 decode -i "aGVsbG8gd29ybGQ="
```

Decode Base64 file to binary output:

```bash
crew base64 decode --input-path ./payload.b64 --output-path ./payload.bin
```

---

### `crew regex match` — Match Regex Pattern

Run regex matching against inline text or file input and display matches/captures.

```
USAGE:
    crew regex match (--pattern <PATTERN> | --template <NAME>) (--input <TEXT> | --input-path <PATH>) [OPTIONS]

OPTIONS:
    -t, --template <NAME>       Use a saved regex pattern template by name
    -p, --pattern <PATTERN>    Regex pattern
    -i, --input <TEXT>         Inline input text
    --input-path <PATH>        Read input text from file path
    --ignore-case              Enable case-insensitive mode
    -m, --multiline            Enable multiline mode
    --save-template <NAME>     Save effective regex pattern as template
    -c, --copy                 Copy full report to clipboard
    --save <PATH>              Save full report to file
    -h, --help                 Prints help information
```

#### Examples

Inline match:

```bash
crew regex match -p "cat" -i "cat scatter cat"
```

Named captures:

```bash
crew regex match -p "(?<word>\\w+)-(?<digits>\\d+)" -i "item-42"
```

File input with flags:

```bash
crew regex match --pattern "^foo" --input-path ./sample.txt --multiline --ignore-case
```

Save a reusable pattern template:

```bash
crew regex match --pattern "(?<word>\\w+)-(?<digits>\\d+)" --input "item-42" --save-template word-digit
```

Use a saved pattern template:

```bash
crew regex match --template word-digit --input "order-9000"
```

---

### `crew regex list` — List Saved Regex Presets

List saved regex presets and optionally filter them by name.

```
USAGE:
    crew regex list [OPTIONS]

OPTIONS:
    -n, --name <TEXT>  Filter presets by name
    -h, --help         Prints help information
```

#### Examples

List all presets:

```bash
crew regex list
```

Filter presets by name:

```bash
crew regex list --name word
```

---

### `crew regex update` — Update A Saved Regex Preset

Update a saved regex preset by name. You can replace the pattern, switch case sensitivity, and toggle multiline mode.

```
USAGE:
    crew regex update <NAME> [OPTIONS]

OPTIONS:
    -p, --pattern <PATTERN>  Replace the preset pattern
    --ignore-case            Enable case-insensitive matching
    --case-sensitive         Disable case-insensitive matching
    -m, --multiline          Enable multiline mode
    --singleline-input       Disable multiline mode
    -h, --help               Prints help information
```

#### Examples

Replace the saved pattern:

```bash
crew regex update word-digit --pattern "(?<word>\\w+)-(?<digits>\\d+)"
```

Enable case-insensitive and multiline matching:

```bash
crew regex update word-digit --ignore-case --multiline
```

Disable case-insensitive matching:

```bash
crew regex update word-digit --case-sensitive
```

---

### `crew regex delete` — Delete A Saved Regex Preset

Delete a saved regex preset by name.

```
USAGE:
    crew regex delete <NAME>

OPTIONS:
    -h, --help  Prints help information
```

#### Examples

Delete a preset:

```bash
crew regex delete word-digit
```

---

### `crew json format` — Format JSON

Format JSON input as prettified or minified output, optionally sort keys, copy output, or save to file.

```
USAGE:
    crew json format (--input <JSON> | --input-path <PATH>) [OPTIONS]

OPTIONS:
    -i, --input <JSON>          Input JSON string to format
    --input-path <PATH>         Read input JSON from file path
    -p, --prettify, --pretify   Prettify JSON output
    -m, --minify                Minify JSON output
    -s, --sort                  Sort JSON object keys alphabetically
    -c, --copy                  Copy formatted output to clipboard
    --save <PATH>               Save formatted output to file
    -h, --help                  Prints help information
```

#### Examples

Prettify JSON:

```bash
crew json format --input "{\"name\":\"devcrew\",\"enabled\":true}" --prettify
```

Minify and sort keys:

```bash
crew json format -i "{\"z\":1,\"a\":2}" -m -s
```

Prettify and copy output:

```bash
crew json format -i "{\"name\":\"devcrew\"}" -p -c
```

Prettify and save output to file:

```bash
crew json format -i "{\"name\":\"devcrew\"}" -p --save ./formatted.json
```

Read input from file path:

```bash
crew json format --input-path ./payload.json --prettify --sort
```

---

### `crew json diff` — Compare JSON

Compare two JSON inputs and print a summary with path-level differences.

```
USAGE:
    crew json diff [OPTIONS]

OPTIONS:
    -l, --left-input <JSON>         Left JSON input string
    --left-input-path <PATH>        Read left JSON input from file path
    -r, --right-input <JSON>        Right JSON input string
    --right-input-path <PATH>       Read right JSON input from file path
    --ignore-object-property-order  Ignore object property ordering differences (default)
    --respect-object-property-order Treat object property ordering differences as meaningful
    --treat-array-order-as-significant
                                    Treat array item ordering differences as meaningful (default)
    --ignore-array-order            Ignore array item ordering differences
    --ignore-whitespace-differences Ignore formatting and whitespace-only differences (default)
    --respect-whitespace-differences
                                    Treat formatting and whitespace-only differences as meaningful
    --treat-null-and-empty-string-as-equal
                                    Treat null and empty string values as equivalent
    -h, --help                      Prints help information
```

#### Examples

Compare inline JSON values:

```bash
crew json diff -l "{\"a\":1}" -r "{\"a\":2}"
```

Compare file-based JSON inputs:

```bash
crew json diff --left-input-path ./left.json --right-input-path ./right.json
```

Compare mixed inputs:

```bash
crew json diff --left-input-path ./left.json -r "{\"a\":1,\"b\":2}"
```

Ignore array ordering:

```bash
crew json diff -l "{\"items\":[1,2,3]}" -r "{\"items\":[3,2,1]}" --ignore-array-order
```

Treat `null` and empty string as equal:

```bash
crew json diff -l "{\"value\":null}" -r "{\"value\":\"\"}" --treat-null-and-empty-string-as-equal
```

---

### `crew jwt decode` — Decode a JWT

Decode a JWT token and optionally validate its signature.

```
USAGE:
    crew jwt decode <TOKEN> [OPTIONS]

OPTIONS:
    -s, --secret <SECRET>  Secret or public key used to validate the token signature
    -h, --help             Prints help information
```

#### Examples

Decode a token:

```bash
crew jwt decode "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjMifQ."
```

Decode and validate the signature with a secret:
```bash
crew jwt decode "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjMifQ." --secret "your-secret-key"
```

When `--secret` is provided, the command prints both the decoded token contents and the signature validation result.

---

### `crew jwt encode` — Build a JWT

Build a JWT with algorithm, keys, standard claims, and custom claims.

```
USAGE:
    crew jwt encode [OPTIONS]

OPTIONS:
    -t, --template <NAME>        Use a saved template by name
    -a, --algorithm <ALGORITHM>  JWT algorithm (HS256, HS384, HS512, RS256, RS384, RS512)
    -s, --secret <SECRET>        Secret key (HMAC) or private key (RSA)
    -p, --public-key <KEY>       Public key (RSA)
    --issuer <ISSUER>            Issuer claim
    --audience <AUDIENCE>        Audience claim
    --subject <SUBJECT>          Subject claim
    --expiration <MINUTES>       Expiration in minutes (default: 60)
    -c, --claim <CLAIM>          Custom claim (repeatable: key=value, key:value, or key-value)
    --save <NAME>                Save effective options as a template
    --copy                       Copy generated token to clipboard
    -h, --help                   Prints help information
```

#### Examples

Build with defaults:

```bash
crew jwt encode
```

Build with algorithm and claims:

```bash
crew jwt encode --algorithm HS256 --claim role=admin --claim env:dev
```

Build from a saved template and override expiration:

```bash
crew jwt encode --template "my-template" --expiration 15
```

Build and save as a reusable template:

```bash
crew jwt encode --algorithm RS256 --issuer "devcrew" --save "cli-rs-template"
```

Build and copy token to clipboard:

```bash
crew jwt encode --copy
```

---

### `crew jwt list-templates` — List Saved JWT Templates

List saved JWT templates and optionally filter them by name.

```
USAGE:
    crew jwt list-templates [OPTIONS]

OPTIONS:
    -n, --name <TEXT>  Filter templates by name
    -h, --help         Prints help information
```

#### Examples

List all saved templates:

```bash
crew jwt list-templates
```

Filter templates by name:

```bash
crew jwt list-templates --name cli
```

---

### `crew jwt update-template` — Update A Saved JWT Template

Update a saved JWT template by name. You can rename the template, change token settings, and replace custom claims.

```
USAGE:
    crew jwt update-template <NAME> [OPTIONS]

OPTIONS:
    --template-name <NAME>  Rename the template
    -a, --algorithm <ALGORITHM>
                            JWT algorithm (HS256, HS384, HS512, RS256, RS384, RS512)
    -s, --secret <SECRET>   Secret key (HMAC) or private key (RSA)
    -p, --public-key <KEY>  Public key (RSA)
    --issuer <ISSUER>       Issuer claim
    --audience <AUDIENCE>   Audience claim
    --subject <SUBJECT>     Subject claim
    --expiration <MINUTES>  Expiration in minutes
    -c, --claim <CLAIM>     Replace claims with the provided set (repeatable)
    --clear-claims          Remove all custom claims
    -h, --help              Prints help information
```

#### Examples

Update expiration and subject:

```bash
crew jwt update-template my-template --expiration 15 --subject "service-token"
```

Replace claims:

```bash
crew jwt update-template my-template --claim role=admin --claim env=prod
```

Rename a template:

```bash
crew jwt update-template my-template --template-name my-template-v2
```

---

### `crew jwt delete-template` — Delete A Saved JWT Template

Delete a saved JWT template by name.

```
USAGE:
    crew jwt delete-template <NAME>

OPTIONS:
    -h, --help  Prints help information
```

#### Examples

Delete a template:

```bash
crew jwt delete-template my-template
```

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
# Id: 21 Guid: 1c516271-9404-47d4-8e64-de4233a3fb02 Notes: my-api-key
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

### `crew guid update-notes` — Update Saved GUID Notes

Update or clear notes for a saved GUID by its record ID.

```
USAGE:
    crew guid update-notes <ID> [OPTIONS]

OPTIONS:
    -n, --notes <TEXT>  New notes value
    --clear-notes       Remove notes from the saved GUID
    -h, --help          Prints help information
```

#### Examples

Update notes by ID:

```bash
crew guid update-notes 21 --notes "rotated-prod-key"
```

Clear notes by ID:

```bash
crew guid update-notes 21 --clear-notes
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
