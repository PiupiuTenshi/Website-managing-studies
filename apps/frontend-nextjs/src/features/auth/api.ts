import { getApiBaseUrl } from "@/features/platform-status/api";
import type { ApiResponse, AuthTokenResponse, LoginInput } from "./types";

async function parseApiResponse<T>(response: Response): Promise<ApiResponse<T>> {
  const body = (await response.json()) as ApiResponse<T>;

  if (!response.ok || !body.success) {
    throw new Error(body.error?.message ?? `Request failed with HTTP ${response.status}.`);
  }

  return body;
}

export async function login(input: LoginInput): Promise<AuthTokenResponse> {
  const response = await fetch(`${getApiBaseUrl()}/api/auth/login`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(input)
  });

  const body = await parseApiResponse<AuthTokenResponse>(response);
  if (!body.data) {
    throw new Error("Login response did not include auth data.");
  }

  return body.data;
}

export async function fetchCurrentUser(accessToken: string) {
  const response = await fetch(`${getApiBaseUrl()}/api/auth/me`, {
    headers: {
      Authorization: `Bearer ${accessToken}`
    },
    cache: "no-store"
  });

  const body = await parseApiResponse<AuthTokenResponse["user"]>(response);
  if (!body.data) {
    throw new Error("Current user response did not include user data.");
  }

  return body.data;
}

export async function logout(accessToken: string, refreshToken: string) {
  await fetch(`${getApiBaseUrl()}/api/auth/logout`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${accessToken}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ refreshToken })
  });
}
