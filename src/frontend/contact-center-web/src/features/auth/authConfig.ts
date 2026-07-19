export type AuthProviderMode = 'Local' | 'Auth0';

function readViteEnv(name: string): string {
  const value = (import.meta.env[name] as string | undefined) ?? '';
  return value.trim().replace(/^['"]|['"]$/g, '');
}

/** Auth0 exige hostname sin esquema (sin https://). */
function normalizeAuth0Domain(raw: string): string {
  return raw
    .replace(/^https?:\/\//i, '')
    .replace(/\/+$/, '')
    .trim();
}

const rawProvider = readViteEnv('VITE_AUTH_PROVIDER') || 'Local';

export const AUTH_PROVIDER: AuthProviderMode =
  rawProvider.toLowerCase() === 'auth0' ? 'Auth0' : 'Local';

export const isAuth0Mode = AUTH_PROVIDER === 'Auth0';

export const auth0Config = {
  domain: normalizeAuth0Domain(readViteEnv('VITE_AUTH0_DOMAIN')),
  clientId: readViteEnv('VITE_AUTH0_CLIENT_ID'),
  audience: readViteEnv('VITE_AUTH0_AUDIENCE') || 'https://contactcenterai-api',
  get redirectUri(): string {
    const configured = readViteEnv('VITE_AUTH0_REDIRECT_URI').replace(/\/+$/, '');
    if (configured) {
      return configured;
    }

    return typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173';
  },
};

export function isAuth0Configured(): boolean {
  return Boolean(auth0Config.domain && auth0Config.clientId);
}

/** Mensaje seguro: solo nombre y descripción, sin tokens ni identificadores. */
export function formatAuth0Error(error: unknown): string {
  if (!error) {
    return 'Error Auth0 desconocido.';
  }

  let name = 'Error';
  let message = '';

  if (error instanceof Error) {
    name = error.name?.trim() || 'Error';
    message = sanitizeAuth0Message(error.message);
  } else if (typeof error === 'object') {
    const record = error as { error?: string; error_description?: string; message?: string };
    name = sanitizeAuth0Message(record.error || record.message || 'Error');
    message = sanitizeAuth0Message(record.error_description || '');
  } else {
    return 'Error Auth0 desconocido.';
  }

  const combined = message ? `${name}: ${message}` : name;

  if (/not authorized to access resource server/i.test(combined)) {
    return (
      `${combined} ` +
      'El Application SPA no está autorizado para el API (audience) en Auth0 Dashboard. ' +
      'Autorice la aplicación SPA en APIs → su API → Machine to Machine / Applications.'
    );
  }

  return combined;
}

function sanitizeAuth0Message(value: string | undefined): string {
  if (!value) {
    return '';
  }

  return value
    .replace(/Bearer\s+[A-Za-z0-9._~+/-]+=*/gi, '[redacted]')
    .replace(/\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9._~+/-]+=*/g, '[redacted]')
    .replace(/[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}/g, '[email]')
    .replace(/Client\s+"[^"]+"/gi, 'Client "[redacted]"')
    .replace(/client_id=[^&\s]+/gi, 'client_id=[redacted]')
    .replace(/https?:\/\/[^\s"]+/gi, '[url]')
    .slice(0, 300);
}
