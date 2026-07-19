import { useCallback, useEffect, useMemo, useState } from 'react';
import AddIcon from '@mui/icons-material/Add';
import ConfirmationNumberIcon from '@mui/icons-material/ConfirmationNumber';
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
  Typography,
} from '@mui/material';
import { getUsers } from '../../shared/api/authApi';
import {
  assignTicket,
  changeTicketStatus,
  createTicket,
  getTickets,
  resolveTicket,
} from '../../shared/api/ticketsApi';
import type { UserListItem } from '../../shared/types/auth';
import type {
  CreateTicketPayload,
  Ticket,
  TicketPriority,
  TicketStatus,
} from '../../shared/types/tickets';
import { useAuth } from '../auth/useAuth';

const PRIORITIES: TicketPriority[] = ['Low', 'Medium', 'High', 'Critical'];
const STATUSES: TicketStatus[] = ['Pending', 'InReview', 'Resolved', 'Closed'];

const priorityLabels: Record<string, string> = {
  Low: 'Baja',
  Medium: 'Media',
  High: 'Alta',
  Critical: 'Crítica',
};

const statusLabels: Record<string, string> = {
  Pending: 'Pendiente',
  InReview: 'En revisión',
  Resolved: 'Resuelto',
  Closed: 'Cerrado',
};

const priorityColors: Record<string, 'default' | 'info' | 'warning' | 'error'> = {
  Low: 'default',
  Medium: 'info',
  High: 'warning',
  Critical: 'error',
};

const statusColors: Record<string, 'default' | 'info' | 'warning' | 'success' | 'secondary'> = {
  Pending: 'warning',
  InReview: 'info',
  Resolved: 'success',
  Closed: 'default',
};

interface TicketFormState {
  subject: string;
  description: string;
  priority: TicketPriority;
  conversationId: string;
}

const emptyForm: TicketFormState = {
  subject: '',
  description: '',
  priority: 'Medium',
  conversationId: '',
};

function formatDate(value: string): string {
  return new Date(value).toLocaleString('es-CL');
}

