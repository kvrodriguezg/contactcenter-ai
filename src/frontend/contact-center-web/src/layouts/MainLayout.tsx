import { AppBar, Box, Container, Toolbar, Typography } from '@mui/material';
import { Outlet } from 'react-router-dom';

export function MainLayout() {
  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <AppBar position="static" elevation={0}>
        <Toolbar>
          <Typography variant="h6" component="div">
            ContactCenterAI
          </Typography>
        </Toolbar>
      </AppBar>
      <Container component="main" sx={{ py: 4, flex: 1 }}>
        <Outlet />
      </Container>
    </Box>
  );
}
