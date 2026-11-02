// Shared helpers for the Tamin contract conformance checks.
// Dependencies (js-yaml, ajv) are declared as devDependencies in package.json; install them with npm ci.
import { createRequire } from 'node:module';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
export const toolRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
export const yaml = require('js-yaml');
export const HTTP_METHODS = ['get', 'put', 'post', 'delete', 'options', 'head', 'patch', 'trace'];
export const ENVIRONMENTS = ['prod', 'pilot'];
export const PDF_PAGES = 66;

/** Parses `--root <dir>`; the default is the repository that contains this script. */
export function parseArgs(argv) {
  const args = { root: toolRoot };
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--root') args.root = path.resolve(argv[++i]);
    else throw new Error(`Unknown argument '${argv[i]}'.`);
  }
  return args;
}

export function readJson(file) {
  return JSON.parse(fs.readFileSync(file, 'utf8'));
}

export function loadManifest(root) {
  const manifest = readJson(path.join(root, 'openapi', 'tamin-route-manifest.json'));
  if (!Array.isArray(manifest.routes)) throw new Error('Manifest has no routes array.');
  return manifest;
}

/** Loads the six host-scoped documents as written (no reference resolution). */
export function loadSpecDocuments(root) {
  const dir = path.join(root, 'openapi', 'tamin');
  return fs.readdirSync(dir).filter((f) => f.endsWith('.yaml')).sort().map((file) => {
    const doc = yaml.load(fs.readFileSync(path.join(dir, file), 'utf8'));
    const [client, env] = file.replace(/\.yaml$/, '').split('.');
    if (!Array.isArray(doc.servers) || doc.servers.length !== 1) throw new Error(`${file}: expected exactly one server.`);
    return { file, client, env, host: new URL(doc.servers[0].url).host, doc };
  });
}

export const routeKey = (env, host, method, route) => `${env} ${host} ${method.toUpperCase()} ${route}`;

export function report(title, problems) {
  if (problems.length === 0) return true;
  console.error(`${title} failed with ${problems.length} problem(s):`);
  for (const p of problems) console.error(`  - ${p}`);
  return false;
}
