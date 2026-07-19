import type {
  Company,
  CreateCompanyPayload,
  CreateUserPayload,
  CurrentUser,
  LoginResponse,
  UpdateCompanyPayload,
  UpdateUserPayload,
  UserListItem,
} from '../types/auth';
import { apiGet, apiPost, apiPut } from './client';

export async function login(email: string, password: string): Promise<LoginResponse> {
  return apiPost<LoginResponse>('/api/auth/login', { email, password }, { skipAuth: true });
}

export async function getCurrentUser(): Promise<CurrentUser> {
  return apiGet<CurrentUser>('/api/auth/me');
}

export async function getCompanies(): Promise<Company[]> {
  return apiGet<Company[]>('/api/companies');
}

export async function getCompanyById(id: string): Promise<Company> {
  return apiGet<Company>(`/api/companies/${id}`);
}

export async function createCompany(payload: CreateCompanyPayload): Promise<Company> {
  return apiPost<Company>('/api/companies', payload);
}

export async function updateCompany(id: string, payload: UpdateCompanyPayload): Promise<Company> {
  return apiPut<Company>(`/api/companies/${id}`, payload);
}

export async function activateCompany(id: string): Promise<Company> {
  return apiPost<Company>(`/api/companies/${id}/activate`, {});
}

export async function deactivateCompany(id: string): Promise<Company> {
  return apiPost<Company>(`/api/companies/${id}/deactivate`, {});
}

export async function getUsers(): Promise<UserListItem[]> {
  return apiGet<UserListItem[]>('/api/users');
}

export async function getUserById(id: string): Promise<UserListItem> {
  return apiGet<UserListItem>(`/api/users/${id}`);
}

export async function createUser(payload: CreateUserPayload): Promise<UserListItem> {
  return apiPost<UserListItem>('/api/users', payload);
}

export async function updateUser(id: string, payload: UpdateUserPayload): Promise<UserListItem> {
  return apiPut<UserListItem>(`/api/users/${id}`, payload);
}
