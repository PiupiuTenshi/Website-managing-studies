import { getApiBaseUrl } from "@/features/platform-status/api";
import { loadAuthSession } from "@/features/auth/auth-storage";
import type { ApiResponse } from "@/features/auth/types";
import type {
  GradeLevel,
  Subject,
  ClassRoom,
  ClassEnrollment,
  ParentStudentLink,
  CreateGradeLevelRequest,
  CreateSubjectRequest,
  CreateClassRoomRequest,
  EnrollStudentRequest,
  LinkParentRequest
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

// --- Grade Levels ---
export async function getGradeLevels(): Promise<GradeLevel[]> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/grade-levels`);
  return response.data;
}

export async function createGradeLevel(request: CreateGradeLevelRequest): Promise<GradeLevel> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/grade-levels`, {
    method: "POST",
    body: JSON.stringify(request)
  });
  return response.data;
}

// --- Subjects ---
export async function getSubjects(): Promise<Subject[]> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/subjects`);
  return response.data;
}

export async function createSubject(request: CreateSubjectRequest): Promise<Subject> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/subjects`, {
    method: "POST",
    body: JSON.stringify(request)
  });
  return response.data;
}

// --- Class Rooms ---
export async function getClassRooms(gradeLevelId?: string, managerId?: string): Promise<ClassRoom[]> {
  const params = new URLSearchParams();
  if (gradeLevelId) params.append("gradeLevelId", gradeLevelId);
  if (managerId) params.append("managerId", managerId);
  
  const qs = params.toString() ? `?${params.toString()}` : "";
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/classes${qs}`);
  return response.data;
}

export async function getClassRoom(id: string): Promise<ClassRoom> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/classes/${id}`);
  return response.data;
}

export async function createClassRoom(request: CreateClassRoomRequest): Promise<ClassRoom> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/classes`, {
    method: "POST",
    body: JSON.stringify(request)
  });
  return response.data;
}

export async function getEnrollments(classRoomId: string): Promise<ClassEnrollment[]> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/classes/${classRoomId}/students`);
  return response.data;
}

export async function enrollStudent(classRoomId: string, request: EnrollStudentRequest): Promise<ClassEnrollment> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/classes/${classRoomId}/students`, {
    method: "POST",
    body: JSON.stringify(request)
  });
  return response.data;
}

export async function removeEnrollment(classRoomId: string, studentId: string): Promise<void> {
  await fetchWithAuth(`${getApiBaseUrl()}/api/classes/${classRoomId}/students/${studentId}`, {
    method: "DELETE"
  });
}

// --- Parents & Links ---
export async function getLinkedStudents(parentId: string): Promise<ParentStudentLink[]> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/parents/${parentId}/students`);
  return response.data;
}

export async function linkParent(request: LinkParentRequest): Promise<ParentStudentLink> {
  const response = await fetchWithAuth(`${getApiBaseUrl()}/api/parent-student-links`, {
    method: "POST",
    body: JSON.stringify(request)
  });
  return response.data;
}

export async function unlinkParent(linkId: string): Promise<void> {
  await fetchWithAuth(`${getApiBaseUrl()}/api/parent-student-links/${linkId}`, {
    method: "DELETE"
  });
}
