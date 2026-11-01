// Route check: the PDF-derived manifest and the OpenAPI documents must describe exactly the same routes.
//   node route-check.mjs [--root <repo>]
import { HTTP_METHODS, ENVIRONMENTS, PDF_PAGES, parseArgs, loadManifest, loadSpecDocuments, routeKey, report } from './common.mjs';

const { root } = parseArgs(process.argv.slice(2));
const manifest = loadManifest(root);
const documents = loadSpecDocuments(root);
const problems = [];

// 1. Manifest sanity (it is hand-maintained, so protect it from typos).
const manifestByKey = new Map();
const hostsByEnv = { prod: new Set(), pilot: new Set() };
for (const document of documents) hostsByEnv[document.env]?.add(document.host);
const servicesByEnv = new Map();
for (const r of manifest.routes) {
  const label = `manifest ${r.service} ${r.env} ${r.method} ${r.path}`;
  if (!ENVIRONMENTS.includes(r.env)) { problems.push(`${label}: env must be prod or pilot.`); continue; }
  if (!HTTP_METHODS.includes(String(r.method).toLowerCase()) || r.method !== r.method.toUpperCase()) problems.push(`${label}: method must be an upper-case HTTP verb.`);
  if (typeof r.host !== 'string' || typeof r.path !== 'string' || !r.path.startsWith('/') || /[?#\s]/.test(r.path)) problems.push(`${label}: host/path malformed (path must be literal, start with '/', and carry no query string).`);
  if (!Number.isInteger(r.page) || r.page < 1 || r.page > PDF_PAGES) problems.push(`${label}: page must be an integer from 1 to ${PDF_PAGES}.`);
  if (typeof r.service !== 'string' || r.service === '') problems.push(`${label}: service key missing.`);
  const key = routeKey(r.env, r.host, r.method, r.path);
  if (manifestByKey.has(key)) problems.push(`${label}: duplicate manifest route.`);
  manifestByKey.set(key, r);
  const sk = `${r.service} ${r.env}`;
  servicesByEnv.set(sk, (servicesByEnv.get(sk) ?? 0) + 1);
}
for (const r of manifest.routes) {
  for (const env of ENVIRONMENTS) if (!servicesByEnv.has(`${r.service} ${env}`)) problems.push(`manifest service '${r.service}' has no ${env} route.`);
}

// 2. Spec routes, keyed identically.
const specByKey = new Map();
const resolveParameter = (document, parameter) => {
  if (!parameter?.$ref) return parameter;
  const match = /^#\/components\/parameters\/(.+)$/.exec(parameter.$ref);
  return match ? document.doc.components?.parameters?.[match[1]] : undefined;
};
for (const document of documents) {
  if (!ENVIRONMENTS.includes(document.env)) { problems.push(`${document.file}: environment suffix must be prod or pilot.`); continue; }
  for (const [route, item] of Object.entries(document.doc.paths ?? {})) {
    if (item.servers) problems.push(`${document.file} ${route}: path-level servers are not allowed in host-scoped documents.`);
    for (const method of HTTP_METHODS) {
      const operation = item[method];
      if (!operation) continue;
      if (operation.servers) problems.push(`${document.file} ${method.toUpperCase()} ${route}: operation-level servers are not allowed in host-scoped documents.`);
      const key = routeKey(document.env, document.host, method, route);
      if (specByKey.has(key)) problems.push(`${document.file}: ${key} is also defined in ${specByKey.get(key).document.file}.`);
      specByKey.set(key, { document, operation, item });
    }
  }
}

// 3. Compare.
for (const [key, r] of manifestByKey) {
  const found = specByKey.get(key);
  if (!found) {
    problems.push(`MISSING from spec: ${key} (PDF p${r.page}, service ${r.service}).`);
    continue;
  }
  if (r.query) {
    const parameters = [...(found.item.parameters ?? []), ...(found.operation.parameters ?? [])].map((p) => resolveParameter(found.document, p));
    for (const name of r.query) {
      if (!parameters.some((p) => p?.in === 'query' && p.name === name)) problems.push(`QUERY parameter '${name}' printed on PDF p${r.page} is not declared by ${key} in ${found.document.file}.`);
    }
  }
}
for (const [key, found] of specByKey) {
  if (!manifestByKey.has(key)) problems.push(`NOT IN MANIFEST: ${key} (${found.document.file}, operationId ${found.operation.operationId}).`);
}

if (!report('Route check', problems)) process.exit(1);
const perEnv = ENVIRONMENTS.map((env) => `${env} ${[...manifestByKey.values()].filter((r) => r.env === env).length}`).join(', ');
console.log(`Route check passed: ${manifestByKey.size} manifest routes match the spec exactly (${perEnv}).`);
