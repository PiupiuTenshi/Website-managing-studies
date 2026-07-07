import { getApiBaseUrl } from "@/features/platform-status/api";
import { loadAuthSession } from "@/features/auth/auth-storage";
import type { ApiResponse } from "@/features/auth/types";
import type {
  StudentAssignment,
  Submission,
  DraftSubmissionRequest,
  SubmitAssignmentRequest
} from "./types";

async function fetchWithAuth(url: string, options: RequestInit = {}) {
  const session = loadAuthSession();
  const token = session?.accessToken;
  if (!token) {
    throw new Error("No auth token available.");
  }

  const headers = new Headers(options.headers);
  headers.set("Authorization", `Bearer ${token}`);
  if (!headers.has("Content-Type") && options.method !== "GET" && options.method !== "DELETE") {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(url, { ...options, headers });
  
  if (response.status === 204) {
    return { success: true, data: null };
  }

  const body = (await response.json()) as ApiResponse<any>;
  if (!response.ok || !body.success) {
    throw new Error(body.error?.message ?? `Request failed with HTTP ${response.status}.`);
  }

  return body;
}

export async function getStudentAssignments(): Promise<StudentAssignment[]> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/student/assignments`);
  return response.data;
}

export async function getStudentSubmission(assignmentId: string): Promise<Submission | null> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/student/assignments/${assignmentId}/submission`);
  return response.data;
}

export async function draftSubmission(assignmentId: string, request: DraftSubmissionRequest): Promise<Submission> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/student/assignments/${assignmentId}/submission/draft`, {
    method: "POST",
    body: JSON.stringify(request)
  });
  return response.data;
}

export async function submitAssignment(assignmentId: string, request: SubmitAssignmentRequest): Promise<Submission> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/student/assignments/${assignmentId}/submission/submit`, {
    method: "POST",
    body: JSON.stringify(request)
  });
  return response.data;
}
