export type RoleName = "Admin" | "Manager" | "Student" | "Parent";

export type AuthUser = {
  id: string;
  email: string;
  fullName: string;
  status: string;
  activeRole: RoleName;
  roles: RoleName[];
};

export type AuthTokenResponse = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  user: AuthUser;
};

export type LoginInput = {
  login: string;
  password: string;
  role: RoleName;
};

export type ApiResponse<T> = {
  success: boolean;
  data: T | null;
  error: {
    code: string;
    message: string;
    userAction: string;
    details: string[];
  } | null;
  traceId: string;
  message: string | null;
};
