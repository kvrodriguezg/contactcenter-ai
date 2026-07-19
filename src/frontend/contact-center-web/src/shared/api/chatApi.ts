import type {
  AskQuestionRequest,
  AskQuestionResponse,
  ConversationDetailDto,
  ConversationDto,
} from '../types/chat';
import { getTokenProvider } from '../../features/auth/tokenProvider';

const CORE_API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080';
const CHAT_API_BASE_URL = import.meta.env.VITE_CHAT_API_BASE_URL ?? 'http://localhost:8081';
const CHAT_SERVICE_MODE = (import.meta.env.VITE_CHAT_SERVICE_MODE as string | undefined)?.trim() || 'Embedded';

export const isChatExternal =
  CHAT_SERVICE_MODE.toLowerCase() === 'external';

function getChatBaseUrl(): string {
  return isChatExternal ? CHAT_API_BASE_URL : CORE_API_BASE_URL;
}

async function chatRequest<T>(path: string, options: { method?: string; body?: unknown } = {}): Promise<T> {
  const headers: Record<string, string> = {
    Accept: 'application/json',
  };

  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  const token = await getTokenProvider().getToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(`${getChatBaseUrl()}${path}`, {
    method: options.method ?? (options.body !== undefined ? 'POST' : 'GET'),
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

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

export async function askQuestion(request: AskQuestionRequest): Promise<AskQuestionResponse> {
  return chatRequest<AskQuestionResponse>('/api/chat/ask', { body: request });
}

export async function getConversations(): Promise<ConversationDto[]> {
  return chatRequest<ConversationDto[]>('/api/chat/conversations');
}

export async function getConversationById(id: string): Promise<ConversationDetailDto> {
  return chatRequest<ConversationDetailDto>(`/api/chat/conversations/${id}`);
}
