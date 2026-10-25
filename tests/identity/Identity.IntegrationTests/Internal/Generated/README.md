# Generated Wolverine handler code

Everything under this folder is written by Wolverine and committed, so this test host loads its handlers with
`TypeLoadMode.Static` and no test run compiles handler code. Do not edit it by hand. A stale or missing folder makes the
host fail at startup.

Regenerate it after adding or changing a message handler, a message type, the test host's Wolverine setup or its
test-only handlers:

```sh
scripts/generate-test-handlers.sh
```

This regenerates every `tests/*/*.IntegrationTests` project; pass project directories to regenerate some. It needs no
database or Docker. CI runs the same script and fails when it changes a file (`.github/workflows/ci.yml`).
