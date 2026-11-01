import { execFileSync, spawn } from "node:child_process";
import { randomBytes } from "node:crypto";
import { resolve } from "node:path";
import { hostUrl } from "./playwright.config";

const hostDirectory = resolve(import.meta.dirname, "../../../artifacts/e2e-host");
const databases = ["engine", "registry", "identity", "web"];
const password = randomBytes(16).toString("hex");
const startTimeout = 120_000;

function docker(...args: string[]) {
  return execFileSync("docker", args, { encoding: "utf8", stdio: "pipe" }).trim();
}

async function startPostgres() {
  const container = docker(
    "run", "-d", "--rm",
    "-e", `POSTGRES_PASSWORD=${password}`,
    "-p", "127.0.0.1::5432",
    "postgres:17-alpine",
  );
  const port = docker("port", container, "5432/tcp").split(":").at(-1)!;

  await waitFor("Postgres", async () => {
    docker("exec", "-e", `PGPASSWORD=${password}`, container, "psql", "-h", "127.0.0.1", "-U", "postgres", "-c", "SELECT 1");
    return true;
  });

  for (const database of databases) {
    docker("exec", container, "psql", "-U", "postgres", "-c", `CREATE DATABASE ${database}`);
  }

  return { container, port };
}

async function waitFor(what: string, ready: () => Promise<boolean>) {
  const deadline = Date.now() + startTimeout;

  while (Date.now() < deadline) {
    if (await ready().catch(() => false)) {
      return;
    }
    await new Promise((resolved) => setTimeout(resolved, 500));
  }

  throw new Error(`${what} did not start within ${startTimeout} ms.`);
}

export default async function globalSetup() {
  const { container, port } = await startPostgres();
  const connections = Object.fromEntries(
    databases.map((database) => [
      `ConnectionStrings__${database}`,
      `Host=127.0.0.1;Port=${port};Database=${database};Username=postgres;Password=${password}`,
    ]),
  );

  const host = spawn("./Flownodes", {
    cwd: hostDirectory,
    env: {
      ...process.env,
      ...connections,
      ASPNETCORE_ENVIRONMENT: "Production",
      ASPNETCORE_URLS: hostUrl,
      Flownodes__InstallationKey: randomBytes(32).toString("base64"),
      Flownodes__Identity__Authority: `${hostUrl}/`,
      Flownodes__Identity__AllowInsecureTransport: "true",
      Flownodes__Integrations__Directory: resolve(hostDirectory, "integrations"),
    },
  });

  let output = "";
  const remember = (chunk: Buffer) => {
    output += chunk.toString();
  };
  host.stdout.on("data", remember);
  host.stderr.on("data", remember);

  const stop = () => {
    host.kill();
    docker("rm", "-f", container);
  };

  try {
    await waitFor("The host", async () => {
      if (host.exitCode !== null) {
        throw new Error(`The host exited with ${host.exitCode}:\n${output}`);
      }
      return (await fetch(`${hostUrl}/bff/session`)).status === 401;
    });
  } catch (error) {
    stop();
    throw new Error(`${String(error)}\n${output}`, { cause: error });
  }

  const setupToken = /Setup token: (\S+)/.exec(output)?.[1];
  if (!setupToken) {
    stop();
    throw new Error(`The host logged no setup token:\n${output}`);
  }
  process.env.FLOWNODES_SETUP_TOKEN = setupToken;

  return stop;
}
