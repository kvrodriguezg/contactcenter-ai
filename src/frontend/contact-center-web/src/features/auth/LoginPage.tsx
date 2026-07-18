import { useState, type FormEvent } from 'react';
import {
  Alert,
  Box,
  Button,
  Container,
  Divider,
  Paper,
  TextField,
  Typography,
} from '@mui/material';
import { isAuth0Mode } from './authConfig';
import { useAuth } from './useAuth';

export function LoginPage() {
  const { login, loginWithAuth0, authError, clearAuthError } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const displayError = error || authError || '';

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    clearAuthError();
    setError('');
    setIsSubmitting(true);

    try {
      await login(email.trim(), password);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible iniciar sesión';
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleAuth0Login = async () => {
    clearAuthError();
    setError('');
    setIsSubmitting(true);

    try {
      await loginWithAuth0();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible iniciar sesión con Auth0';
      setError(message);
      setIsSubmitting(false);
    }
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.default',
        px: 2,
      }}
    >
      <Container maxWidth="sm">
        <Paper elevation={2} sx={{ p: 4 }}>
          <Typography variant="h4" component="h1" gutterBottom>
            ContactCenterAI
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
            {isAuth0Mode
              ? 'Inicie sesión con Auth0 para acceder a la plataforma.'
              : 'Ingrese sus credenciales para acceder a la plataforma.'}
          </Typography>

          {displayError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {displayError}
            </Alert>
          )}

          {isAuth0Mode ? (
            <Button
              type="button"
              variant="contained"
              fullWidth
              size="large"
              disabled={isSubmitting}
              onClick={() => void handleAuth0Login()}
            >
              {isSubmitting ? 'Redirigiendo...' : 'Ingresar con Auth0'}
            </Button>
          ) : (
            <Box component="form" onSubmit={handleSubmit}>
              <TextField
                label="Correo electrónico"
                type="email"
                fullWidth
                required
                margin="normal"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                autoComplete="email"
              />
              <TextField
                label="Contraseña"
                type="password"
                fullWidth
                required
                margin="normal"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="current-password"
              />
              <Button
                type="submit"
                variant="contained"
                fullWidth
                size="large"
                sx={{ mt: 3 }}
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Ingresando...' : 'Ingresar'}
              </Button>
            </Box>
          )}

          {!isAuth0Mode && (
            <>
              <Divider sx={{ my: 3 }} />
              <Typography variant="caption" color="text.secondary">
                El proveedor Auth0 se activa con VITE_AUTH_PROVIDER=Auth0.
              </Typography>
            </>
          )}
        </Paper>
      </Container>
    </Box>
  );
}
