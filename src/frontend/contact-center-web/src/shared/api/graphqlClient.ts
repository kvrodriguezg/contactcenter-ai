import { getTokenProvider } from '../../features/auth/tokenProvider';

function resolveGraphQlUrl(): string {
  const configured =
    (import.meta.env.VITE_GRAPHQL_URL as string | undefined)?.trim() ||
    (import.meta.env.VITE_BFF_GRAPHQL_URL as string | undefined)?.trim() ||
    '';

  if (configured) {
    return configured.replace(/\/+$/, '') || '/graphql';
  }

  return '/graphql';
}

const GRAPHQL_URL = resolveGraphQlUrl();

export type GraphQlResponse<T> = {
  data?: T;
  errors?: Array<{ message: string; extensions?: Record<string, unknown> }>;
};

export async function graphqlRequest<T>(
  query: string,
  variables?: Record<string, unknown>,
): Promise<T> {
  const headers: Record<string, string> = {
    Accept: 'application/json',
    'Content-Type': 'application/json',
  };

  const token = await getTokenProvider().getToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(GRAPHQL_URL, {
    method: 'POST',
    headers,
    body: JSON.stringify({ query, variables }),
  });

  if (response.status === 401) {
    getTokenProvider().clearToken();
    throw new Error('No autorizado');
  }

  const payload = (await response.json()) as GraphQlResponse<T>;
  if (payload.errors?.length) {
    throw new Error(payload.errors.map((e) => e.message).join(' '));
  }

  if (!payload.data) {
    throw new Error('Respuesta GraphQL vacía');
  }

  return payload.data;
}

export const BFF_GRAPHQL_URL = GRAPHQL_URL;
export { GRAPHQL_URL };
