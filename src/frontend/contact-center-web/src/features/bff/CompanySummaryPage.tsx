import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';
import { useAuth } from '../auth/useAuth';
import { graphqlRequest } from '../../shared/api/graphqlClient';

type CompanySummaryData = {
  me: {
    email: string;
    role: string;
    companyId: string | null;
    companyName: string | null;
  };
  companyById: {
    id: string;
    name: string;
    status: string;
    users: Array<{ id: string; email: string; role: string; isActive: boolean }>;
    documents: Array<{ id: string; originalFileName: string; status: string }>;
    tickets: Array<{ id: string; subject: string; status: string; priority: string }>;
  } | null;
};

const SUMMARY_BY_ID = `
query CompanySummary($companyId: UUID!) {
  me { email role companyId companyName }
  companyById(id: $companyId) {
    id
    name
    status
    users { id email role isActive }
    documents { id originalFileName status }
    tickets { id subject status priority }
  }
}
`;

const FIRST_COMPANY = `
query FirstCompany {
  companies { id }
}
`;

/**
 * Optional admin demo that reads a company summary via the GraphQL BFF.
 * REST remains the primary interface for CRUD; this page only demonstrates aggregation.
 */
export function CompanySummaryPage() {
  const { user } = useAuth();
  const [data, setData] = useState<CompanySummaryData | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        let companyId = user?.companyId ?? null;
        if (!companyId && user?.role === 'SuperAdmin') {
          const listed = await graphqlRequest<{ companies: Array<{ id: string }> }>(FIRST_COMPANY);
          companyId = listed.companies[0]?.id ?? null;
        }

        if (!companyId) {
          setError('No hay empresa disponible para resumir.');
          setLoading(false);
          return;
        }

        const result = await graphqlRequest<CompanySummaryData>(SUMMARY_BY_ID, { companyId });
        if (!cancelled) {
          setData(result);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Error GraphQL');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, [user]);

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h5" gutterBottom>
          Resumen de empresa (GraphQL demo)
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Vista demostrativa del BFF. REST sigue siendo la interfaz principal de administración.
        </Typography>
      </Box>

      {error && <Alert severity="warning">{error}</Alert>}

      {data?.companyById && (
        <>
          <Paper sx={{ p: 2 }}>
            <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
              <Typography variant="h6">{data.companyById.name}</Typography>
              <Chip size="small" label={data.companyById.status} />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              Consulta agregada: usuarios, documentos y tickets en una sola petición GraphQL.
            </Typography>
          </Paper>

          <Section title="Usuarios" empty={data.companyById.users.length === 0}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Email</TableCell>
                  <TableCell>Rol</TableCell>
                  <TableCell>Activo</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.companyById.users.map((u) => (
                  <TableRow key={u.id}>
                    <TableCell>{u.email}</TableCell>
                    <TableCell>{u.role}</TableCell>
                    <TableCell>{u.isActive ? 'Sí' : 'No'}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Section>

          <Section title="Documentos" empty={data.companyById.documents.length === 0}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Archivo</TableCell>
                  <TableCell>Estado</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.companyById.documents.map((d) => (
                  <TableRow key={d.id}>
                    <TableCell>{d.originalFileName}</TableCell>
                    <TableCell>{d.status}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Section>

          <Section title="Tickets" empty={data.companyById.tickets.length === 0}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Asunto</TableCell>
                  <TableCell>Estado</TableCell>
                  <TableCell>Prioridad</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.companyById.tickets.map((t) => (
                  <TableRow key={t.id}>
                    <TableCell>{t.subject}</TableCell>
                    <TableCell>{t.status}</TableCell>
                    <TableCell>{t.priority}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Section>
        </>
      )}
    </Stack>
  );
}

function Section({
  title,
  empty,
  children,
}: {
  title: string;
  empty: boolean;
  children: React.ReactNode;
}) {
  return (
    <Paper sx={{ p: 2 }}>
      <Typography variant="subtitle1" gutterBottom>
        {title}
      </Typography>
      {empty ? (
        <Typography variant="body2" color="text.secondary">
          Sin datos.
        </Typography>
      ) : (
        children
      )}
    </Paper>
  );
}
