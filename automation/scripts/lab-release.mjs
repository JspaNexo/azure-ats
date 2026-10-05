import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { cp, mkdir, readFile, rename, writeFile } from 'node:fs/promises';
import { randomBytes, randomUUID } from 'node:crypto';
import { join } from 'node:path';

export const labUrl = 'http://localhost:15173';
export const identityUrl = 'http://localhost:18085';

export function shouldDeploy(env) {
  assert.ok(['true', 'false'].includes(env.ATS_ENABLE_CD ?? 'false'), 'ATS_ENABLE_CD must be true or false');
  return env.ATS_ENABLE_CD === 'true' && ['main', 'refs/heads/main'].includes(env.ATS_SOURCE_BRANCH);
}

export function validateSummary(summary, env) {
  assert.equal(summary.status, 'passed', 'Integration must pass before publication');
  assert.match(env.ATS_IMAGE_TAG ?? '', /^tc-\d+$/);
  assert.match(env.BUILD_SOURCEVERSION ?? '', /^(?:[a-f0-9]{40}|[a-f0-9]{64})$/);
  assert.equal(summary.imageTag, env.ATS_IMAGE_TAG, 'Integration belongs to another build');
  assert.equal(summary.sourceVersion, env.BUILD_SOURCEVERSION, 'Integration belongs to another commit');
  assert.equal(summary.publicKeycloakUrl, identityUrl, 'Frontend must be built for the laboratory identity URL');
  for (const service of ['backend', 'frontend']) {
    assert.match(summary.imageIds?.[service] ?? '', /^sha256:[a-f0-9]{64}$/);
  }
}

// Capture output rather than echoing commands or credentials; callers print progress.
export function docker(args, env = process.env, input) {
  return new Promise((resolveCommand, reject) => {
    const child = spawn('docker', args, { env, stdio: ['pipe', 'pipe', 'pipe'] });
    let stdout = '';
    let stderr = '';
    child.stdout.on('data', data => { stdout += data; });
    child.stderr.on('data', data => { stderr += data; });
    child.stdin.on('error', () => {}); // An early exit is reported by close.
    child.on('error', reject);
    child.on('close', code => {
      if (code === 0) resolveCommand(stdout.trim());
      else reject(new Error(`docker ${args[0]} failed (${code}): ${stderr.trim()}`));
    });
    child.stdin.end(input);
  });
}

export async function readJson(path) {
  try { return JSON.parse(await readFile(path, 'utf8')); }
  catch (error) { if (error.code === 'ENOENT') return null; throw error; }
}

export async function writeJson(path, value) {
  const temporary = `${path}.${randomUUID()}.tmp`;
  await writeFile(temporary, JSON.stringify(value, null, 2));
  await rename(temporary, path);
}

export async function prepareRelease(root, labDir, release) {
  assert.match(release.tag, /^tc-\d+$/);
  // Each release keeps its own Compose file and mounts for recovery.
  release.directory = join(labDir, 'releases', release.tag);
  await mkdir(release.directory, { recursive: true });
  const credentialsPath = join(labDir, 'credentials.env');
  try {
    await writeFile(credentialsPath,
      `ATS_POSTGRES_PASSWORD=${randomBytes(24).toString('hex')}\nATS_KEYCLOAK_ADMIN_PASSWORD=${randomBytes(24).toString('hex')}\n`,
      { flag: 'wx', mode: 0o600 });
  } catch (error) { if (error.code !== 'EEXIST') throw error; }
  await cp(join(root, 'automation/compose.lab.yml'), join(release.directory, 'compose.yml'));
  await cp(join(root, 'database/init'), join(release.directory, 'database/init'), { recursive: true });
  await cp(join(root, 'keycloak/themes'), join(release.directory, 'keycloak/themes'), { recursive: true });
  const realm = JSON.parse(await readFile(join(root, 'keycloak/realm-export.json'), 'utf8'));
  const frontend = realm.clients.find(client => client.clientId === 'ats-frontend');
  assert.ok(frontend, 'Missing ats-frontend client');
  frontend.rootUrl = labUrl;
  frontend.baseUrl = `${labUrl}/`;
  frontend.redirectUris = [`${labUrl}/*`, `${labUrl}/`];
  frontend.webOrigins = [labUrl];
  frontend.attributes['post.logout.redirect.uris'] = `${labUrl}/*##${labUrl}/`;
  await writeJson(join(release.directory, 'keycloak/realm-export.json'), realm);
}

