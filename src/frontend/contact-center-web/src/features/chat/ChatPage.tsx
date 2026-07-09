import AddIcon from '@mui/icons-material/Add';
import SendIcon from '@mui/icons-material/Send';
import SmartToyIcon from '@mui/icons-material/SmartToy';
import PersonIcon from '@mui/icons-material/Person';
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  IconButton,
  List,
  ListItemButton,
  ListItemText,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import { useCallback, useEffect, useRef, useState } from 'react';
import { askQuestion, getConversationById, getConversations } from '../../shared/api/chatApi';
import type { ChatSourceDto, ConversationDto, ConversationMessageDto } from '../../shared/types/chat';

type DisplayMessage = {
  id: string;
  role: 'User' | 'Assistant';
  content: string;
  sources: ChatSourceDto[];
  createdAt: string;
};

function mapMessage(message: ConversationMessageDto): DisplayMessage {
  return {
    id: message.id,
    role: message.role === 'Assistant' ? 'Assistant' : 'User',
    content: message.content,
    sources: message.sources ?? [],
    createdAt: message.createdAt,
  };
}

function formatDate(value: string): string {
  return new Date(value).toLocaleString('es-CL');
}

function formatScore(score: number): string {
  return `${(score * 100).toFixed(1)}%`;
}

function SourcesPanel({ sources }: { sources: ChatSourceDto[] }) {
  if (sources.length === 0) {
    return null;
  }

  return (
    <Box sx={{ mt: 1.5 }}>
      <Typography variant="caption" color="text.secondary" sx={{ mb: 0.5, display: 'block' }}>
        Fuentes consultadas
      </Typography>
      {sources.map((source) => (
        <Accordion
          key={`${source.documentId}-${source.chunkIndex}`}
          disableGutters
          elevation={0}
          sx={{
            border: '1px solid',
            borderColor: 'divider',
            '&:before': { display: 'none' },
            mb: 0.5,
          }}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Stack direction="row" spacing={1} alignItems="center" sx={{ flexWrap: 'wrap' }}>
              <Typography variant="body2" fontWeight={500}>
                {source.originalFileName}
              </Typography>
              <Chip label={`Chunk ${source.chunkIndex}`} size="small" variant="outlined" />
              <Chip label={formatScore(source.score)} size="small" color="primary" variant="outlined" />
            </Stack>
          </AccordionSummary>
          <AccordionDetails>
            <Typography variant="body2" color="text.secondary">
              {source.contentPreview}
            </Typography>
          </AccordionDetails>
        </Accordion>
      ))}
    </Box>
  );
}

function MessageBubble({ message }: { message: DisplayMessage }) {
  const isUser = message.role === 'User';

  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: isUser ? 'flex-end' : 'flex-start',
        mb: 2,
      }}
    >
      <Paper
        elevation={0}
        sx={{
          p: 2,
          maxWidth: '85%',
          bgcolor: isUser ? 'primary.main' : 'grey.100',
          color: isUser ? 'primary.contrastText' : 'text.primary',
        }}
      >
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
          {isUser ? <PersonIcon fontSize="small" /> : <SmartToyIcon fontSize="small" />}
          <Typography variant="caption" sx={{ opacity: 0.9 }}>
            {isUser ? 'Tú' : 'Asistente'} · {formatDate(message.createdAt)}
          </Typography>
        </Stack>
        <Typography variant="body1" sx={{ whiteSpace: 'pre-wrap' }}>
          {message.content}
        </Typography>
        {!isUser && <SourcesPanel sources={message.sources} />}
      </Paper>
    </Box>
  );
}

