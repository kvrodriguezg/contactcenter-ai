import type {
  AssignTicketPayload,
  ChangeTicketStatusPayload,
  CreateTicketPayload,
  ListTicketsParams,
  ResolveTicketPayload,
  Ticket,
} from '../types/tickets';
import { apiGet, apiPost, apiPut } from './client';

function buildQuery(params?: ListTicketsParams): string {
  if (!params) {
    return '';
  }

  const search = new URLSearchParams();
  if (params.status) {
    search.set('status', params.status);
  }
  if (params.priority) {
    search.set('priority', params.priority);
  }

  const query = search.toString();
  return query ? `?${query}` : '';
}

export async function getTickets(params?: ListTicketsParams): Promise<Ticket[]> {
  return apiGet<Ticket[]>(`/api/tickets${buildQuery(params)}`);
}

export async function getTicketById(id: string): Promise<Ticket> {
  return apiGet<Ticket>(`/api/tickets/${id}`);
}

export async function createTicket(payload: CreateTicketPayload): Promise<Ticket> {
  return apiPost<Ticket>('/api/tickets', payload);
}

export async function assignTicket(id: string, payload: AssignTicketPayload): Promise<Ticket> {
  return apiPut<Ticket>(`/api/tickets/${id}/assign`, payload);
}

export async function changeTicketStatus(
  id: string,
  payload: ChangeTicketStatusPayload,
): Promise<Ticket> {
  return apiPut<Ticket>(`/api/tickets/${id}/status`, payload);
}

export async function resolveTicket(id: string, payload: ResolveTicketPayload): Promise<Ticket> {
  return apiPut<Ticket>(`/api/tickets/${id}/resolve`, payload);
}
