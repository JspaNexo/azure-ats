import assert from 'node:assert/strict';
import { mkdtemp, mkdir, readFile, rm, rmdir } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, isAbsolute, join, relative, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { activateRelease, applyRelease, docker, prepareRelease, readJson, shouldDeploy,
  validateSummary, verifyLab, writeJson } from './lab-release.mjs';
import { sourceVersion } from './source-version.mjs';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const resultPath = join(root, 'ci-results/deployment.json');
const env = process.env;
const redact = text => String(text).replaceAll(env.ATS_GHCR_TOKEN || '\0', '[REDACTED]');

async function publish(summary, registryEnv) {
  const owner = env.ATS_GHCR_OWNER?.toLowerCase();
  assert.match(owner ?? '', /^[a-z0-9]+(?:-[a-z0-9]+)*$/);
  assert.ok(env.ATS_GHCR_USER, 'Configure env.ATS_GHCR_USER in TeamCity');
  assert.ok(env.ATS_GHCR_TOKEN, 'Configure env.ATS_GHCR_TOKEN as Password in TeamCity');
  await docker(['login', 'ghcr.io', '--username', env.ATS_GHCR_USER, '--password-stdin'], registryEnv, `${env.ATS_GHCR_TOKEN}\n`);
  const release = { tag: summary.imageTag, sourceVersion: summary.sourceVersion, completedAt: new Date().toISOString() };
  for (const service of ['backend', 'frontend']) {
    const image = `ghcr.io/${owner}/ats-${service}:${summary.imageTag}`;
    const imageId = summary.imageIds[service];
    // Tag by the immutable image ID recorded by CI, instead of a movable local tag.
    await docker(['image', 'inspect', imageId], registryEnv);
    await docker(['tag', imageId, image], registryEnv);
    console.log(`Publishing tested ${service}: ${image}`);
    const output = await docker(['push', image], registryEnv);
    const digest = output.match(/digest:\s*(sha256:[a-f0-9]{64})/)?.[1];
    assert.ok(digest, `Registry did not report the digest for ${service}`);
    release[service] = `${image.split(':')[0]}@${digest}`;
    await docker(['pull', release[service]], registryEnv);
    const pulledId = await docker(['image', 'inspect', '--format', '{{.Id}}', release[service]], registryEnv);
    assert.equal(pulledId, imageId, `Registry ${service} image differs from the tested image`);
  }
  return release;
}

async function main() {
  await mkdir(join(root, 'ci-results'), { recursive: true });
  if (!shouldDeploy(env)) {
    const reason = env.ATS_ENABLE_CD !== 'true' ? 'CD is disabled. Configure GHCR and set ATS_ENABLE_CD=true.' : 'Only main is deployed.';
    console.log(reason);
    await writeJson(resultPath, { status: 'skipped', reason });
    return;
  }
  const summary = JSON.parse(await readFile(join(root, 'ci-results/summary.json'), 'utf8'));
  validateSummary(summary, { ...env, BUILD_SOURCEVERSION: await sourceVersion(root) });
  assert.ok(isAbsolute(env.ATS_LAB_DIR ?? ''), 'ATS_LAB_DIR must be an absolute persistent directory');
  const labDir = resolve(env.ATS_LAB_DIR);
  const fromCheckout = relative(root, labDir);
  assert.ok(isAbsolute(fromCheckout) || fromCheckout === '..' || fromCheckout.startsWith(`..${sep}`),
    'ATS_LAB_DIR must be outside the disposable checkout');
  assert.notEqual(labDir, dirname(labDir), 'ATS_LAB_DIR must not be a drive root');
  await mkdir(labDir, { recursive: true });
  const lockDir = join(labDir, '.deployment-lock');
  try { await mkdir(lockDir); }
  catch (error) {
    if (error.code === 'EEXIST') throw new Error(`Another deployment holds ${lockDir}. If a previous build was terminated, confirm it is stopped before removing this empty lock directory.`);
    throw error;
  }
  let authDir;
  try {
    authDir = await mkdtemp(join(tmpdir(), 'ats-ghcr-'));
    const registryEnv = { ...env, DOCKER_CONFIG: authDir };
    const release = await publish(summary, registryEnv);
    await prepareRelease(root, labDir, release);
    console.log(`Deploying ${release.tag} to http://localhost:15173`);
    await activateRelease(release, {
      readCurrent: () => readJson(join(labDir, 'current.json')),
      apply: item => applyRelease(item, labDir, registryEnv),
      verify: () => verifyLab(env),
      save: async (item, previous) => {
        if (previous) await writeJson(join(labDir, 'previous.json'), previous);
        await writeJson(join(labDir, 'current.json'), item);
      },
    });
    await writeJson(resultPath, { status: 'deployed', ...release });
    console.log('Laboratory deployment verified: database, API, web and authentication.');
  } finally {
    try {
      // mkdtemp creates a unique directory owned by this invocation only.
      if (authDir) {
        assert.equal(dirname(resolve(authDir)), resolve(tmpdir()));
        assert.ok(relative(tmpdir(), authDir).startsWith('ats-ghcr-'));
        await rm(authDir, { recursive: true, force: true });
      }
    } finally { await rmdir(lockDir); }
  }
}

try { await main(); }
catch (error) {
  const message = redact(error.message);
  await writeJson(resultPath, { status: 'failed', error: message });
  console.error(message);
  process.exitCode = 1;
}
