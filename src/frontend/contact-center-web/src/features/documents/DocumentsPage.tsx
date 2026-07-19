import UploadFileIcon from '@mui/icons-material/UploadFile';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useCallback, useEffect, useRef, useState } from 'react';
import { getCompanies } from '../../shared/api/authApi';
import { getDocuments, uploadDocument } from '../../shared/api/documentsApi';
import type { Company } from '../../shared/types/auth';
import type { DocumentDto, DocumentStatus } from '../../shared/types/documents';
import { useAuth } from '../auth/useAuth';

const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

const statusLabels: Record<DocumentStatus, string> = {
  Uploaded: 'Subido',
  PendingProcessing: 'Pendiente',
  Processing: 'Procesando',
  Processed: 'Procesado',
  Failed: 'Fallido',
};

const statusColors: Record<DocumentStatus, 'default' | 'info' | 'warning' | 'success' | 'error'> = {
  Uploaded: 'default',
  PendingProcessing: 'warning',
  Processing: 'info',
  Processed: 'success',
  Failed: 'error',
};

function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function DocumentsPage() {
  const { user } = useAuth();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [documents, setDocuments] = useState<DocumentDto[]>([]);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [selectedCompanyId, setSelectedCompanyId] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState('');
  const [uploadError, setUploadError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const isSuperAdmin = user?.role === 'SuperAdmin';

  const loadDocuments = useCallback(async () => {
    setError('');
    try {
      const data = await getDocuments();
      setDocuments(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible cargar los documentos';
      setError(message);
    }
  }, []);

  useEffect(() => {
    const load = async () => {
      try {
        await loadDocuments();

        if (isSuperAdmin) {
          const companyData = await getCompanies();
          setCompanies(companyData);
        }
      } catch (err) {
        const message = err instanceof Error ? err.message : 'No fue posible cargar los documentos';
        setError(message);
      } finally {
        setIsLoading(false);
      }
    };

    void load();
  }, [isSuperAdmin, loadDocuments]);

  const handleUploadClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = '';

    if (!file) {
      return;
    }

    setUploadError('');
    setSuccessMessage('');

    if (!file.name.toLowerCase().endsWith('.pdf')) {
      setUploadError('Solo se permiten archivos PDF.');
      return;
    }

    if (file.type && file.type !== 'application/pdf') {
      setUploadError('Solo se permiten archivos PDF.');
      return;
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      setUploadError('El archivo no puede superar los 10 MB.');
      return;
    }

    setIsUploading(true);

    try {
      const companyId = isSuperAdmin && selectedCompanyId ? selectedCompanyId : undefined;
      const uploaded = await uploadDocument(file, companyId);
      setSuccessMessage(`Documento "${uploaded.originalFileName}" subido correctamente.`);
      await loadDocuments();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible subir el documento';
      setUploadError(message);
    } finally {
      setIsUploading(false);
    }
  };

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Documentos
      </Typography>

      <Paper sx={{ p: 2, mb: 3 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }}>
          {isSuperAdmin && companies.length > 0 && (
            <FormControl sx={{ minWidth: 240 }} size="small">
              <InputLabel id="company-select-label">Empresa</InputLabel>
              <Select
                labelId="company-select-label"
                label="Empresa"
                value={selectedCompanyId}
                onChange={(e) => setSelectedCompanyId(e.target.value)}
              >
                <MenuItem value="">
                  <em>Empresa por defecto</em>
                </MenuItem>
                {companies.map((company) => (
                  <MenuItem key={company.id} value={company.id}>
                    {company.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}

          <input
            ref={fileInputRef}
            type="file"
            accept=".pdf,application/pdf"
            hidden
            onChange={handleFileChange}
          />

          <Button
            variant="contained"
            startIcon={isUploading ? <CircularProgress size={18} color="inherit" /> : <UploadFileIcon />}
            onClick={handleUploadClick}
            disabled={isUploading}
          >
            {isUploading ? 'Subiendo...' : 'Subir documento'}
          </Button>
        </Stack>

        {successMessage && (
          <Alert severity="success" sx={{ mt: 2 }}>
            {successMessage}
          </Alert>
        )}

        {uploadError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            {uploadError}
          </Alert>
        )}
      </Paper>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Nombre</TableCell>
              <TableCell>Tamaño</TableCell>
              <TableCell>Estado</TableCell>
              <TableCell>Empresa</TableCell>
              <TableCell>Fecha de carga</TableCell>
              <TableCell>Error</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {documents.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} align="center">
                  No hay documentos registrados.
                </TableCell>
              </TableRow>
            ) : (
              documents.map((document) => (
                <TableRow key={document.id}>
                  <TableCell>{document.originalFileName}</TableCell>
                  <TableCell>{formatFileSize(document.sizeBytes)}</TableCell>
                  <TableCell>
                    <Chip
                      label={statusLabels[document.status] ?? document.status}
                      color={statusColors[document.status] ?? 'default'}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>{document.companyName}</TableCell>
                  <TableCell>
                    {new Date(document.createdAt).toLocaleString('es-CL')}
                  </TableCell>
                  <TableCell>{document.errorMessage ?? '-'}</TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
}
