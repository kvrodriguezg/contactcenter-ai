import type {
  AskQuestionRequest,
  AskQuestionResponse,
  ConversationDetailDto,
  ConversationDto,
} from '../types/chat';
import { apiGet, apiPost } from './client';

export async function askQuestion(request: AskQuestionRequest): Promise<AskQuestionResponse> {
  return apiPost<AskQuestionResponse>('/api/chat/ask', request);
}

export async function getConversations(): Promise<ConversationDto[]> {
  return apiGet<ConversationDto[]>('/api/chat/conversations');
}

export async function getConversationById(id: string): Promise<ConversationDetailDto> {
  return apiGet<ConversationDetailDto>(`/api/chat/conversations/${id}`);
}
