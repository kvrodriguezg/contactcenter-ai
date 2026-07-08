export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  userId: string;
  email: string;
  role: string;
  companyId?: string | null;
  companyName?: string | null;
}

export interface CurrentUser {
  userId: string;
  email: string;
  role: string;
  companyId?: string | null;
  companyName?: string | null;
  isActive: boolean;
}

export interface Company {
  id: string;
  name: string;
  status: string;
  createdAt: string;
}

export interface UserListItem {
  id: string;
  email: string;
  role: string;
  isActive: boolean;
  companyId?: string | null;
  companyName?: string | null;
  createdAt: string;
}
