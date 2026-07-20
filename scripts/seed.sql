-- =============================================================================
-- ContactCenterAI — seed de desarrollo
-- =============================================================================
-- Equivalente lógico a ApplicationDbSeeder (solo Development en la app).
-- Credenciales locales (AUTH_PROVIDER=Local):
--   admin@contactcenterai.cl  / Admin123*  → SuperAdmin
--   agente@contactcenterai.cl / Agent123*  → Agent
--
-- Los PasswordHash fueron generados con Microsoft.AspNetCore.Identity.PasswordHasher
-- (mismo algoritmo que PasswordHasherService del proyecto).
-- =============================================================================

-- Ejecutar contra la base Core: contactcenterai

DO $$
DECLARE
    v_company_id uuid;
    v_admin_id   uuid := gen_random_uuid();
    v_agent_id   uuid := gen_random_uuid();
BEGIN
    SELECT "Id" INTO v_company_id
    FROM companies
    WHERE "Name" = 'Empresa Telecomunicaciones Simulada'
    LIMIT 1;

    IF v_company_id IS NULL THEN
        v_company_id := gen_random_uuid();
        INSERT INTO companies ("Id", "Name", "Status", "CreatedAt", "UpdatedAt")
        VALUES (v_company_id, 'Empresa Telecomunicaciones Simulada', 'Active', NOW() AT TIME ZONE 'utc', NULL);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM users WHERE "Email" = 'admin@contactcenterai.cl') THEN
        INSERT INTO users (
            "Id", "Email", "Name", "PasswordHash", "Role", "CompanyId",
            "IsActive", "AuthenticationProvider", "CreatedAt"
        ) VALUES (
            v_admin_id,
            'admin@contactcenterai.cl',
            'Administrador',
            'AQAAAAIAAYagAAAAELULisKP99CxUNccHfDVcCpDmim3hMlj0YL7e21smFc4aYSvD8us6R9me0geTTFrOQ==',
            'SuperAdmin',
            NULL,
            TRUE,
            'Local',
            NOW() AT TIME ZONE 'utc'
        );
    END IF;

    IF NOT EXISTS (SELECT 1 FROM users WHERE "Email" = 'agente@contactcenterai.cl') THEN
        INSERT INTO users (
            "Id", "Email", "Name", "PasswordHash", "Role", "CompanyId",
            "IsActive", "AuthenticationProvider", "CreatedAt"
        ) VALUES (
            v_agent_id,
            'agente@contactcenterai.cl',
            'Agente Demo',
            'AQAAAAIAAYagAAAAEIUNsqGJ5AaUSD84ZWvZLWP2+rv6rtwn7AxxKi/hchtm5mJSkwfONx+/5x1ruG+UYQ==',
            'Agent',
            v_company_id,
            TRUE,
            'Local',
            NOW() AT TIME ZONE 'utc'
        );
    END IF;
END $$;
