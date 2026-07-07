import { getApiBaseUrl } from "@/features/platform-status/api";
import { loadAuthSession } from "@/features/auth/auth-storage";
import type { ApiResponse } from "@/features/auth/types";
import type {
  Assignment,
  AssignmentTarget,
  CreateAssignmentRequest,
  UpdateAssignmentRequest,
  AssignTargetRequest
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

export async function getAssignments(subjectId?: string, status?: string): Promise<Assignment[]> {
  const params = new URLSearchParams();
  if (subjectId) params.append("subjectId", subjectId);
  if (status) params.append("status", status);
  
  const qs = params.toString() ? `?${params.toString()}` : "";
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/assignments${qs}`);
  return response.data;
}

export async function getAssignment(id: string): Promise<Assignment> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/assignments/${id}`);
  return response.data;
}

export async function createAssignment(request: CreateAssignmentRequest): Promise<Assignment> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/assignments`, {
    method: "POST",
    body: JSON.stringify(request)
  });
  return response.data;
}

export async function updateAssignment(id: string, request: UpdateAssignmentRequest): Promise<Assignment> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/assignments/${id}`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
  return response.data;
}

export async function deleteAssignment(id: string): Promise<void> {
  await fetchWithAuth(`${getApiBaseUrl()}/api/assignments/${id}`, {
    method: "DELETE"
  });
}

export async function publishAssignment(id: string): Promise<Assignment> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/assignments/${id}/publish`, {
    method: "POST"
  });
  return response.data;
}

export async function getAssignmentTargets(id: string): Promise<AssignmentTarget[]> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/assignments/${id}/targets`);
  return response.data;
}

export async function setAssignmentTargets(id: string, request: AssignTargetRequest): Promise<void> {
  await fetchWithAuth(`${getApiBaseUrl()}/api/assignments/${id}/targets`, {
    method: "POST",
    body: JSON.stringify(request)
  });
}
