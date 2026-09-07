import {
  Candidate,
  InterviewReportDto,
  Recruiter,
  JobPosition
} from '../types';
import { keycloak } from './keycloak';

const API_BASE = '/api/v1';

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
    this.name = 'ApiError';
  }
}

export async function getAuthHeaders(): Promise<Record<string, string>> {
  if (keycloak.authenticated) {
    try {
      await keycloak.updateToken(30);
    } catch {
      // Continue with current token
    }
  }
  const headers: Record<string, string> = {};
  if (keycloak.token) {
    headers['Authorization'] = `Bearer ${keycloak.token}`;
  }
  return headers;
}

async function fetchWithAuth(url: string, init: RequestInit = {}): Promise<Response> {
  const authHeaders = await getAuthHeaders();
  const headers = new Headers(init.headers || {});
  for (const [key, value] of Object.entries(authHeaders)) {
    headers.set(key, value);
  }
  return fetch(url, { ...init, headers });
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    let errorDetail = res.statusText;
    try {
      const errJson = await res.json();
      errorDetail = errJson.detail || errJson.message || errJson.title || JSON.stringify(errJson);
    } catch {
      // ignore
    }
    throw new ApiError(res.status, errorDetail);
  }

  // If 204 No Content
  if (res.status === 204) {
    return {} as T;
  }

  return res.json();
}

