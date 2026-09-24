export interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  roles: string[];
}

export interface AuthResponse {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  token: string;
  refreshToken: string;
  refreshTokenExpiration: string;
  roles: string[];
}
