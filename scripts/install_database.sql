-- =============================================================================
-- ContactCenterAI — instalación de bases de datos (referencia de entrega)
-- =============================================================================
-- En runtime, Entity Framework aplica migraciones al iniciar (Development).
-- Este script documenta el esquema resultante para instalación manual.
--
-- Orden recomendado:
--   1) Crear bases (sección 0)
--   2) Conectar a contactcenterai      → sección CORE
--   3) Conectar a contactcenterai_chat → sección CHAT
--   4) (Opcional) scripts/seed.sql sobre contactcenterai
-- =============================================================================

-- ---------------------------------------------------------------------------
-- 0) Crear bases (conectado a la BD "postgres")
-- ---------------------------------------------------------------------------
CREATE DATABASE contactcenterai;
CREATE DATABASE contactcenterai_chat;

-- =============================================================================
-- SECCIÓN CORE — base: contactcenterai
-- Requiere imagen/extensión pgvector (pgvector/pgvector:pg16)
-- =============================================================================

CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId"    character varying(150) NOT NULL,
    "ProductVersion" character varying(32)  NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

CREATE TABLE IF NOT EXISTS companies (
    "Id"        uuid           NOT NULL,
    "Name"      varchar(200)   NOT NULL,
    "Status"    varchar(50)    NOT NULL,
    "CreatedAt" timestamptz    NOT NULL,
    "UpdatedAt" timestamptz    NULL,
    CONSTRAINT "PK_companies" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_companies_Name" ON companies ("Name");

CREATE TABLE IF NOT EXISTS users (
    "Id"                     uuid           NOT NULL,
    "Email"                  varchar(256)   NOT NULL,
    "Name"                   varchar(200)   NULL,
    "PasswordHash"           varchar(500)   NOT NULL,
    "Role"                   varchar(50)    NOT NULL,
    "CompanyId"              uuid           NULL,
    "IsActive"               boolean        NOT NULL,
    "ExternalSubject"        varchar(256)   NULL,
    "AuthenticationProvider" varchar(50)    NOT NULL DEFAULT 'Local',
    "LastLoginAt"            timestamptz    NULL,
    "CreatedAt"              timestamptz    NOT NULL,
    "UpdatedAt"              timestamptz    NULL,
    CONSTRAINT "PK_users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_users_companies_CompanyId"
        FOREIGN KEY ("CompanyId") REFERENCES companies ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_Email" ON users ("Email");
CREATE INDEX IF NOT EXISTS "IX_users_CompanyId" ON users ("CompanyId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_ExternalSubject"
    ON users ("ExternalSubject") WHERE "ExternalSubject" IS NOT NULL;

CREATE TABLE IF NOT EXISTS refresh_tokens (
    "Id"        uuid           NOT NULL,
    "Token"     varchar(500)   NOT NULL,
    "UserId"    uuid           NOT NULL,
    "ExpiresAt" timestamptz    NOT NULL,
    "RevokedAt" timestamptz    NULL,
    CONSTRAINT "PK_refresh_tokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_refresh_tokens_users_UserId"
        FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_refresh_tokens_Token" ON refresh_tokens ("Token");
CREATE INDEX IF NOT EXISTS "IX_refresh_tokens_UserId" ON refresh_tokens ("UserId");

CREATE TABLE IF NOT EXISTS documents (
    "Id"               uuid           NOT NULL,
    "CompanyId"        uuid           NOT NULL,
    "UploadedByUserId" uuid           NOT NULL,
    "FileName"         varchar(255)   NOT NULL,
    "OriginalFileName" varchar(255)   NOT NULL,
    "ContentType"      varchar(100)   NOT NULL,
    "SizeBytes"        bigint         NOT NULL,
    "StoragePath"      varchar(500)   NOT NULL,
    "Status"           varchar(50)    NOT NULL,
    "ErrorMessage"     varchar(2000)  NULL,
    "ProcessedAt"      timestamptz    NULL,
    "CreatedAt"        timestamptz    NOT NULL,
    "UpdatedAt"        timestamptz    NULL,
    CONSTRAINT "PK_documents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_documents_companies_CompanyId"
        FOREIGN KEY ("CompanyId") REFERENCES companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_documents_users_UploadedByUserId"
        FOREIGN KEY ("UploadedByUserId") REFERENCES users ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_documents_CompanyId" ON documents ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_documents_CreatedAt" ON documents ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_documents_Status" ON documents ("Status");
CREATE INDEX IF NOT EXISTS "IX_documents_UploadedByUserId" ON documents ("UploadedByUserId");

CREATE TABLE IF NOT EXISTS document_chunks (
    "Id"             uuid           NOT NULL,
    "DocumentId"     uuid           NOT NULL,
    "ChunkIndex"     integer        NOT NULL,
    "Content"        text           NOT NULL,
    "Embedding"      vector(1536)   NULL,
    "EmbeddingModel" varchar(100)   NULL,
    "EmbeddedAt"     timestamptz    NULL,
    "CreatedAt"      timestamptz    NOT NULL,
    CONSTRAINT "PK_document_chunks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_document_chunks_documents_DocumentId"
        FOREIGN KEY ("DocumentId") REFERENCES documents ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_document_chunks_DocumentId" ON document_chunks ("DocumentId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_document_chunks_DocumentId_ChunkIndex"
    ON document_chunks ("DocumentId", "ChunkIndex");

-- Conversaciones en Core (modo CHAT_SERVICE_MODE=Embedded / compatibilidad)
CREATE TABLE IF NOT EXISTS conversations (
    "Id"        uuid           NOT NULL,
    "CompanyId" uuid           NOT NULL,
    "UserId"    uuid           NOT NULL,
    "Title"     varchar(200)   NOT NULL,
    "CreatedAt" timestamptz    NOT NULL,
    "UpdatedAt" timestamptz    NULL,
    CONSTRAINT "PK_conversations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_conversations_companies_CompanyId"
        FOREIGN KEY ("CompanyId") REFERENCES companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_conversations_users_UserId"
        FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_conversations_CompanyId" ON conversations ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_conversations_CreatedAt" ON conversations ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_conversations_UserId" ON conversations ("UserId");

CREATE TABLE IF NOT EXISTS conversation_messages (
    "Id"             uuid           NOT NULL,
    "ConversationId" uuid           NOT NULL,
    "Role"           varchar(20)    NOT NULL,
    "Content"        text           NOT NULL,
    "SourcesJson"    text           NULL,
    "CreatedAt"      timestamptz    NOT NULL,
    CONSTRAINT "PK_conversation_messages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_conversation_messages_conversations_ConversationId"
        FOREIGN KEY ("ConversationId") REFERENCES conversations ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_conversation_messages_ConversationId"
    ON conversation_messages ("ConversationId");

CREATE TABLE IF NOT EXISTS tickets (
    "Id"                    uuid           NOT NULL,
    "CompanyId"             uuid           NOT NULL,
    "CreatedByUserId"       uuid           NOT NULL,
    "AssignedToUserId"      uuid           NULL,
    "ConversationId"        uuid           NULL,
    "Subject"               varchar(200)   NOT NULL,
    "Description"           varchar(4000)  NOT NULL,
    "Priority"              varchar(50)    NOT NULL,
    "Status"                varchar(50)    NOT NULL,
    "Resolution"            varchar(4000)  NULL,
    "ResolvedAt"            timestamptz    NULL,
    "EscalationStatus"      varchar(100)   NULL,
    "EscalationProcessedAt" timestamptz    NULL,
    "CreatedAt"             timestamptz    NOT NULL,
    "UpdatedAt"             timestamptz    NULL,
    CONSTRAINT "PK_tickets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_tickets_companies_CompanyId"
        FOREIGN KEY ("CompanyId") REFERENCES companies ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_tickets_users_CreatedByUserId"
        FOREIGN KEY ("CreatedByUserId") REFERENCES users ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_tickets_users_AssignedToUserId"
        FOREIGN KEY ("AssignedToUserId") REFERENCES users ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_tickets_conversations_ConversationId"
        FOREIGN KEY ("ConversationId") REFERENCES conversations ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_tickets_AssignedToUserId" ON tickets ("AssignedToUserId");
CREATE INDEX IF NOT EXISTS "IX_tickets_CompanyId" ON tickets ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_tickets_ConversationId" ON tickets ("ConversationId");
CREATE INDEX IF NOT EXISTS "IX_tickets_CreatedAt" ON tickets ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_tickets_CreatedByUserId" ON tickets ("CreatedByUserId");
CREATE INDEX IF NOT EXISTS "IX_tickets_Priority" ON tickets ("Priority");
CREATE INDEX IF NOT EXISTS "IX_tickets_Status" ON tickets ("Status");

-- =============================================================================
-- SECCIÓN CHAT — base: contactcenterai_chat
-- Conectar a contactcenterai_chat ANTES de ejecutar esta sección.
-- (Sin pgvector; referencias lógicas a CompanyId / ExternalUserId)
-- =============================================================================

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId"    character varying(150) NOT NULL,
    "ProductVersion" character varying(32)  NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

CREATE TABLE IF NOT EXISTS conversations (
    "Id"             uuid           NOT NULL,
    "ExternalUserId" uuid           NOT NULL,
    "UserEmail"      varchar(256)   NOT NULL,
    "CompanyId"      uuid           NOT NULL,
    "Title"          varchar(200)   NOT NULL,
    "CreatedAt"      timestamptz    NOT NULL,
    "UpdatedAt"      timestamptz    NULL,
    CONSTRAINT "PK_conversations" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_conversations_CompanyId" ON conversations ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_conversations_ExternalUserId" ON conversations ("ExternalUserId");
CREATE INDEX IF NOT EXISTS "IX_conversations_CompanyId_ExternalUserId"
    ON conversations ("CompanyId", "ExternalUserId");

CREATE TABLE IF NOT EXISTS conversation_messages (
    "Id"             uuid           NOT NULL,
    "ConversationId" uuid           NOT NULL,
    "Role"           varchar(50)    NOT NULL,
    "Content"        text           NOT NULL,
    "SourcesJson"    text           NULL,
    "CreatedAt"      timestamptz    NOT NULL,
    CONSTRAINT "PK_conversation_messages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_conversation_messages_conversations_ConversationId"
        FOREIGN KEY ("ConversationId") REFERENCES conversations ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_conversation_messages_ConversationId"
    ON conversation_messages ("ConversationId");
