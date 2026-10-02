// Run against an isolated local database and Keycloak realm, with Gemini:UseMockData=true.
import assert from 'node:assert/strict';
import { readFile, writeFile } from 'node:fs/promises';
import { randomUUID } from 'node:crypto';
import { createSmokePdf } from './smoke-fixture.mjs';

const apiUrl = process.env.ATS_SMOKE_API_URL ?? 'http://localhost:15027';
const identityUrl = process.env.ATS_SMOKE_KEYCLOAK_URL ?? 'http://localhost:18085';
const pdfPath = process.env.ATS_SMOKE_PDF_PATH;
let apiRequests = 0;

async function token(username, password) {
  const response = await fetch(`${identityUrl}/realms/ats-realm/protocol/openid-connect/token`, {
    method: 'POST',
    signal: AbortSignal.timeout(15_000),
    body: new URLSearchParams({ client_id: 'ats-frontend', grant_type: 'password', username, password }),
  });
  assert.equal(response.status, 200, `Keycloak login failed for ${username}`);
  return (await response.json()).access_token;
}

async function request(path, { token: accessToken, method = 'GET', body, expected = 200 } = {}) {
  const headers = {};
  if (accessToken) headers.Authorization = `Bearer ${accessToken}`;
  if (body && !(body instanceof FormData)) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(body);
  }
  const response = await fetch(`${apiUrl}${path}`, { method, headers, body, signal: AbortSignal.timeout(120_000) });
  if (response.status !== expected) {
    throw new Error(`${method} ${path}: expected ${expected}, received ${response.status}: ${await response.text()}`);
  }
  apiRequests++;
  return response;
}

const admin = await token('admin', process.env.ATS_SMOKE_ADMIN_PASSWORD ?? 'Admin123!');
const recruiter = await token('carlos.mendoza', process.env.ATS_SMOKE_RECRUITER_PASSWORD ?? 'Recruiter123!');
await request('/health');
await request('/api/v1/candidates', { expected: 401 });
const initialCandidates = await (await request('/api/v1/candidates', { token: admin })).json();
assert.ok(initialCandidates.length > 0, 'The SQL seed must load successfully');
const email = `review-${randomUUID()}@example.com`;
const candidateBody = { firstName: 'Review', lastName: 'Candidate', email, phoneNumber: '+59170000000' };
await request('/api/v1/candidates', { token: admin, method: 'POST', body: { ...candidateBody, firstName: 'x'.repeat(101) }, expected: 400 });
const candidate = await (await request('/api/v1/candidates', { token: admin, method: 'POST', body: candidateBody })).json();
assert.equal(candidate.status, 'Registered');
assert.equal(candidate.matchScore, null);
await request('/api/v1/candidates', { token: admin, method: 'POST', body: { ...candidateBody, email: email.toUpperCase() }, expected: 409 });

const positionBody = { title: 'Smoke vacancy', department: 'Engineering', seniority: 'Junior', minExperienceYears: 0 };
await request('/api/v1/positions', { token: recruiter, method: 'POST', body: positionBody, expected: 403 });
const position = await (await request('/api/v1/positions', { token: admin, method: 'POST', body: positionBody, expected: 201 })).json();
assert.equal(position.minExperienceYears, 0, 'Zero years must not be replaced by the database default');
await request(`/api/v1/positions/${position.id}/status`, { token: admin, method: 'PATCH', body: { status: 'Typo' }, expected: 400 });
await request(`/api/v1/candidates/${candidate.id}/assign`, { token: recruiter, method: 'PATCH', body: { recruiterId: 'carlos.mendoza' }, expected: 403 });
await request(`/api/v1/candidates/${candidate.id}/assign`, {
  token: admin, method: 'PATCH', body: { recruiterId: 'carlos.mendoza', recruiterName: 'Carlos Mendoza', recruiterEmail: 'carlos.mendoza@empresa.com' },
});
const assigned = await (await request('/api/v1/candidates?recruiterId=carlos.mendoza', { token: recruiter })).json();
assert.ok(assigned.some(c => c.id === candidate.id));
assert.ok(assigned.every(c => c.assignedRecruiterId === 'carlos.mendoza'));
await request(`/api/v1/candidates/${candidate.id}/decision`, { token: recruiter, method: 'PATCH', body: { decision: 'Unknown' }, expected: 400 });
await request(`/api/v1/candidates/${candidate.id}/decision`, { token: recruiter, method: 'PATCH', body: { decision: 'Approved', notes: 'Smoke verification' } });

