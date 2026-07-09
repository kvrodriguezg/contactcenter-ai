import type { DocumentDto } from '../types/documents';
import { apiGet, apiPostFormData } from './client';

export async function getDocuments(): Promise<DocumentDto[]> {
  return apiGet<DocumentDto[]>('/api/documents');
}

export async function getDocumentById(id: string): Promise<DocumentDto> {
  return apiGet<DocumentDto>(`/api/documents/${id}`);
}

export async function uploadDocument(file: File, companyId?: string): Promise<DocumentDto> {
  const formData = new FormData();
  formData.append('file', file);

  if (companyId) {
    formData.append('companyId', companyId);
  }

  return apiPostFormData<DocumentDto>('/api/documents', formData);
}
