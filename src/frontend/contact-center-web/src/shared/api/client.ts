import { getTokenProvider } from '../../features/auth/tokenProvider';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080';

type RequestOptions = {
  skipAuth?: boolean;
  method?: string;
  body?: unknown;
};

let onUnauthorized: (() => void) | null = null;

export function setOnUnauthorized(handler: () => void) {
  onUnauthorized = handler;
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = {
    Accept: 'application/json',
  };

  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  if (!options.skipAuth) {
    const token = await getTokenProvider().getToken();
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? (options.body !== undefined ? 'POST' : 'GET'),
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  if (response.status === 401) {
    if (!options.skipAuth) {
      getTokenProvider().clearToken();
      onUnauthorized?.();
    }
    let message = 'No autorizado';
    try {
      const data = (await response.json()) as { message?: string };
      if (data.message) {
        message = data.message;
      }
    } catch {
      // Sin cuerpo JSON.
    }
    throw new Error(message);
  }

  if (!response.ok) {
    let message = `Error ${response.status}`;
    try {
      const data = (await response.json()) as { message?: string };
      if (data.message) {
        message = data.message;
      }
    } catch {
      // Sin cuerpo JSON.
    }
    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export function apiGet<T>(path: string): Promise<T> {
  return request<T>(path);
}

export function apiPost<T>(
  path: string,
  body: unknown,
  options?: { skipAuth?: boolean },
): Promise<T> {
  return request<T>(path, { body, skipAuth: options?.skipAuth });
}

export function apiPut<T>(path: string, body: unknown): Promise<T> {
  return request<T>(path, { body, method: 'PUT' });
}

async function parseErrorMessage(response: Response): Promise<string> {
  try {
    const data = (await response.json()) as {
      message?: string;
      errors?: Record<string, string[]>;
    };

    if (data.errors) {
      const messages = Object.values(data.errors).flat();
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }

    if (data.message) {
      return data.message;
    }
  } catch {
    // Sin cuerpo JSON.
  }

  return `Error ${response.status}`;
}

export async function apiPostFormData<T>(path: string, formData: FormData): Promise<T> {
  const headers: Record<string, string> = {
    Accept: 'application/json',
  };

  const token = await getTokenProvider().getToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'POST',
    headers,
    body: formData,
  });

  if (response.status === 401) {
    getTokenProvider().clearToken();
    onUnauthorized?.();
    throw new Error(await parseErrorMessage(response));
  }

  if (!response.ok) {
    throw new Error(await parseErrorMessage(response));
  }

  return response.json() as Promise<T>;
}

export { API_BASE_URL };
