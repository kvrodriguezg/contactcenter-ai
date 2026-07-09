export type DocumentStatus =
  | 'Uploaded'
  | 'PendingProcessing'
  | 'Processing'
  | 'Processed'
  | 'Failed';

export interface DocumentDto {
  id: string;
  originalFileName: string;
  sizeBytes: number;
  status: DocumentStatus;
  companyId: string;
  companyName: string;
  uploadedByUserId: string;
  createdAt: string;
  processedAt?: string | null;
  errorMessage?: string | null;
}

export interface UploadDocumentRequest {
  file: File;
  companyId?: string;
}