const pdfBytes = pdfPath ? await readFile(pdfPath) : createSmokePdf();
function ingestionForm({ address = email, dominance = 13, file = pdfBytes, jobPositionId = position.id } = {}) {
  const form = new FormData();
  const fields = { firstName: 'Review', lastName: 'Candidate', email: address, jobPositionId, dominance, influence: 27, steadiness: 41, conscientiousness: 95 };
  for (const [key, value] of Object.entries(fields)) form.append(key, String(value));
  form.append('file', new Blob([file], { type: 'application/pdf' }), 'review.pdf');
  return form;
}
const invalidEmail = `invalid-${randomUUID()}@example.com`;
await request('/api/v1/ingestion/evaluate', { token: admin, method: 'POST', body: ingestionForm({ address: invalidEmail, dominance: 999 }), expected: 400 });
await request('/api/v1/ingestion/evaluate', { token: admin, method: 'POST', body: ingestionForm({ address: invalidEmail, file: new TextEncoder().encode('not a PDF') }), expected: 400 });
await request('/api/v1/ingestion/evaluate', { token: admin, method: 'POST', body: ingestionForm({ address: invalidEmail, jobPositionId: randomUUID() }), expected: 404 });
const afterInvalid = await (await request('/api/v1/candidates', { token: admin })).json();
assert.ok(afterInvalid.every(c => c.email !== invalidEmail), 'Invalid ingestion must not create a candidate');

const evaluated = await (await request('/api/v1/ingestion/evaluate', { token: admin, method: 'POST', body: ingestionForm() })).json();
assert.equal(evaluated.status, 'ReportReady');
assert.equal(evaluated.jobPositionId, position.id);
assert.deepEqual(evaluated.discScores, { dominance: 13, influence: 27, steadiness: 41, conscientiousness: 95 });
assert.ok(evaluated.report.fileUrl);
const reevaluated = await (await request('/api/v1/ingestion/evaluate', { token: admin, method: 'POST', body: ingestionForm({ dominance: 83 }) })).json();
assert.equal(reevaluated.discScores.dominance, 83);
assert.equal(reevaluated.evaluatorDecision, 'Approved');
assert.equal(reevaluated.evaluatorNotes, 'Smoke verification');
assert.notEqual(reevaluated.report.fileUrl, evaluated.report.fileUrl, 'A new CV/DISC must regenerate the report');
const cached = await (await request(`/api/v1/reports/generate?candidateId=${candidate.id}`, { token: admin, method: 'POST' })).json();
assert.equal(cached.fileUrl, reevaluated.report.fileUrl, 'Unchanged inputs must reuse the report');
const download = await request(`/api/v1/reports/download/${candidate.id}`, { token: recruiter });
const reportBytes = Buffer.from(await download.arrayBuffer());
assert.equal(reportBytes.subarray(0, 5).toString(), '%PDF-');
assert.ok(reportBytes.length > 500);
const positions = await (await request('/api/v1/positions', { token: admin })).json();
const persistedPosition = positions.find(item => item.id === position.id);
assert.ok(persistedPosition, 'The created vacancy must remain queryable');
assert.equal(persistedPosition.minExperienceYears, 0);
if (process.env.ATS_SMOKE_RESULT_PATH) {
  await writeFile(process.env.ATS_SMOKE_RESULT_PATH, JSON.stringify({ status: 'passed', apiRequests }, null, 2));
}
console.log(`Passed integration flow: ${apiRequests} API requests, real Keycloak roles, SQL seed, ingestion with explicit mock AI, reprocessing and PDF download.`);