export function ChatPage() {
  const [conversations, setConversations] = useState<ConversationDto[]>([]);
  const [selectedConversationId, setSelectedConversationId] = useState<string | null>(null);
  const [messages, setMessages] = useState<DisplayMessage[]>([]);
  const [question, setQuestion] = useState('');
  const [isLoadingHistory, setIsLoadingHistory] = useState(true);
  const [isLoadingConversation, setIsLoadingConversation] = useState(false);
  const [isSending, setIsSending] = useState(false);
  const [error, setError] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const loadConversations = useCallback(async () => {
    try {
      const data = await getConversations();
      setConversations(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible cargar el historial';
      setError(message);
    }
  }, []);

  useEffect(() => {
    const load = async () => {
      setIsLoadingHistory(true);
      setError('');
      await loadConversations();
      setIsLoadingHistory(false);
    };

    void load();
  }, [loadConversations]);

  useEffect(() => {
    scrollToBottom();
  }, [messages, isSending]);

  const handleSelectConversation = async (conversationId: string) => {
    setSelectedConversationId(conversationId);
    setIsLoadingConversation(true);
    setError('');

    try {
      const detail = await getConversationById(conversationId);
      setMessages(detail.messages.map(mapMessage));
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible cargar la conversación';
      setError(message);
      setMessages([]);
    } finally {
      setIsLoadingConversation(false);
    }
  };

  const handleNewConversation = () => {
    setSelectedConversationId(null);
    setMessages([]);
    setQuestion('');
    setError('');
  };

  const handleSend = async () => {
    const trimmedQuestion = question.trim();

    if (!trimmedQuestion || isSending) {
      return;
    }

    setIsSending(true);
    setError('');

    const userMessage: DisplayMessage = {
      id: `temp-user-${Date.now()}`,
      role: 'User',
      content: trimmedQuestion,
      sources: [],
      createdAt: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setQuestion('');

    try {
      const response = await askQuestion({
        question: trimmedQuestion,
        conversationId: selectedConversationId ?? undefined,
        topK: 5,
      });

      const assistantMessage: DisplayMessage = {
        id: `temp-assistant-${Date.now()}`,
        role: 'Assistant',
        content: response.answer,
        sources: response.sources,
        createdAt: response.createdAt,
      };

      setMessages((prev) => [...prev, assistantMessage]);
      setSelectedConversationId(response.conversationId);
      await loadConversations();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'No fue posible enviar la pregunta';
      setError(message);
    } finally {
      setIsSending(false);
    }
  };

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      void handleSend();
    }
  };

  const showEmptyState = !selectedConversationId && messages.length === 0 && !isLoadingConversation;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Chat inteligente
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Consulta los documentos procesados mediante IA.
      </Typography>

      <Box
        sx={{
          display: 'flex',
          flexDirection: { xs: 'column', md: 'row' },
          gap: 2,
          height: { md: 'calc(100vh - 220px)' },
          minHeight: 480,
        }}
      >
        <Paper
          variant="outlined"
          sx={{
            width: { xs: '100%', md: 300 },
            flexShrink: 0,
            display: 'flex',
            flexDirection: 'column',
            overflow: 'hidden',
          }}
        >
          <Box sx={{ p: 2, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <Typography variant="subtitle1" fontWeight={600}>
              Historial
            </Typography>
            <Button size="small" startIcon={<AddIcon />} onClick={handleNewConversation}>
              Nueva
            </Button>
          </Box>
          <Divider />
          {isLoadingHistory ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <CircularProgress size={28} />
            </Box>
          ) : (
            <List sx={{ overflow: 'auto', flexGrow: 1, py: 0 }}>
              {conversations.length === 0 ? (
                <ListItemText
                  sx={{ px: 2, py: 2 }}
                  primary="Sin conversaciones"
                  secondary="Inicia una nueva consulta"
                />
              ) : (
                conversations.map((conversation) => (
                  <ListItemButton
                    key={conversation.id}
                    selected={conversation.id === selectedConversationId}
                    onClick={() => void handleSelectConversation(conversation.id)}
                  >
                    <ListItemText
                      primary={conversation.title}
                      secondary={formatDate(conversation.updatedAt ?? conversation.createdAt)}
                      primaryTypographyProps={{ noWrap: true }}
                    />
                  </ListItemButton>
                ))
              )}
            </List>
          )}
        </Paper>

        <Paper
          variant="outlined"
          sx={{
            flexGrow: 1,
            display: 'flex',
            flexDirection: 'column',
            overflow: 'hidden',
          }}
        >
          <Box sx={{ flexGrow: 1, overflow: 'auto', p: 2 }}>
            {error && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {error}
              </Alert>
            )}

            {isLoadingConversation ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
                <CircularProgress />
              </Box>
            ) : showEmptyState ? (
              <Box
                sx={{
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  justifyContent: 'center',
                  height: '100%',
                  minHeight: 240,
                  textAlign: 'center',
                  px: 2,
                }}
              >
                <SmartToyIcon sx={{ fontSize: 48, color: 'text.disabled', mb: 2 }} />
                <Typography variant="h6" gutterBottom>
                  Inicia una conversación
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Escribe una pregunta sobre los documentos procesados. El asistente responderá
                  usando el contenido indexado y mostrará las fuentes consultadas.
                </Typography>
              </Box>
            ) : (
              <>
                {messages.map((message) => (
                  <MessageBubble key={message.id} message={message} />
                ))}
                {isSending && (
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                    <CircularProgress size={20} />
                    <Typography variant="body2" color="text.secondary">
                      Generando respuesta...
                    </Typography>
                  </Box>
                )}
                <div ref={messagesEndRef} />
              </>
            )}
          </Box>

          <Divider />
          <Box sx={{ p: 2, display: 'flex', gap: 1, alignItems: 'flex-end' }}>
            <TextField
              fullWidth
              multiline
              maxRows={4}
              placeholder="Escribe tu pregunta..."
              value={question}
              onChange={(e) => setQuestion(e.target.value)}
              onKeyDown={handleKeyDown}
              disabled={isSending}
              size="small"
            />
            <IconButton
              color="primary"
              onClick={() => void handleSend()}
              disabled={isSending || question.trim().length === 0}
              sx={{ mb: 0.5 }}
            >
              {isSending ? <CircularProgress size={24} /> : <SendIcon />}
            </IconButton>
          </Box>
        </Paper>
      </Box>
    </Box>
  );
}
