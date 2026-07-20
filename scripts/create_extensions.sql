-- ContactCenterAI — extensiones PostgreSQL (Core DB)
-- Origen: deploy/docker/init-db.sql (montado en docker-entrypoint-initdb.d)
-- Requiere imagen con soporte pgvector (pgvector/pgvector:pg16).

CREATE EXTENSION IF NOT EXISTS vector;
