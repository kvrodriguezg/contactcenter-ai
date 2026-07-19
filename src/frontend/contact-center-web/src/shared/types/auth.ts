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

export type CompanyStatus = 'Active' | 'Inactive';

export interface Company {
  id: string;
  name: string;
  status: CompanyStatus | string;
  createdAt: string;
}

export interface UserListItem {
  id: string;
  email: string;
  name?: string | null;
  role: string;
  isActive: boolean;
  companyId?: string | null;
  companyName?: string | null;
  authenticationProvider: string;
  /** Auth0 claim `sub` (p. ej. auth0|...). */
  externalSubject?: string | null;
  createdAt: string;
}

export interface CreateCompanyPayload {
  name: string;
}

export interface UpdateCompanyPayload {
  name: string;
  status: CompanyStatus | string;
}

export interface CreateUserPayload {
  email: string;
  name?: string | null;
  role: string;
  companyId?: string | null;
  password?: string | null;
  externalSubject?: string | null;
}

export interface UpdateUserPayload {
  role: string;
  isActive: boolean;
  companyId?: string | null;
  name?: string | null;
  /** Omitir para no modificar el ID Auth0 (p. ej. al cambiar solo el estado). */
  externalSubject?: string | null;
}
