// Optional live deployment/recovery check. Requires the images from ci-stack.mjs
// and free laboratory ports; uses a separate project and removes only its volumes.
import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { activateRelease, applyRelease, docker, labUrl, prepareRelease, verifyLab } from '../scripts/lab-release.mjs';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const projectName = `ats-lab-verification-${randomUUID()}`;
const labDir = join(root, 'scratch', projectName);
const summary = JSON.parse(await readFile(join(root, 'ci-results/summary.json'), 'utf8'));
assert.equal(summary.status, 'passed');
assert.equal(summary.publicKeycloakUrl, 'http://localhost:18085');
const release = { tag: summary.imageTag, backend: summary.imageIds.backend, frontend: summary.imageIds.frontend };
await prepareRelease(root, labDir, release);
let current = null;
let attempted;
const controls = {
  readCurrent: async () => current,
  apply: async item => {
    attempted = item;
    await applyRelease(item, labDir, process.env, docker, projectName);
  },
  verify: async () => {
    if (attempted.frontend === 'postgres:16-alpine') {
      // This image does not serve HTTP. Keep this intentional failure quick.
      await assert.rejects(fetch(`${labUrl}/health`, { signal: AbortSignal.timeout(5_000) }));
      throw new Error('Intentional failure: candidate frontend is not an HTTP server');
    }
    await verifyLab();
  },
  save: async item => { current = item; },
};
try {
  await activateRelease(release, controls);
  console.log('Live laboratory deployment verified.');
  const before = await docker(['volume', 'inspect', `${projectName}_postgres_data`, `${projectName}_backend_storage`, '--format', '{{.CreatedAt}}']);
  await assert.rejects(activateRelease({ ...release, tag: 'tc-9000000002', frontend: 'postgres:16-alpine' }, controls),
    /Intentional failure.*recovered and verified/);
  assert.equal(current, release);
  const after = await docker(['volume', 'inspect', `${projectName}_postgres_data`, `${projectName}_backend_storage`, '--format', '{{.CreatedAt}}']);
  assert.equal(after, before, 'Recovery must preserve existing volumes');
  console.log('Live recovery verified: previous application restored, volumes preserved, failed deployment rejected.');
} finally {
  assert.match(projectName, /^ats-lab-verification-[a-f0-9-]+$/);
  await docker(['compose', '--project-name', projectName, '--file', join(release.directory, 'compose.yml'),
    '--env-file', join(labDir, 'credentials.env'), 'down', '--volumes', '--remove-orphans'], {
    ...process.env, ATS_BACKEND_IMAGE: release.backend, ATS_FRONTEND_IMAGE: release.frontend,
  });
}
