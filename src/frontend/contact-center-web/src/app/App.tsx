import { Auth0Provider } from '@auth0/auth0-react';
import { CssBaseline, ThemeProvider } from '@mui/material';
import type { ReactNode } from 'react';
import { BrowserRouter, useNavigate } from 'react-router-dom';
import {
  auth0Config,
  isAuth0Configured,
  isAuth0Mode,
} from '../features/auth/authConfig';
import { AppRouter } from './router';
import { appTheme } from './theme';

function Auth0ProviderWithNavigate({ children }: { children: ReactNode }) {
  const navigate = useNavigate();

  if (!isAuth0Mode || !isAuth0Configured()) {
    return children;
  }

  const onRedirectCallback = (appState?: { returnTo?: string }) => {
    navigate(appState?.returnTo || '/dashboard', { replace: true });
  };

  return (
    <Auth0Provider
      domain={auth0Config.domain}
      clientId={auth0Config.clientId}
      authorizationParams={{
        redirect_uri: auth0Config.redirectUri,
        audience: auth0Config.audience,
      }}
      cacheLocation="localstorage"
      useRefreshTokens={false}
      onRedirectCallback={onRedirectCallback}
    >
      {children}
    </Auth0Provider>
  );
}

function App() {
  return (
    <ThemeProvider theme={appTheme}>
      <CssBaseline />
      <BrowserRouter>
        <Auth0ProviderWithNavigate>
          <AppRouter />
        </Auth0ProviderWithNavigate>
      </BrowserRouter>
    </ThemeProvider>
  );
}

export default App;
