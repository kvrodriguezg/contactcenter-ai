import { Auth0Provider } from '@auth0/auth0-react';
import { CssBaseline, ThemeProvider } from '@mui/material';
import type { ReactNode } from 'react';
import { BrowserRouter } from 'react-router-dom';
import { auth0Config, isAuth0Mode } from '../features/auth/authConfig';
import { AppRouter } from './router';
import { appTheme } from './theme';

function Auth0Root({ children }: { children: ReactNode }) {
  if (!isAuth0Mode) {
    return children;
  }

  return (
    <Auth0Provider
      domain={auth0Config.domain}
      clientId={auth0Config.clientId}
      authorizationParams={{
        redirect_uri: auth0Config.redirectUri,
        audience: auth0Config.audience,
      }}
      cacheLocation="memory"
      useRefreshTokens={false}
    >
      {children}
    </Auth0Provider>
  );
}

function App() {
  return (
    <ThemeProvider theme={appTheme}>
      <CssBaseline />
      <Auth0Root>
        <BrowserRouter>
          <AppRouter />
        </BrowserRouter>
      </Auth0Root>
    </ThemeProvider>
  );
}

export default App;
