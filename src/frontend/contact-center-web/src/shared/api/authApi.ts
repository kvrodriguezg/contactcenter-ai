import type { Company, CurrentUser, LoginResponse, UserListItem } from '../types/auth';
import { apiGet, apiPost } from './client';

export async function login(email: string, password: string): Promise<LoginResponse> {
  return apiPost<LoginResponse>('/api/auth/login', { email, password }, { skipAuth: true });
}

export async function getCurrentUser(): Promise<CurrentUser> {
  return apiGet<CurrentUser>('/api/auth/me');
}

export async function getCompanies(): Promise<Company[]> {
  return apiGet<Company[]>('/api/companies');
}

export async function getUsers(): Promise<UserListItem[]> {
  return apiGet<UserListItem[]>('/api/users');
}