export const api = {
  // System Health
  async getHealth(): Promise<{ status: string }> {
    const res = await fetch('/health');
    if (!res.ok) throw new ApiError(res.status, 'Unhealthy');
    const text = await res.text();
    return { status: text.trim() };
  },

  // Candidates
  async getCandidates(recruiterId?: string): Promise<Candidate[]> {
    const url = recruiterId
      ? `${API_BASE}/candidates?recruiterId=${encodeURIComponent(recruiterId)}`
      : `${API_BASE}/candidates`;
    const res = await fetchWithAuth(url);
    return handleResponse<Candidate[]>(res);
  },

  async getCandidate(id: string): Promise<Candidate> {
    const res = await fetchWithAuth(`${API_BASE}/candidates/${id}`);
    return handleResponse<Candidate>(res);
  },

  async createCandidate(payload: {
    firstName: string;
    lastName: string;
    email: string;
    phone?: string;
    targetRole: string;
  }): Promise<Candidate> {
    const res = await fetchWithAuth(`${API_BASE}/candidates`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(payload),
    });
    return handleResponse<Candidate>(res);
  },

  async updateCandidateDecision(
    candidateId: string,
    decision: string,
    notes?: string
  ): Promise<Candidate> {
    const res = await fetchWithAuth(`${API_BASE}/candidates/${candidateId}/decision`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ decision, notes }),
    });
    return handleResponse<Candidate>(res);
  },

  async assignCandidate(
    candidateId: string,
    recruiterId: string,
    recruiterName: string,
    recruiterEmail: string
  ): Promise<Candidate> {
    const res = await fetchWithAuth(`${API_BASE}/candidates/${candidateId}/assign`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ recruiterId, recruiterName, recruiterEmail }),
    });
    return handleResponse<Candidate>(res);
  },

  async getRecruiters(): Promise<Recruiter[]> {
    const res = await fetchWithAuth(`${API_BASE}/users/recruiters`);
    return handleResponse<Recruiter[]>(res);
  },

  // Job Positions / Vacancies
  async getJobPositions(status?: string): Promise<JobPosition[]> {
    const url = status
      ? `${API_BASE}/positions?status=${encodeURIComponent(status)}`
      : `${API_BASE}/positions`;
    const res = await fetchWithAuth(url);
    return handleResponse<JobPosition[]>(res);
  },

  async createJobPosition(payload: {
    title: string;
    department: string;
    seniority?: string;
    minExperienceYears?: number;
    description?: string;
    requirements?: string;
  }): Promise<JobPosition> {
    const res = await fetchWithAuth(`${API_BASE}/positions`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(payload),
    });
    return handleResponse<JobPosition>(res);
  },

  async updateJobPositionStatus(id: string, status: string): Promise<JobPosition> {
    const res = await fetchWithAuth(`${API_BASE}/positions/${id}/status`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ status }),
    });
    return handleResponse<JobPosition>(res);
  },

  // Direct Ingestion with AI
  async ingestCandidate(formData: FormData): Promise<Candidate> {
    const res = await fetchWithAuth(`${API_BASE}/ingestion/evaluate`, {
      method: 'POST',
      body: formData,
    });
    return handleResponse<Candidate>(res);
  },

  // CV Documents
  async getCvDocument(candidateId: string): Promise<any> {
    const res = await fetchWithAuth(`${API_BASE}/documents/candidate/${candidateId}`);
    return handleResponse<any>(res);
  },

  async uploadCv(candidateId: string, file: File): Promise<any> {
    const formData = new FormData();
    formData.append('candidateId', candidateId);
    formData.append('file', file);

    const res = await fetchWithAuth(`${API_BASE}/documents/cv`, {
      method: 'POST',
      body: formData,
    });
    return handleResponse<any>(res);
  },

  // DISC Profiles
  async getDiscProfile(candidateId: string): Promise<any> {
    const res = await fetchWithAuth(`${API_BASE}/disc/candidate/${candidateId}`);
    return handleResponse<any>(res);
  },

  async submitDisc(
    candidateId: string,
    payload: {
      dominance: number;
      influence: number;
      steadiness: number;
      conscientiousness: number;
      primaryStyle?: string;
    }
  ): Promise<any> {
    const res = await fetchWithAuth(`${API_BASE}/disc/results`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        candidateId,
        dominance: payload.dominance,
        influence: payload.influence,
        steadiness: payload.steadiness,
        conscientiousness: payload.conscientiousness,
        primaryStyle: payload.primaryStyle,
      }),
    });
    return handleResponse<any>(res);
  },

  // Reports
  async generateReport(candidateId: string): Promise<InterviewReportDto> {
    const res = await fetchWithAuth(`${API_BASE}/reports/generate?candidateId=${encodeURIComponent(candidateId)}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
    });
    return handleResponse<InterviewReportDto>(res);
  },

  async getReport(candidateId: string): Promise<InterviewReportDto> {
    const res = await fetchWithAuth(`${API_BASE}/reports/candidate/${candidateId}`);
    return handleResponse<InterviewReportDto>(res);
  },

  async downloadReportPdf(candidateId: string): Promise<Blob> {
    const res = await fetchWithAuth(`${API_BASE}/reports/download/${candidateId}`);
    if (!res.ok) {
      throw new ApiError(res.status, 'Error al descargar el expediente PDF');
    }
    return res.blob();
  },

  getReportDownloadUrl(candidateId: string): string {
    return `${API_BASE}/reports/download/${candidateId}`;
  },

  // Webhooks / AI Simulation triggers
  async simulateCvAnalysis(candidateId: string, documentId: string): Promise<void> {
    const eventId = '00000000-0000-0000-0000-000000000001';
    const correlationId = '00000000-0000-0000-0000-000000000001';
    const res = await fetchWithAuth(
      `${API_BASE}/webhooks/process-cv?candidateId=${encodeURIComponent(candidateId)}&documentId=${encodeURIComponent(documentId)}&eventId=${eventId}&correlationId=${correlationId}`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      }
    );
    return handleResponse<void>(res);
  },

  async simulateDiscInterpretation(candidateId: string, discResultId: string): Promise<void> {
    const eventId = '00000000-0000-0000-0000-000000000002';
    const correlationId = '00000000-0000-0000-0000-000000000002';
    const res = await fetchWithAuth(
      `${API_BASE}/webhooks/process-disc?candidateId=${encodeURIComponent(candidateId)}&discResultId=${encodeURIComponent(discResultId)}&eventId=${eventId}&correlationId=${correlationId}`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      }
    );
    return handleResponse<void>(res);
  },
};
