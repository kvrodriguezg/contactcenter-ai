export type ChatSourceDto = {
  documentId: string;
  documentName?: string;
  originalFileName?: string;
  chunkId?: string;
  chunkIndex: number;
  contentPreview: string;
  similarity?: number;
  score?: number;
  pageNumber?: number | null;
};

export type AskQuestionRequest = {
  question: string;
  conversationId?: string;
  topK?: number;
  companyId?: string;
};

export type AskQuestionResponse = {
  answer: string;
  conversationId: string;
  sources: ChatSourceDto[];
  createdAt: string;
};

export type ConversationDto = {
  id: string;
  companyId: string;
  companyName?: string;
  userId?: string;
  externalUserId?: string;
  userEmail?: string;
  title: string;
  createdAt: string;
  updatedAt?: string | null;
};

export type ConversationMessageDto = {
  id: string;
  role: string;
  content: string;
  sources: ChatSourceDto[];
  createdAt: string;
};

export type ConversationDetailDto = {
  id: string;
  companyId: string;
  companyName?: string;
  userId?: string;
  externalUserId?: string;
  userEmail?: string;
  title: string;
  createdAt: string;
  updatedAt?: string | null;
  messages: ConversationMessageDto[];
};
