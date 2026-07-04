import { Alert, Paper, Stack, Typography } from '@mui/material';

export function HomePage() {
  return (
    <Paper sx={{ p: 4 }}>
      <Stack spacing={2}>
        <Typography variant="h4" component="h1">
          Plataforma de Soporte Inteligente
        </Typography>
        <Typography color="text.secondary">
          Infraestructura base lista. Los módulos funcionales se implementarán en las siguientes etapas.
        </Typography>
        <Alert severity="info">
          Frontend React + TypeScript + Vite + Material UI configurado correctamente.
        </Alert>
      </Stack>
    </Paper>
  );
}
