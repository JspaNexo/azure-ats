import assert from 'node:assert/strict';
import { test } from 'node:test';
import { activateRelease, applyRelease, identityUrl, shouldDeploy, validateSummary } from '../scripts/lab-release.mjs';

const env = { ATS_ENABLE_CD: 'true', ATS_SOURCE_BRANCH: 'main', ATS_IMAGE_TAG: 'tc-42', BUILD_SOURCEVERSION: 'a'.repeat(40) };
const summary = { status: 'passed', imageTag: 'tc-42', sourceVersion: env.BUILD_SOURCEVERSION,
  publicKeycloakUrl: identityUrl, imageIds: { backend: `sha256:${'b'.repeat(64)}`, frontend: `sha256:${'c'.repeat(64)}` } };
const previous = { tag: 'tc-41', backend: 'previous-backend', frontend: 'previous-frontend' };
const candidate = { tag: 'tc-42', backend: 'tested-backend', frontend: 'tested-frontend', directory: 'release-42' };

test('CD requires explicit enablement and main; feature branches and missing branch cannot deploy', () => {
  assert.equal(shouldDeploy({}), false);
  assert.equal(shouldDeploy({ ...env, ATS_ENABLE_CD: 'false' }), false);
  for (const branch of ['feature/cd', 'pull/42', '', undefined]) {
    assert.equal(shouldDeploy({ ...env, ATS_SOURCE_BRANCH: branch }), false);
  }
  assert.equal(shouldDeploy(env), true);
  assert.equal(shouldDeploy({ ...env, ATS_SOURCE_BRANCH: 'refs/heads/main' }), true);
  assert.throws(() => shouldDeploy({ ...env, ATS_ENABLE_CD: 'ture' }));
});

test('publication rejects failed tests and summaries belonging to another commit or build', () => {
  validateSummary(summary, env);
  for (const change of [{ status: 'failed' }, { sourceVersion: 'd'.repeat(40) }, { imageTag: 'tc-41' },
    { imageIds: undefined }, { publicKeycloakUrl: 'http://localhost:8085' }]) {
    assert.throws(() => validateSummary({ ...summary, ...change }, env));
  }
});

test('a new version is saved only after startup and verification succeed', async () => {
  const events = [];
  await activateRelease(candidate, {
    readCurrent: async () => previous,
    apply: async item => events.push(['apply', item]),
    verify: async () => events.push(['verify']),
    save: async (item, old) => events.push(['save', item, old]),
  });
  assert.deepEqual(events, [['apply', candidate], ['verify'], ['save', candidate, previous]]);
});

test('failed health checks restore both previous application images, verify recovery and fail the build', async () => {
  const applied = [];
  let checks = 0;
  let saved = false;
  await assert.rejects(activateRelease(candidate, {
    readCurrent: async () => previous,
    apply: async item => applied.push(item),
    verify: async () => { if (++checks === 1) throw new Error('API unhealthy'); },
    save: async () => { saved = true; },
  }), /API unhealthy.*tc-41 recovered and verified/);
  assert.deepEqual(applied, [candidate, previous]);
  assert.equal(checks, 2);
  assert.equal(saved, false);
});

test('partial startup failure also triggers recovery', async () => {
  const applied = [];
  await assert.rejects(activateRelease(candidate, {
    readCurrent: async () => previous,
    apply: async item => { applied.push(item); if (item === candidate) throw new Error('container failed'); },
    verify: async () => {},
    save: async () => assert.fail('Failed deployment must not be saved'),
  }), /container failed.*recovered and verified/);
  assert.deepEqual(applied, [candidate, previous]);
});

test('first deployment failure reports that no earlier release can be restored', async () => {
  let starts = 0;
  await assert.rejects(activateRelease(candidate, {
    readCurrent: async () => null,
    apply: async () => { starts++; },
    verify: async () => { throw new Error('unhealthy'); },
    save: async () => assert.fail('Failed first deployment must not be saved'),
  }), /No previous successful release/);
  assert.equal(starts, 1);
});

test('recovery failure is reported instead of claiming that the previous release is healthy', async () => {
  await assert.rejects(activateRelease(candidate, {
    readCurrent: async () => previous,
    apply: async () => {},
    verify: async () => { throw new Error('database unavailable'); },
    save: async () => assert.fail(),
  }), /Recovery failed: database unavailable/);
});

test('deployment uses tested images with persistent volumes and no rebuild', async () => {
  const calls = [];
  await applyRelease(candidate, 'laboratory', {}, async (args, variables) => calls.push({ args, variables }));
  assert.equal(calls.length, 1);
  assert.deepEqual(calls[0].args.slice(-4), ['up', '-d', '--no-build', '--remove-orphans']);
  assert.equal(calls[0].variables.ATS_BACKEND_IMAGE, candidate.backend);
  assert.equal(calls[0].variables.ATS_FRONTEND_IMAGE, candidate.frontend);
  assert.ok(!calls[0].args.includes('down'));
  assert.ok(!calls[0].args.includes('--volumes'));
});
