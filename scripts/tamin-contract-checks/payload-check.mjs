// Payload check: PDF sample payloads (tests/tamin-contract-fixtures) are validated against the dereferenced
// request/response schema of the operation that the hand-derived route manifest names for each service.
//   node payload-check.mjs [--root <repo>]
// Samples that legitimately conflict with the spec are declared in expected-failures.json. An expected failure must
// fail with exactly the listed errors; a listed item that starts passing (fully or partly) fails this check.
import { createRequire } from 'node:module';
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { ENVIRONMENTS, PDF_PAGES, toolRoot, parseArgs, loadManifest, loadSpecDocuments, readJson, routeKey, report } from './common.mjs';

const require = createRequire(import.meta.url);
const Ajv2020 = require('ajv/dist/2020').default; // JSON Schema 2020-12, the OpenAPI 3.1 dialect.

const { root } = parseArgs(process.argv.slice(2));
const fixtureRoot = path.join(root, 'tests', 'tamin-contract-fixtures');
const manifest = loadManifest(root);
const index = readJson(path.join(fixtureRoot, 'index.json'));
const expectedFile = readJson(path.join(fixtureRoot, 'expected-failures.json'));
const problems = [];

// ---------------------------------------------------------------- dereferenced operations
const redoclyCli = path.join(toolRoot, 'node_modules', '@redocly', 'cli', 'bin', 'cli.js');
const scratch = fs.mkdtempSync(path.join(os.tmpdir(), 'tamin-payload-'));
const operations = new Map(); // routeKey -> dereferenced operation
try {
  for (const document of loadSpecDocuments(root)) {
    const input = path.join(root, 'openapi', 'tamin', document.file);
    const output = path.join(scratch, document.file.replace(/\.yaml$/, '.json'));
    const run = spawnSync(process.execPath, [redoclyCli, 'bundle', input, '--dereferenced', '--output', output], { encoding: 'utf8' });
    if (run.status !== 0) throw new Error(`Dereferenced bundle failed for ${document.file}: ${run.stdout}${run.stderr}`);
    const bundle = readJson(output);
    for (const [route, item] of Object.entries(bundle.paths ?? {})) {
      for (const [method, operation] of Object.entries(item)) {
        if (typeof operation === 'object' && operation && operation.responses) operations.set(routeKey(document.env, document.host, method, route), operation);
      }
    }
  }
} finally {
  fs.rmSync(scratch, { recursive: true, force: true });
}