export function TicketsPage() {
  const { user } = useAuth();
  const canManage = user?.role === 'SuperAdmin' || user?.role === 'CompanyAdmin';

  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const [statusFilter, setStatusFilter] = useState<TicketStatus | ''>('');
  const [priorityFilter, setPriorityFilter] = useState<TicketPriority | ''>('');

  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState<TicketFormState>(emptyForm);
  const [formError, setFormError] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  const [manageTicket, setManageTicket] = useState<Ticket | null>(null);
  const [assignUserId, setAssignUserId] = useState('');
  const [nextStatus, setNextStatus] = useState<TicketStatus>('InReview');
  const [resolution, setResolution] = useState('');
  const [manageError, setManageError] = useState('');
  const [isManaging, setIsManaging] = useState(false);

  const companyUsers = useMemo(() => {
    if (!manageTicket) {
      return [];
    }
    return users.filter(
      (u) => u.isActive && u.companyId === manageTicket.companyId && u.role !== 'SuperAdmin',
    );
  }, [users, manageTicket]);

  const loadTickets = useCallback(async () => {
    setError('');
    try {
      const data = await getTickets({
        status: statusFilter,
        priority: priorityFilter,
      });
      setTickets(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible cargar los tickets';
      setError(message);
    }
  }, [statusFilter, priorityFilter]);

  useEffect(() => {
    const load = async () => {
      setIsLoading(true);
      await loadTickets();
      if (canManage) {
        try {
          setUsers(await getUsers());
        } catch {
          // Assignment list is optional; manage actions will surface errors.
        }
      }
      setIsLoading(false);
    };
    void load();
  }, [loadTickets, canManage]);

  const openCreateDialog = () => {
    setForm(emptyForm);
    setFormError('');
    setCreateOpen(true);
  };

  const closeCreateDialog = () => {
    if (isSaving) {
      return;
    }
    setCreateOpen(false);
  };

  const handleCreate = async () => {
    setFormError('');
    if (!form.subject.trim()) {
      setFormError('El asunto es obligatorio.');
      return;
    }
    if (!form.description.trim()) {
      setFormError('La descripción es obligatoria.');
      return;
    }

    const payload: CreateTicketPayload = {
      subject: form.subject.trim(),
      description: form.description.trim(),
      priority: form.priority,
      conversationId: form.conversationId.trim() || null,
    };

    setIsSaving(true);
    try {
      await createTicket(payload);
      setSuccessMessage('Ticket creado correctamente.');
      setCreateOpen(false);
      await loadTickets();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'No fue posible crear el ticket.');
    } finally {
      setIsSaving(false);
    }
  };

  const openManageDialog = (ticket: Ticket) => {
    setManageTicket(ticket);
    setAssignUserId(ticket.assignedToUserId ?? '');
    setNextStatus((ticket.status as TicketStatus) || 'Pending');
    setResolution(ticket.resolution ?? '');
    setManageError('');
  };

  const closeManageDialog = () => {
    if (isManaging) {
      return;
    }
    setManageTicket(null);
  };

  const handleAssign = async () => {
    if (!manageTicket || !assignUserId) {
      setManageError('Seleccione un responsable.');
      return;
    }
    setIsManaging(true);
    setManageError('');
    try {
      await assignTicket(manageTicket.id, { assignedToUserId: assignUserId });
      setSuccessMessage('Responsable asignado correctamente.');
      setManageTicket(null);
      await loadTickets();
    } catch (err) {
      setManageError(err instanceof Error ? err.message : 'No fue posible asignar el responsable.');
    } finally {
      setIsManaging(false);
    }
  };

  const handleChangeStatus = async () => {
    if (!manageTicket) {
      return;
    }
    setIsManaging(true);
    setManageError('');
    try {
      await changeTicketStatus(manageTicket.id, { status: nextStatus });
      setSuccessMessage('Estado actualizado correctamente.');
      setManageTicket(null);
      await loadTickets();
    } catch (err) {
      setManageError(err instanceof Error ? err.message : 'No fue posible cambiar el estado.');
    } finally {
      setIsManaging(false);
    }
  };

  const handleResolve = async () => {
    if (!manageTicket) {
      return;
    }
    if (!resolution.trim()) {
      setManageError('La resolución es obligatoria.');
      return;
    }
    setIsManaging(true);
    setManageError('');
    try {
      await resolveTicket(manageTicket.id, { resolution: resolution.trim() });
      setSuccessMessage('Ticket resuelto correctamente.');
      setManageTicket(null);
      await loadTickets();
    } catch (err) {
      setManageError(err instanceof Error ? err.message : 'No fue posible resolver el ticket.');
    } finally {
      setIsManaging(false);
    }
  };

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        alignItems={{ sm: 'center' }}
        justifyContent="space-between"
        sx={{ mb: 3 }}
      >
        <Box>
          <Typography variant="h4" gutterBottom>
            Tickets
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Escalamiento de consultas no resueltas desde el Chat IA.
          </Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreateDialog}>
          Nuevo ticket
        </Button>
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

      <Paper sx={{ p: 2, mb: 3 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }}>
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <InputLabel id="status-filter-label">Estado</InputLabel>
            <Select
              labelId="status-filter-label"
              label="Estado"
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value as TicketStatus | '')}
            >
              <MenuItem value="">
                <em>Todos</em>
              </MenuItem>
              {STATUSES.map((status) => (
                <MenuItem key={status} value={status}>
                  {statusLabels[status]}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <InputLabel id="priority-filter-label">Prioridad</InputLabel>
            <Select
              labelId="priority-filter-label"
              label="Prioridad"
              value={priorityFilter}
              onChange={(e) => setPriorityFilter(e.target.value as TicketPriority | '')}
            >
              <MenuItem value="">
                <em>Todas</em>
              </MenuItem>
              {PRIORITIES.map((priority) => (
                <MenuItem key={priority} value={priority}>
                  {priorityLabels[priority]}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Stack>
      </Paper>

      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Asunto</TableCell>
              <TableCell>Empresa</TableCell>
              <TableCell>Creador</TableCell>
              <TableCell>Prioridad</TableCell>
              <TableCell>Estado</TableCell>
              <TableCell>Responsable</TableCell>
              <TableCell>Fecha</TableCell>
              {canManage && <TableCell align="right">Acciones</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {tickets.length === 0 ? (
              <TableRow>
                <TableCell colSpan={canManage ? 8 : 7} align="center">
                  <Box sx={{ py: 4, color: 'text.secondary' }}>
                    <ConfirmationNumberIcon sx={{ fontSize: 40, mb: 1, opacity: 0.4 }} />
                    <Typography>No hay tickets para mostrar.</Typography>
                  </Box>
                </TableCell>
              </TableRow>
            ) : (
              tickets.map((ticket) => (
                <TableRow key={ticket.id} hover>
                  <TableCell>{ticket.subject}</TableCell>
                  <TableCell>{ticket.companyName || '-'}</TableCell>
                  <TableCell>{ticket.createdByName || ticket.createdByEmail || '-'}</TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      label={priorityLabels[ticket.priority] ?? ticket.priority}
                      color={priorityColors[ticket.priority] ?? 'default'}
                    />
                  </TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      label={statusLabels[ticket.status] ?? ticket.status}
                      color={statusColors[ticket.status] ?? 'default'}
                      variant="outlined"
                    />
                  </TableCell>
                  <TableCell>{ticket.assignedToName || ticket.assignedToEmail || '-'}</TableCell>
                  <TableCell>{formatDate(ticket.createdAt)}</TableCell>
                  {canManage && (
                    <TableCell align="right">
                      <Button size="small" onClick={() => openManageDialog(ticket)}>
                        Gestionar
                      </Button>
                    </TableCell>
                  )}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog open={createOpen} onClose={closeCreateDialog} fullWidth maxWidth="sm">
        <DialogTitle>Crear ticket</DialogTitle>
        <DialogContent>
          {formError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {formError}
            </Alert>
          )}
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              label="Asunto"
              value={form.subject}
              onChange={(e) => setForm((prev) => ({ ...prev, subject: e.target.value }))}
              fullWidth
              required
            />
            <TextField
              label="Descripción"
              value={form.description}
              onChange={(e) => setForm((prev) => ({ ...prev, description: e.target.value }))}
              fullWidth
              required
              multiline
              minRows={3}
            />
            <FormControl fullWidth>
              <InputLabel id="priority-create-label">Prioridad</InputLabel>
              <Select
                labelId="priority-create-label"
                label="Prioridad"
                value={form.priority}
                onChange={(e) =>
                  setForm((prev) => ({ ...prev, priority: e.target.value as TicketPriority }))
                }
              >
                {PRIORITIES.map((priority) => (
                  <MenuItem key={priority} value={priority}>
                    {priorityLabels[priority]}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              label="Conversation ID (opcional)"
              value={form.conversationId}
              onChange={(e) => setForm((prev) => ({ ...prev, conversationId: e.target.value }))}
              fullWidth
              helperText="Se completa automáticamente al escalar desde Chat IA."
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeCreateDialog} disabled={isSaving}>
            Cancelar
          </Button>
          <Button variant="contained" onClick={() => void handleCreate()} disabled={isSaving}>
            {isSaving ? <CircularProgress size={22} /> : 'Crear'}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={!!manageTicket} onClose={closeManageDialog} fullWidth maxWidth="sm">
        <DialogTitle>Gestionar ticket</DialogTitle>
        <DialogContent>
          {manageError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {manageError}
            </Alert>
          )}
          {manageTicket && (
            <Stack spacing={2} sx={{ mt: 1 }}>
              <Typography variant="subtitle1">{manageTicket.subject}</Typography>
              <Typography variant="body2" color="text.secondary">
                {manageTicket.description}
              </Typography>

              <FormControl fullWidth size="small">
                <InputLabel id="assign-user-label">Responsable</InputLabel>
                <Select
                  labelId="assign-user-label"
                  label="Responsable"
                  value={assignUserId}
                  onChange={(e) => setAssignUserId(e.target.value)}
                >
                  {companyUsers.map((u) => (
                    <MenuItem key={u.id} value={u.id}>
                      {u.name || u.email} ({u.role})
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <Button
                variant="outlined"
                onClick={() => void handleAssign()}
                disabled={isManaging || !assignUserId}
              >
                Asignar responsable
              </Button>

              <FormControl fullWidth size="small">
                <InputLabel id="status-manage-label">Estado</InputLabel>
                <Select
                  labelId="status-manage-label"
                  label="Estado"
                  value={nextStatus}
                  onChange={(e) => setNextStatus(e.target.value as TicketStatus)}
                >
                  {STATUSES.map((status) => (
                    <MenuItem key={status} value={status}>
                      {statusLabels[status]}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <Button
                variant="outlined"
                onClick={() => void handleChangeStatus()}
                disabled={isManaging}
              >
                Cambiar estado
              </Button>

              <TextField
                label="Resolución"
                value={resolution}
                onChange={(e) => setResolution(e.target.value)}
                fullWidth
                multiline
                minRows={2}
              />
              <Button
                variant="contained"
                color="success"
                onClick={() => void handleResolve()}
                disabled={isManaging}
              >
                Resolver ticket
              </Button>
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={closeManageDialog} disabled={isManaging}>
            Cerrar
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
