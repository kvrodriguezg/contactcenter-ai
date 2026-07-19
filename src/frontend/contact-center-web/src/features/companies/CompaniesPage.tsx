import { useCallback, useEffect, useState } from 'react';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import PauseCircleOutlineIcon from '@mui/icons-material/PauseCircleOutline';
import PlayCircleOutlineIcon from '@mui/icons-material/PlayCircleOutline';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
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
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import {
  activateCompany,
  createCompany,
  deactivateCompany,
  getCompanies,
  updateCompany,
} from '../../shared/api/authApi';
import type { Company } from '../../shared/types/auth';
import { useAuth } from '../auth/useAuth';

interface CompanyFormState {
  name: string;
  status: 'Active' | 'Inactive';
}

const emptyForm: CompanyFormState = { name: '', status: 'Active' };

export function CompaniesPage() {
  const { user } = useAuth();
  const isSuperAdmin = user?.role === 'SuperAdmin';

  const [companies, setCompanies] = useState<Company[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingCompany, setEditingCompany] = useState<Company | null>(null);
  const [form, setForm] = useState<CompanyFormState>(emptyForm);
  const [formError, setFormError] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [statusChangingId, setStatusChangingId] = useState<string | null>(null);

  const loadCompanies = useCallback(async () => {
    setError('');
    try {
      const data = await getCompanies();
      setCompanies(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible cargar las empresas';
      setError(message);
    }
  }, []);

  useEffect(() => {
    const load = async () => {
      await loadCompanies();
      setIsLoading(false);
    };
    void load();
  }, [loadCompanies]);

  const openCreateDialog = () => {
    setEditingCompany(null);
    setForm(emptyForm);
    setFormError('');
    setDialogOpen(true);
  };

  const openEditDialog = (company: Company) => {
    setEditingCompany(company);
    setForm({ name: company.name, status: company.status === 'Inactive' ? 'Inactive' : 'Active' });
    setFormError('');
    setDialogOpen(true);
  };

  const closeDialog = () => {
    if (isSaving) {
      return;
    }
    setDialogOpen(false);
  };

  const handleSubmit = async () => {
    setFormError('');

    if (!form.name.trim()) {
      setFormError('El nombre de la empresa es obligatorio.');
      return;
    }

    setIsSaving(true);
    try {
      if (editingCompany) {
        await updateCompany(editingCompany.id, { name: form.name.trim(), status: form.status });
        setSuccessMessage(`Empresa "${form.name.trim()}" actualizada correctamente.`);
      } else {
        await createCompany({ name: form.name.trim() });
        setSuccessMessage(`Empresa "${form.name.trim()}" creada correctamente.`);
      }
      setDialogOpen(false);
      await loadCompanies();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible guardar la empresa';
      setFormError(message);
    } finally {
      setIsSaving(false);
    }
  };

  const handleToggleStatus = async (company: Company) => {
    setError('');
    setSuccessMessage('');
    setStatusChangingId(company.id);
    try {
      if (company.status === 'Active') {
        await deactivateCompany(company.id);
        setSuccessMessage(`Empresa "${company.name}" desactivada.`);
      } else {
        await activateCompany(company.id);
        setSuccessMessage(`Empresa "${company.name}" activada.`);
      }
      await loadCompanies();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible cambiar el estado de la empresa';
      setError(message);
    } finally {
      setStatusChangingId(null);
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
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h4" gutterBottom sx={{ mb: 0 }}>
          Empresas
        </Typography>

        {isSuperAdmin && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={openCreateDialog}>
            Nueva empresa
          </Button>
        )}
      </Stack>

      {successMessage && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccessMessage('')}>
          {successMessage}
        </Alert>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>
          {error}
        </Alert>
      )}

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Nombre</TableCell>
              <TableCell>Estado</TableCell>
              <TableCell>Fecha de creación</TableCell>
              {isSuperAdmin && <TableCell align="right">Acciones</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {companies.length === 0 ? (
              <TableRow>
                <TableCell colSpan={isSuperAdmin ? 4 : 3} align="center">
                  No hay empresas registradas.
                </TableCell>
              </TableRow>
            ) : (
              companies.map((company) => (
                <TableRow key={company.id}>
                  <TableCell>{company.name}</TableCell>
                  <TableCell>
                    <Chip
                      label={company.status === 'Active' ? 'Activa' : 'Inactiva'}
                      color={company.status === 'Active' ? 'success' : 'default'}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    {new Date(company.createdAt).toLocaleDateString('es-CL')}
                  </TableCell>
                  {isSuperAdmin && (
                    <TableCell align="right">
                      <Tooltip title="Editar">
                        <IconButton size="small" onClick={() => openEditDialog(company)}>
                          <EditIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title={company.status === 'Active' ? 'Desactivar' : 'Activar'}>
                        <span>
                          <IconButton
                            size="small"
                            onClick={() => handleToggleStatus(company)}
                            disabled={statusChangingId === company.id}
                          >
                            {statusChangingId === company.id ? (
                              <CircularProgress size={18} />
                            ) : company.status === 'Active' ? (
                              <PauseCircleOutlineIcon fontSize="small" />
                            ) : (
                              <PlayCircleOutlineIcon fontSize="small" />
                            )}
                          </IconButton>
                        </span>
                      </Tooltip>
                    </TableCell>
                  )}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog open={dialogOpen} onClose={closeDialog} fullWidth maxWidth="sm">
        <DialogTitle>{editingCompany ? 'Editar empresa' : 'Nueva empresa'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {formError && <Alert severity="error">{formError}</Alert>}

            <TextField
              label="Nombre"
              value={form.name}
              onChange={(e) => setForm((prev) => ({ ...prev, name: e.target.value }))}
              autoFocus
              fullWidth
              required
            />

            {editingCompany && (
              <FormControl fullWidth>
                <InputLabel id="company-status-label">Estado</InputLabel>
                <Select
                  labelId="company-status-label"
                  label="Estado"
                  value={form.status}
                  onChange={(e) =>
                    setForm((prev) => ({ ...prev, status: e.target.value as 'Active' | 'Inactive' }))
                  }
                >
                  <MenuItem value="Active">Activa</MenuItem>
                  <MenuItem value="Inactive">Inactiva</MenuItem>
                </Select>
              </FormControl>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDialog} disabled={isSaving}>
            Cancelar
          </Button>
          <Button onClick={handleSubmit} variant="contained" disabled={isSaving}>
            {isSaving ? <CircularProgress size={20} /> : 'Guardar'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
