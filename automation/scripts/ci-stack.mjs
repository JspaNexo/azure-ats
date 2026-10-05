import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { mkdir, readFile, unlink, writeFile } from 'node:fs/promises';
import { randomUUID } from 'node:crypto';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { sourceVersion } from './source-version.mjs';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const results = join(root, 'ci-results');
const statePath = join(results, 'stack.json');
const composeFile = join(root, 'automation/compose.ci.yml');

function run(command, args, env = process.env, capture = false) {
  return new Promise((resolveCommand, reject) => {
    const child = spawn(command, args, { cwd: root, env, stdio: capture ? ['ignore', 'pipe', 'pipe'] : 'inherit' });
    let stdout = '';
    let stderr = '';
    if (capture) {
      child.stdout.on('data', data => { stdout += data; });
      child.stderr.on('data', data => { stderr += data; });
    }
    child.on('error', reject);
    child.on('close', code => {
      if (code === 0) resolveCommand(stdout.trim());
      else reject(new Error(`${command} ${args.join(' ')} failed (${code}). ${stderr}`));
    });
  });
}

function compose(state, args, capture = false) {
  assert.match(state.projectName, /^ats-ci-[a-f0-9-]+$/);
  assert.match(state.imageTag, /^[a-zA-Z0-9_][a-zA-Z0-9_.-]{0,127}$/);
  return run('docker', ['compose', '--project-name', state.projectName, '--file', composeFile, ...args], {
    ...process.env,
    ATS_IMAGE_TAG: state.imageTag,
    ATS_CI_ISSUER: state.issuer ?? 'http://localhost:8080/realms/ats-realm',
  }, capture);
}

async function serviceUrl(state, service, port) {
  const binding = await compose(state, ['port', service, String(port)], true);
  assert.match(binding, /^127\.0\.0\.1:\d+$/);
  return `http://${binding}`;
}

async function waitFor(url, label) {
  console.log(`Waiting for ${label}: ${url}`);
  const deadline = Date.now() + 180_000;
  let lastError = '';
  while (Date.now() < deadline) {
    try {
      const response = await fetch(url, { signal: AbortSignal.timeout(5_000) });
      await response.arrayBuffer();
      if (response.ok) return;
      lastError = `HTTP ${response.status}`;
    } catch (error) {
      lastError = error.message;
    }
    await new Promise(resolveWait => setTimeout(resolveWait, 2_000));
  }
  throw new Error(`${label} was not ready within 180 seconds: ${lastError}`);
}

async function cleanup(state) {
  // This Compose project is created with a random name exclusively for this test.
  await compose(state, ['down', '--volumes', '--remove-orphans']);
}

if (process.argv[2] === 'cleanup') {
  let state;
  try { state = JSON.parse(await readFile(statePath, 'utf8')); }
  catch (error) { if (error.code !== 'ENOENT') throw error; }
  if (state) await cleanup(state);
} else {
  const state = {
    projectName: `ats-ci-${randomUUID()}`,
    imageTag: process.env.ATS_IMAGE_TAG ?? `local-${randomUUID()}`,
  };
  await mkdir(results, { recursive: true });
  for (const file of ['summary.json', 'compose.log', 'http.json']) {
    await unlink(join(results, file)).catch(error => { if (error.code !== 'ENOENT') throw error; });
  }
  await compose(state, ['config', '--quiet']);
  await writeFile(statePath, JSON.stringify(state, null, 2));
  try {
    const commit = await sourceVersion(root);
    await compose(state, ['build', 'backend', 'frontend']);
    await compose(state, ['up', '-d', 'postgres', 'keycloak']);
    const keycloakUrl = await serviceUrl(state, 'keycloak', 8080);
    state.issuer = `${keycloakUrl}/realms/ats-realm`;
    await writeFile(statePath, JSON.stringify(state, null, 2));
    await waitFor(`${state.issuer}/.well-known/openid-configuration`, 'Keycloak');
    await compose(state, ['up', '-d', 'backend', 'frontend']);
    const backendUrl = await serviceUrl(state, 'backend', 8080);
    const frontendUrl = await serviceUrl(state, 'frontend', 80);
    await waitFor(`${backendUrl}/health`, 'backend and database schema');
    await waitFor(`${frontendUrl}/health`, 'Nginx API proxy');
    const page = await fetch(frontendUrl, { signal: AbortSignal.timeout(10_000) });
    assert.equal(page.status, 200);
    assert.match(await page.text(), /<div\b[^>]*\bid="root"[^>]*>/);
    await compose(state, ['exec', '-T', 'frontend', 'nginx', '-t']);
    await run(process.execPath, ['automation/scripts/smoke-api.mjs'], {
      ...process.env,
      ATS_SMOKE_API_URL: frontendUrl,
      ATS_SMOKE_KEYCLOAK_URL: keycloakUrl,
      ATS_SMOKE_ADMIN_PASSWORD: 'Admin123!',
      ATS_SMOKE_RECRUITER_PASSWORD: 'Recruiter123!',
      ATS_SMOKE_PDF_PATH: '',
      ATS_SMOKE_RESULT_PATH: join(results, 'http.json'),
    });
    const smokeResult = JSON.parse(await readFile(join(results, 'http.json'), 'utf8'));
    const images = {};
    for (const service of ['backend', 'frontend']) {
      images[service] = await run('docker', ['image', 'inspect', '--format', '{{.Id}}', `ats-ci-${service}:${state.imageTag}`], process.env, true);
    }
    await writeFile(join(results, 'summary.json'), JSON.stringify({
      status: 'passed',
      sourceVersion: commit,
      imageTag: state.imageTag,
      images: [`ats-ci-backend:${state.imageTag}`, `ats-ci-frontend:${state.imageTag}`],
      imageIds: images,
      publicKeycloakUrl: process.env.ATS_PUBLIC_KEYCLOAK_URL ?? 'http://localhost:8085',
      apiRequests: smokeResult.apiRequests,
      completedAt: new Date().toISOString(),
    }, null, 2));
    console.log('Integration passed: PostgreSQL, Keycloak, API, Nginx and PDF.');
  } catch (error) {
    await writeFile(join(results, 'summary.json'), JSON.stringify({ status: 'failed', error: error.message }, null, 2));
    const logs = await compose(state, ['logs', '--no-color', '--tail', '200'], true).catch(logError => logError.message);
    await writeFile(join(results, 'compose.log'), logs);
    console.error(logs);
    throw error;
  } finally {
    await cleanup(state);
  }
}
