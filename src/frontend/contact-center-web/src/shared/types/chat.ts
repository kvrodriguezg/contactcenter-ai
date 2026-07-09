export type ChatSourceDto = {
  documentId: string;
  originalFileName: string;
  chunkIndex: number;
  contentPreview: string;
  score: number;
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
  companyName: string;
  userId: string;
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
  companyName: string;
  userId: string;
  title: string;
  createdAt: string;
  updatedAt?: string | null;
  messages: ConversationMessageDto[];
};