export async function applyRelease(release, labDir, env = process.env, run = docker, projectName = 'ats-lab') {
  assert.match(projectName, /^ats-lab(?:-verification-[a-f0-9-]+)?$/);
  const composeArgs = ['compose', '--project-name', projectName, '--file', join(release.directory, 'compose.yml'),
    '--env-file', join(labDir, 'credentials.env')];
  await run([...composeArgs, 'up', '-d', '--no-build', '--remove-orphans'], {
    ...env, ATS_BACKEND_IMAGE: release.backend, ATS_FRONTEND_IMAGE: release.frontend,
  });
}

export async function waitFor(url, validate = async response => { assert.ok(response.ok, `HTTP ${response.status}`); }) {
  const deadline = Date.now() + 180_000;
  let lastError;
  while (Date.now() < deadline) {
    try {
      await validate(await fetch(url, { signal: AbortSignal.timeout(5_000) }));
      return;
    } catch (error) { lastError = error; }
    await new Promise(resolveWait => setTimeout(resolveWait, 2_000));
  }
  throw new Error(`${url} did not become ready: ${lastError?.message}`);
}

// These checks read persistent application data, without creating candidates.
export async function verifyLab(env = process.env) {
  const issuer = `${identityUrl}/realms/ats-realm`;
  await waitFor(`${issuer}/.well-known/openid-configuration`, async response => {
    assert.equal(response.status, 200);
    assert.equal((await response.json()).issuer, issuer);
  });
  await waitFor(`${labUrl}/health`);
  await waitFor(labUrl, async response => {
    assert.equal(response.status, 200);
    const html = await response.text();
    assert.match(html, /<div\b[^>]*\bid="root"[^>]*>/);
    const scripts = [...html.matchAll(/src="(\/assets\/[^"\s]+\.js)"/g)];
    assert.ok(scripts.length, 'Missing frontend application bundle');
    let configured = false;
    for (const [, path] of scripts) {
      const asset = await fetch(`${labUrl}${path}`, { signal: AbortSignal.timeout(5_000) });
      assert.equal(asset.status, 200);
      configured ||= (await asset.text()).includes(identityUrl);
    }
    assert.ok(configured, 'Frontend bundle must point to the laboratory Keycloak');
  });
  const login = new URL(`${issuer}/protocol/openid-connect/auth`);
  login.search = new URLSearchParams({ client_id: 'ats-frontend', redirect_uri: `${labUrl}/`,
    response_type: 'code', scope: 'openid', code_challenge: 'a'.repeat(43), code_challenge_method: 'S256' }).toString();
  await waitFor(login.href);
  const tokenResponse = await fetch(`${issuer}/protocol/openid-connect/token`, {
    method: 'POST', signal: AbortSignal.timeout(15_000),
    body: new URLSearchParams({ client_id: 'ats-frontend', grant_type: 'password', username: 'admin',
      password: env.ATS_LAB_ADMIN_PASSWORD ?? 'Admin123!' }),
  });
  assert.equal(tokenResponse.status, 200, 'Laboratory application login failed');
  const { access_token: token } = await tokenResponse.json();
  await waitFor(`${labUrl}/api/v1/candidates`, async () => {
    const response = await fetch(`${labUrl}/api/v1/candidates`, {
      signal: AbortSignal.timeout(5_000), headers: { Authorization: `Bearer ${token}` },
    });
    assert.equal(response.status, 200, 'Authenticated API request failed');
    assert.ok(Array.isArray(await response.json()));
  });
}

export async function activateRelease(release, { readCurrent, apply, verify, save }) {
  const previous = await readCurrent();
  try {
    await apply(release);
    await verify();
    await save(release, previous);
  } catch (error) {
    let recovery = 'No previous successful release is available.';
    if (previous) {
      try {
        await apply(previous);
        await verify();
        recovery = `Previous release ${previous.tag} recovered and verified.`;
      } catch (rollbackError) {
        recovery = `Recovery failed: ${rollbackError.message}`;
      }
    }
    throw new Error(`Deployment failed: ${error.message} ${recovery}`);
  }
}
