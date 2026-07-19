export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type TicketStatus = 'Pending' | 'InReview' | 'Resolved' | 'Closed';

export interface Ticket {
  id: string;
  companyId: string;
  companyName: string;
  createdByUserId: string;
  createdByEmail: string;
  createdByName?: string | null;
  conversationId?: string | null;
  subject: string;
  description: string;
  priority: TicketPriority | string;
  status: TicketStatus | string;
  assignedToUserId?: string | null;
  assignedToEmail?: string | null;
  assignedToName?: string | null;
  resolution?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  resolvedAt?: string | null;
}

export interface CreateTicketPayload {
  subject: string;
  description: string;
  priority: TicketPriority;
  conversationId?: string | null;
}

export interface AssignTicketPayload {
  assignedToUserId: string;
}

export interface ChangeTicketStatusPayload {
  status: TicketStatus;
}

export interface ResolveTicketPayload {
  resolution: string;
}

export interface ListTicketsParams {
  status?: TicketStatus | '';
  priority?: TicketPriority | '';
}