// ---------------------------------------------------------------- validator
const ajv = new Ajv2020({ allErrors: true, strict: false });
ajv.addFormat('int64', { type: 'number', validate: Number.isInteger });
ajv.addFormat('uri', { type: 'string', validate: (value) => { try { return Boolean(new URL(value)); } catch { return false; } } });
const matches = (schema, value) => ajv.compile(schema)(value);
const escapePointer = (segment) => String(segment).replace(/~/g, '~0').replace(/\//g, '~1');

/** Schemas allow additional properties, so a renamed or invented field would otherwise pass; report fields the schema does not declare. */
function unknownProperties(schema, value, pointer, found) {
  if (!schema || typeof schema !== 'object') return;
  const alternatives = schema.anyOf ?? schema.oneOf;
  if (alternatives) {
    const branch = alternatives.find((candidate) => matches(candidate, value));
    if (branch) unknownProperties(branch, value, pointer, found);
    return;
  }
  if (Array.isArray(value)) {
    if (schema.items) value.forEach((item, i) => unknownProperties(schema.items, item, `${pointer}/${i}`, found));
  } else if (value && typeof value === 'object' && schema.properties) {
    for (const [name, child] of Object.entries(value)) {
      if (!(name in schema.properties)) found.push({ path: `${pointer}/${escapePointer(name)}`, keyword: 'unknownProperty' });
      else unknownProperties(schema.properties[name], child, `${pointer}/${escapePointer(name)}`, found);
    }
  }
}

function validate(schema, value) {
  const validator = ajv.compile(schema);
  const errors = [];
  if (!validator(value)) {
    for (const e of validator.errors) {
      const missing = e.keyword === 'required' ? `/${escapePointer(e.params.missingProperty)}` : '';
      errors.push({ path: `${e.instancePath}${missing}`, keyword: e.keyword });
    }
  }
  unknownProperties(schema, value, '', errors);
  const seen = new Set();
  return errors.filter((e) => { const k = `${e.path}|${e.keyword}`; return seen.has(k) ? false : seen.add(k); });
}

// ---------------------------------------------------------------- manifest lookup
const routeFor = (service, env) => manifest.routes.find((r) => r.service === service && r.env === env);

// ---------------------------------------------------------------- expected failures
const expectedFor = new Map(); // `${fixture} ${env}` -> entry
const fixtureIds = new Set((index.fixtures ?? []).map((f) => f.id));
for (const entry of expectedFile.expectedFailures ?? []) {
  const label = `expected-failures ${entry.fixture}`;
  if (!fixtureIds.has(entry.fixture)) { problems.push(`${label}: no such fixture.`); continue; }
  if (typeof entry.reason !== 'string' || entry.reason.trim() === '') problems.push(`${label}: reason is required.`);
  if (!Number.isInteger(entry.page) || entry.page < 1 || entry.page > PDF_PAGES) problems.push(`${label}: page must be 1-${PDF_PAGES}.`);
  if (!Array.isArray(entry.errors) || entry.errors.length === 0) problems.push(`${label}: errors must list at least one {path, keyword}.`);
  const fixture = index.fixtures.find((f) => f.id === entry.fixture);
  for (const env of entry.envs ?? fixture.envs ?? ENVIRONMENTS) {
    const key = `${entry.fixture} ${env}`;
    if (expectedFor.has(key)) problems.push(`${label}: ${env} is listed more than once.`);
    expectedFor.set(key, entry);
  }
}

// ---------------------------------------------------------------- run fixtures
const usedFiles = new Set();
const consumedExpectations = new Set();
const tally = { pass: 0, expected: 0 };
const lines = [];
for (const fixture of index.fixtures ?? []) {
  const label = `fixture ${fixture.id}`;
  for (const field of ['id', 'service', 'kind', 'file', 'page']) if (fixture[field] === undefined) problems.push(`${label}: '${field}' is required.`);
  if (!['request', 'response'].includes(fixture.kind)) { problems.push(`${label}: kind must be request or response.`); continue; }
  if (!Number.isInteger(fixture.page) || fixture.page < 1 || fixture.page > PDF_PAGES) problems.push(`${label}: page must be 1-${PDF_PAGES}.`);
  const filePath = path.join(fixtureRoot, fixture.file);
  usedFiles.add(path.resolve(filePath));
  if (!fs.existsSync(filePath)) { problems.push(`${label}: file ${fixture.file} does not exist.`); continue; }
  const text = fs.readFileSync(filePath, 'utf8');
  let sample; let syntaxError = null;
  try { sample = JSON.parse(text); } catch (error) { syntaxError = error.message; }

  for (const env of fixture.envs ?? ENVIRONMENTS) {
    const tag = `${fixture.id} [${env}]`;
    const route = routeFor(fixture.service, env);
    if (!route) { problems.push(`${tag}: service '${fixture.service}' has no ${env} route in the manifest.`); continue; }
    const operation = operations.get(routeKey(env, route.host, route.method, route.path));
    if (!operation) { problems.push(`${tag}: manifest route ${route.method} ${route.host}${route.path} is not in the spec.`); continue; }

    let errors;
    let note = '';
    if (syntaxError) {
      errors = [{ path: '', keyword: 'json-syntax' }];
    } else {
      let schema;
      const contentType = fixture.contentType ?? 'application/json';
      if (fixture.kind === 'request') schema = operation.requestBody?.content?.[contentType]?.schema;
      else schema = (operation.responses?.[fixture.status ?? '200'] ?? operation.responses?.default)?.content?.[contentType]?.schema;
      if (fixture.schemaPointer) {
        for (const part of fixture.schemaPointer.split('/').slice(1)) schema = schema?.[part];
      }
      if (!schema) { problems.push(`${tag}: the spec documents no ${fixture.kind} schema (${contentType}${fixture.status ? ' ' + fixture.status : ''}) for ${route.method} ${route.path}.`); continue; }
      let candidate = sample;
      if (env === 'pilot' && fixture.pilotClientId === 'added') {
        if (candidate.clientId !== undefined) { problems.push(`${tag}: pilotClientId is 'added' but the sample already has clientId.`); continue; }
        candidate = { ...candidate, clientId: candidate.docNationalCode }; // p15: pilot requires clientId = doctor national code; the PDF samples omit it.
        note = ' (pilot clientId added)';
      }
      errors = validate(schema, candidate);
    }

    const actual = new Set(errors.map((e) => `${e.path}|${e.keyword}`));
    const expectedEntry = expectedFor.get(`${fixture.id} ${env}`);
    if (!expectedEntry) {
      if (actual.size === 0) { tally.pass++; lines.push(`  pass      ${tag} p${fixture.page}${note}`); }
      else problems.push(`${tag}: ${actual.size} unexpected error(s) at PDF p${fixture.page}${note}: ${[...actual].map((a) => a.replace('|', ' ')).join('; ')}`);
      continue;
    }
    consumedExpectations.add(`${fixture.id} ${env}`);
    const expected = new Set(expectedEntry.errors.map((e) => `${e.path}|${e.keyword}`));
    const nowPassing = [...expected].filter((e) => !actual.has(e));
    const newErrors = [...actual].filter((e) => !expected.has(e));
    if (nowPassing.length) problems.push(`${tag}: expected failure no longer fails for: ${nowPassing.map((a) => a.replace('|', ' ')).join('; ')} - remove it from expected-failures.json.`);
    if (newErrors.length) problems.push(`${tag}: errors not covered by expected-failures.json: ${newErrors.map((a) => a.replace('|', ' ')).join('; ')}`);
    if (!nowPassing.length && !newErrors.length) { tally.expected++; lines.push(`  expected  ${tag} p${fixture.page}${note}: ${expectedEntry.reason}`); }
  }
}
for (const key of expectedFor.keys()) {
  if (!consumedExpectations.has(key) && fixtureIds.has(key.split(' ')[0])) problems.push(`expected-failures ${key}: environment is not exercised by the fixture.`);
}

// Every file in the fixture folder must be indexed, so nothing silently escapes validation.
const walk = (dir) => fs.readdirSync(dir, { withFileTypes: true }).flatMap((d) => (d.isDirectory() ? walk(path.join(dir, d.name)) : [path.join(dir, d.name)]));
for (const file of walk(fixtureRoot)) {
  const name = path.basename(file);
  if (name === 'index.json' || name === 'expected-failures.json') continue;
  if (!usedFiles.has(path.resolve(file))) problems.push(`fixture file ${path.relative(fixtureRoot, file)} is not referenced by index.json.`);
}

for (const line of lines) console.log(line);
if (!report('Payload check', problems)) process.exit(1);
console.log(`Payload check passed: ${tally.pass} samples valid, ${tally.expected} listed expected failures still failing as documented (${index.fixtures.length} fixtures).`);
