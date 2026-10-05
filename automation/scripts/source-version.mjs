import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';

const execute = promisify(execFile);

// Read the checked-out commit at runtime, after TeamCity has fetched the source.
// This also works for detached HEAD checkouts used by CI systems.
export async function sourceVersion(checkoutDir, env = process.env) {
  let stdout;
  try {
    ({ stdout } = await execute('git', ['rev-parse', '--verify', 'HEAD'], {
      cwd: checkoutDir, env, windowsHide: true,
    }));
  } catch {
    throw new Error('Cannot read the checked-out commit. Git must be installed and the agent checkout must contain .git.');
  }
  const commit = stdout.trim();
  assert.match(commit, /^(?:[a-f0-9]{40}|[a-f0-9]{64})$/);
  if (env.BUILD_SOURCEVERSION) {
    assert.equal(env.BUILD_SOURCEVERSION, commit, 'BUILD_SOURCEVERSION differs from the checked-out commit');
  }
  return commit;
}
