export type AuthProviderMode = 'Local' | 'Auth0';

const rawProvider = (import.meta.env.VITE_AUTH_PROVIDER as string | undefined)?.trim() || 'Local';

export const AUTH_PROVIDER: AuthProviderMode =
  rawProvider.toLowerCase() === 'auth0' ? 'Auth0' : 'Local';

export const isAuth0Mode = AUTH_PROVIDER === 'Auth0';

export const auth0Config = {
  domain: (import.meta.env.VITE_AUTH0_DOMAIN as string | undefined)?.trim() ?? '',
  clientId: (import.meta.env.VITE_AUTH0_CLIENT_ID as string | undefined)?.trim() ?? '',
  audience: (import.meta.env.VITE_AUTH0_AUDIENCE as string | undefined)?.trim()
    || 'https://contactcenterai-api',
  get redirectUri(): string {
    const configured = (import.meta.env.VITE_AUTH0_REDIRECT_URI as string | undefined)?.trim();
    if (configured) {
      return configured;
    }

    return typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173';
  },
};

export function isAuth0Configured(): boolean {
  return Boolean(auth0Config.domain && auth0Config.clientId);
}
