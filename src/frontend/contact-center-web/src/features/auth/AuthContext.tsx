import {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { useAuth0 } from '@auth0/auth0-react';
import { useNavigate } from 'react-router-dom';
import { getCurrentUser, login as loginRequest } from '../../shared/api/authApi';
import { setOnUnauthorized } from '../../shared/api/client';
import type { CurrentUser } from '../../shared/types/auth';
import { LoadingScreen } from '../../shared/components/LoadingScreen';
import {
  AUTH_PROVIDER,
  auth0Config,
  formatAuth0Error,
  isAuth0Configured,
  isAuth0Mode,
} from './authConfig';
import {
  Auth0TokenProvider,
  LocalTokenProvider,
  setTokenProvider,
} from './tokenProvider';

interface AuthContextValue {
  user: CurrentUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  authProvider: typeof AUTH_PROVIDER;
  authError: string | null;
  login: (email: string, password: string) => Promise<void>;
  loginWithAuth0: () => Promise<void>;
  logout: () => void;
  clearAuthError: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  if (isAuth0Mode) {
    if (!isAuth0Configured()) {
      return <Auth0MisconfiguredProvider>{children}</Auth0MisconfiguredProvider>;
    }

    return <Auth0AuthProvider>{children}</Auth0AuthProvider>;
  }

  return <LocalAuthProvider>{children}</LocalAuthProvider>;
}

function Auth0MisconfiguredProvider({ children }: AuthProviderProps) {
  const value = useMemo(
    () => ({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      authProvider: AUTH_PROVIDER,
      authError:
        'Auth0 no está configurado. Defina VITE_AUTH0_DOMAIN y VITE_AUTH0_CLIENT_ID en el build.',
      login: async () => {
        throw new Error('Auth0 no está configurado.');
      },
      loginWithAuth0: async () => {
        throw new Error(
          'Auth0 no está configurado. Defina VITE_AUTH0_DOMAIN y VITE_AUTH0_CLIENT_ID.',
        );
      },
      logout: () => undefined,
      clearAuthError: () => undefined,
    }),
    [],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

function LocalAuthProvider({ children }: AuthProviderProps) {
  const navigate = useNavigate();
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [authError, setAuthError] = useState<string | null>(null);
  const tokenProvider = useMemo(() => new LocalTokenProvider(), []);

  useEffect(() => {
    setTokenProvider(tokenProvider);
  }, [tokenProvider]);

  const clearAuthError = useCallback(() => setAuthError(null), []);

  const logout = useCallback(() => {
    tokenProvider.clearToken();
    setUser(null);
    navigate('/login', { replace: true });
  }, [navigate, tokenProvider]);

  useEffect(() => {
    setOnUnauthorized(() => {
      setUser(null);
      navigate('/login', { replace: true });
    });
  }, [navigate]);

  useEffect(() => {
    const initializeAuth = async () => {
      const token = await tokenProvider.getToken();
      if (!token) {
        setIsLoading(false);
        return;
      }

      try {
        const currentUser = await getCurrentUser();
        setUser(currentUser);
      } catch {
        tokenProvider.clearToken();
        setUser(null);
      } finally {
        setIsLoading(false);
      }
    };

    void initializeAuth();
  }, [tokenProvider]);

  const login = useCallback(async (email: string, password: string) => {
    setAuthError(null);
    const response = await loginRequest(email, password);
    tokenProvider.setToken?.(response.accessToken);
    setUser({
      userId: response.userId,
      email: response.email,
      role: response.role,
      companyId: response.companyId,
      companyName: response.companyName,
      isActive: true,
    });
    navigate('/dashboard', { replace: true });
  }, [navigate, tokenProvider]);

  const loginWithAuth0 = useCallback(async () => {
    throw new Error('Auth0 no está activo. Configure VITE_AUTH_PROVIDER=Auth0.');
  }, []);

  const value = useMemo(
    () => ({
      user,
      isAuthenticated: user !== null,
      isLoading,
      authProvider: AUTH_PROVIDER,
      authError,
      login,
      loginWithAuth0,
      logout,
      clearAuthError,
    }),
    [user, isLoading, authError, login, loginWithAuth0, logout, clearAuthError],
  );

  if (isLoading) {
    return <LoadingScreen />;
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

function Auth0AuthProvider({ children }: AuthProviderProps) {
  const navigate = useNavigate();
  const {
    isAuthenticated: isAuth0Authenticated,
    isLoading: isAuth0Loading,
    loginWithRedirect,
    logout: auth0Logout,
    getAccessTokenSilently,
    error: auth0Error,
  } = useAuth0();

  const [user, setUser] = useState<CurrentUser | null>(null);
  const [isProfileLoading, setIsProfileLoading] = useState(true);
  const [authError, setAuthError] = useState<string | null>(null);

  useEffect(() => {
    setTokenProvider(
      new Auth0TokenProvider(() =>
        getAccessTokenSilently({
          authorizationParams: {
            audience: auth0Config.audience,
          },
        }),
      ),
    );
  }, [getAccessTokenSilently]);

  const clearAuthError = useCallback(() => setAuthError(null), []);

  const logout = useCallback(() => {
    setUser(null);
    setAuthError(null);
    auth0Logout({
      logoutParams: {
        returnTo: window.location.origin,
      },
    });
  }, [auth0Logout]);

  useEffect(() => {
    setOnUnauthorized(() => {
      setUser(null);
      navigate('/login', { replace: true });
    });
  }, [navigate]);

  useEffect(() => {
    if (auth0Error) {
      setAuthError(formatAuth0Error(auth0Error));
    }
  }, [auth0Error]);

  useEffect(() => {
    const loadProfile = async () => {
      if (isAuth0Loading) {
        return;
      }

      if (!isAuth0Authenticated) {
        setUser(null);
        setIsProfileLoading(false);
        return;
      }

      setIsProfileLoading(true);

      try {
        const currentUser = await getCurrentUser();
        if (!currentUser.isActive) {
          setAuthError('El usuario está inactivo.');
          setUser(null);
          return;
        }

        if (!currentUser.role) {
          setAuthError('El usuario no tiene un rol asignado en ContactCenterAI.');
          setUser(null);
          return;
        }

        setAuthError(null);
        setUser(currentUser);
      } catch (err) {
        const message = err instanceof Error
          ? err.message
          : 'No fue posible validar el perfil local.';
        setAuthError(message);
        setUser(null);
      } finally {
        setIsProfileLoading(false);
      }
    };

    void loadProfile();
  }, [isAuth0Authenticated, isAuth0Loading]);

  const login = useCallback(async () => {
    throw new Error('El login local está deshabilitado. Use Auth0.');
  }, []);

  const loginWithAuth0 = useCallback(async () => {
    if (!isAuth0Configured()) {
      throw new Error(
        'Auth0 no está configurado. Defina VITE_AUTH0_DOMAIN y VITE_AUTH0_CLIENT_ID.',
      );
    }

    if (typeof loginWithRedirect !== 'function') {
      throw new Error('loginWithRedirect no está disponible. Auth0Provider no está montado.');
    }

    setAuthError(null);

    try {
      await loginWithRedirect({
        appState: { returnTo: '/dashboard' },
        authorizationParams: {
          redirect_uri: auth0Config.redirectUri,
          audience: auth0Config.audience,
        },
      });
    } catch (error) {
      throw new Error(formatAuth0Error(error));
    }
  }, [loginWithRedirect]);

  const isLoading = isAuth0Loading || (isAuth0Authenticated && isProfileLoading);

  const value = useMemo(
    () => ({
      user,
      isAuthenticated: user !== null,
      isLoading,
      authProvider: AUTH_PROVIDER,
      authError,
      login,
      loginWithAuth0,
      logout,
      clearAuthError,
    }),
    [user, isLoading, authError, login, loginWithAuth0, logout, clearAuthError],
  );

  if (isLoading) {
    return <LoadingScreen />;
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
