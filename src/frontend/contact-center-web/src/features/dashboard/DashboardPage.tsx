import {
  Box,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  Paper,
  Typography,
} from '@mui/material';
import Grid from '@mui/material/Grid2';
import BusinessIcon from '@mui/icons-material/Business';
import ChatIcon from '@mui/icons-material/Chat';
import DescriptionIcon from '@mui/icons-material/Description';
import GroupIcon from '@mui/icons-material/Group';
import HistoryIcon from '@mui/icons-material/History';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';

const modules = [
  { title: 'Empresas', description: 'Administración de tenants', icon: <BusinessIcon /> },
  { title: 'Usuarios', description: 'Gestión de cuentas y roles', icon: <GroupIcon /> },
  { title: 'Documentos', description: 'Base de conocimiento', icon: <DescriptionIcon />, path: '/documents' },
  { title: 'Chat IA', description: 'Asistente inteligente RAG', icon: <ChatIcon />, path: '/chat' },
  { title: 'Historial', description: 'Conversaciones registradas', icon: <HistoryIcon />, path: '/chat' },
];

export function DashboardPage() {
  const { user } = useAuth();
  const navigate = useNavigate();

  return (
    <Box>
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h4" gutterBottom>
          Bienvenido
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 2 }}>
          {user?.email}
        </Typography>
        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
          <Chip label={`Rol: ${user?.role ?? '-'}`} color="primary" variant="outlined" />
          {user?.companyName && (
            <Chip label={`Empresa: ${user.companyName}`} variant="outlined" />
          )}
        </Box>
      </Paper>

      <Typography variant="h6" sx={{ mb: 2 }}>
        Módulos del sistema
      </Typography>
      <Grid container spacing={2}>
        {modules.map((module) => (
          <Grid key={module.title} size={{ xs: 12, sm: 6, md: 4 }}>
            <Card variant="outlined" sx={{ height: '100%' }}>
              {'path' in module && module.path ? (
                <CardActionArea sx={{ height: '100%' }} onClick={() => navigate(module.path!)}>
                  <CardContent>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                      {module.icon}
                      <Typography variant="h6">{module.title}</Typography>
                    </Box>
                    <Typography variant="body2" color="text.secondary">
                      {module.description}
                    </Typography>
                  </CardContent>
                </CardActionArea>
              ) : (
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                    {module.icon}
                    <Typography variant="h6">{module.title}</Typography>
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    {module.description}
                  </Typography>
                </CardContent>
              )}
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
}
