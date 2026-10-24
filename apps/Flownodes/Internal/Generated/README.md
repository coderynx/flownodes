# Generated Wolverine handler code

Everything under this folder is written by Wolverine and committed, so the host starts without compiling handlers at
run time. Do not edit it by hand.

Regenerate it after adding or changing a message handler, a message type or the messaging setup:

```sh
ConnectionStrings__registry="Host=x;Database=x" \
ConnectionStrings__identity="Host=x;Database=x" \
ConnectionStrings__engine="Host=x;Database=x" \
Flownodes__InstallationKey="$(head -c 32 /dev/zero | base64)" \
dotnet run --project apps/Flownodes -- codegen write
```

The connection strings and the key are placeholders; the command never connects to a database. CI runs the same command
and fails when it changes a file (`.github/workflows/ci.yml`), which is what catches stale generated code.
