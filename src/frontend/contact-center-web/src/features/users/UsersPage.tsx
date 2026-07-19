import { useCallback, useEffect, useMemo, useState } from 'react';
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
import { createUser, getCompanies, getUsers, updateUser } from '../../shared/api/authApi';
import type { Company, UserListItem } from '../../shared/types/auth';
import { isAuth0Mode } from '../auth/authConfig';
import { useAuth } from '../auth/useAuth';

type RoleValue = 'SuperAdmin' | 'CompanyAdmin' | 'Agent';

const roleLabels: Record<string, string> = {
  SuperAdmin: 'Super administrador',
  CompanyAdmin: 'Administrador de empresa',
  Agent: 'Agente',
};

const providerLabels: Record<string, string> = {
  Local: 'Local',
  Auth0: 'Auth0',
};

interface UserFormState {
  email: string;
  name: string;
  role: RoleValue;
  companyId: string;
  isActive: boolean;
  password: string;
  externalSubject: string;
}

const emptyForm: UserFormState = {
  email: '',
  name: '',
  role: 'Agent',
  companyId: '',
  isActive: true,
  password: '',
  externalSubject: '',
};

export function UsersPage() {
  const { user } = useAuth();
  const isSuperAdmin = user?.role === 'SuperAdmin';
  const isCompanyAdmin = user?.role === 'CompanyAdmin';
  const canManageUsers = isSuperAdmin || isCompanyAdmin;

  const [users, setUsers] = useState<UserListItem[]>([]);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserListItem | null>(null);
  const [form, setForm] = useState<UserFormState>(emptyForm);
  const [formError, setFormError] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [statusChangingId, setStatusChangingId] = useState<string | null>(null);

  const availableRoles: RoleValue[] = useMemo(
    () => (isSuperAdmin ? ['SuperAdmin', 'CompanyAdmin', 'Agent'] : ['CompanyAdmin', 'Agent']),
    [isSuperAdmin],
  );

  const activeCompanies = useMemo(() => companies.filter((c) => c.status === 'Active'), [companies]);

  const loadData = useCallback(async () => {
    setError('');
    try {
      const [usersData, companiesData] = await Promise.all([getUsers(), getCompanies()]);
      setUsers(usersData);
      setCompanies(companiesData);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible cargar los usuarios';
      setError(message);
    }
  }, []);

  useEffect(() => {
    const load = async () => {
      await loadData();
      setIsLoading(false);
    };
    void load();
  }, [loadData]);

  const openCreateDialog = () => {
    setEditingUser(null);
    setForm({
      ...emptyForm,
      role: isCompanyAdmin ? 'Agent' : 'Agent',
      companyId: isCompanyAdmin ? (user?.companyId ?? '') : '',
    });
    setFormError('');
    setDialogOpen(true);
  };

  const openEditDialog = (target: UserListItem) => {
    setEditingUser(target);
    setForm({
      email: target.email,
      name: target.name ?? '',
      role: (target.role as RoleValue) ?? 'Agent',
      companyId: target.companyId ?? '',
      isActive: target.isActive,
      password: '',
      externalSubject: target.externalSubject ?? '',
    });
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

    if (!editingUser && !form.email.trim()) {
      setFormError('El correo electrónico es obligatorio.');
      return;
    }

    if (form.role !== 'SuperAdmin' && !form.companyId) {
      setFormError('Debe seleccionar una empresa para este rol.');
      return;
    }

    const auth0Id = form.externalSubject.trim();
    if (isAuth0Mode && !auth0Id) {
      setFormError('El ID de Auth0 es obligatorio (claim sub completo, p. ej. auth0|...).');
      return;
    }

    setIsSaving(true);
    try {
      if (editingUser) {
        const updated = await updateUser(editingUser.id, {
          role: form.role,
          isActive: form.isActive,
          companyId: form.role === 'SuperAdmin' ? null : form.companyId || null,
          name: form.name.trim() === '' ? null : form.name.trim(),
          externalSubject: auth0Id,
        });
        setSuccessMessage(`Usuario "${updated.email}" actualizado correctamente.`);
      } else {
        const created = await createUser({
          email: form.email.trim(),
          name: form.name.trim() === '' ? null : form.name.trim(),
          role: form.role,
          companyId: form.role === 'SuperAdmin' ? null : form.companyId || null,
          password: form.password.trim() === '' ? null : form.password.trim(),
          externalSubject: auth0Id === '' ? null : auth0Id,
        });
        setSuccessMessage(`Usuario "${created.email}" creado correctamente.`);
      }
      setDialogOpen(false);
      await loadData();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible guardar el usuario';
      setFormError(message);
    } finally {
      setIsSaving(false);
    }
  };

  const handleToggleStatus = async (target: UserListItem) => {
    setError('');
    setSuccessMessage('');
    setStatusChangingId(target.id);
    try {
      const updated = await updateUser(target.id, {
        role: target.role,
        isActive: !target.isActive,
        companyId: target.companyId ?? null,
      });
      setSuccessMessage(
        `Usuario "${updated.email}" ${updated.isActive ? 'activado' : 'desactivado'} correctamente.`,
      );
      await loadData();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible cambiar el estado del usuario';
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
          Usuarios
        </Typography>

        {canManageUsers && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={openCreateDialog}>
            Nuevo usuario
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
              <TableCell>Email</TableCell>
              <TableCell>Nombre</TableCell>
              <TableCell>Rol</TableCell>
              <TableCell>Empresa</TableCell>
              <TableCell>Estado</TableCell>
              <TableCell>Proveedor</TableCell>
              {canManageUsers && <TableCell align="right">Acciones</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {users.length === 0 ? (
              <TableRow>
                <TableCell colSpan={canManageUsers ? 7 : 6} align="center">
                  No hay usuarios registrados.
                </TableCell>
              </TableRow>
            ) : (
              users.map((target) => (
                <TableRow key={target.id}>
                  <TableCell>{target.email}</TableCell>
                  <TableCell>{target.name ?? '-'}</TableCell>
                  <TableCell>{roleLabels[target.role] ?? target.role}</TableCell>
                  <TableCell>{target.companyName ?? '-'}</TableCell>
                  <TableCell>
                    <Chip
                      label={target.isActive ? 'Activo' : 'Inactivo'}
                      color={target.isActive ? 'success' : 'default'}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={providerLabels[target.authenticationProvider] ?? target.authenticationProvider}
                      variant="outlined"
                      size="small"
                    />
                  </TableCell>
                  {canManageUsers && (
                    <TableCell align="right">
                      <Tooltip title="Editar">
                        <IconButton size="small" onClick={() => openEditDialog(target)}>
                          <EditIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title={target.isActive ? 'Desactivar' : 'Activar'}>
                        <span>
                          <IconButton
                            size="small"
                            onClick={() => handleToggleStatus(target)}
                            disabled={statusChangingId === target.id}
                          >
                            {statusChangingId === target.id ? (
                              <CircularProgress size={18} />
                            ) : target.isActive ? (
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
        <DialogTitle>{editingUser ? 'Editar usuario' : 'Nuevo usuario'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {formError && <Alert severity="error">{formError}</Alert>}

            <TextField
              label="Correo electrónico"
              type="email"
              value={form.email}
              onChange={(e) => setForm((prev) => ({ ...prev, email: e.target.value }))}
              disabled={!!editingUser}
              autoFocus={!editingUser}
              fullWidth
              required
            />

            <TextField
              label="Nombre (opcional)"
              value={form.name}
              onChange={(e) => setForm((prev) => ({ ...prev, name: e.target.value }))}
              fullWidth
            />

            <TextField
              label="ID de Auth0"
              value={form.externalSubject}
              onChange={(e) => setForm((prev) => ({ ...prev, externalSubject: e.target.value }))}
              fullWidth
              required={isAuth0Mode}
              placeholder="auth0|687d1234567890abcdef"
              helperText={
                isAuth0Mode
                  ? 'Obligatorio. Pegue el claim sub completo del usuario en Auth0.'
                  : 'Opcional. Claim sub completo si el usuario iniciará sesión con Auth0.'
              }
              inputProps={{ autoComplete: 'off', spellCheck: false }}
            />

            <FormControl fullWidth>
              <InputLabel id="user-role-label">Rol</InputLabel>
              <Select
                labelId="user-role-label"
                label="Rol"
                value={form.role}
                onChange={(e) => setForm((prev) => ({ ...prev, role: e.target.value as RoleValue }))}
              >
                {availableRoles.map((role) => (
                  <MenuItem key={role} value={role}>
                    {roleLabels[role]}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            {form.role !== 'SuperAdmin' && (
              <FormControl fullWidth>
                <InputLabel id="user-company-label">Empresa</InputLabel>
                <Select
                  labelId="user-company-label"
                  label="Empresa"
                  value={form.companyId}
                  disabled={isCompanyAdmin}
                  onChange={(e) => setForm((prev) => ({ ...prev, companyId: e.target.value }))}
                >
                  {activeCompanies.map((company) => (
                    <MenuItem key={company.id} value={company.id}>
                      {company.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}

            {!editingUser && (
              <TextField
                label="Contraseña (opcional, mín. 8 caracteres)"
                type="password"
                value={form.password}
                onChange={(e) => setForm((prev) => ({ ...prev, password: e.target.value }))}
                fullWidth
                helperText="Déjelo en blanco si el usuario iniciará sesión mediante Auth0."
              />
            )}

            {editingUser && (
              <FormControl fullWidth>
                <InputLabel id="user-status-label">Estado</InputLabel>
                <Select
                  labelId="user-status-label"
                  label="Estado"
                  value={form.isActive ? 'active' : 'inactive'}
                  onChange={(e) =>
                    setForm((prev) => ({ ...prev, isActive: e.target.value === 'active' }))
                  }
                >
                  <MenuItem value="active">Activo</MenuItem>
                  <MenuItem value="inactive">Inactivo</MenuItem>
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
