-- Query Storage Schema (PostgreSQL)

CREATE TABLE IF NOT EXISTS projects (
    id              UUID PRIMARY KEY,
    name            TEXT NOT NULL,
    description     TEXT,
    schema_context_json TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS conversations (
    id              UUID PRIMARY KEY,
    project_id      UUID NOT NULL REFERENCES projects(id),
    user_id         TEXT NOT NULL,
    state           TEXT NOT NULL DEFAULT 'SchemaLoaded',
    current_spec_json TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS conversation_turns (
    id              UUID PRIMARY KEY,
    conversation_id UUID NOT NULL REFERENCES conversations(id),
    role            TEXT NOT NULL,
    content         TEXT NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS output_bundles (
    id              UUID PRIMARY KEY,
    conversation_id UUID NOT NULL REFERENCES conversations(id),
    raw_output      TEXT NOT NULL,
    explanation     TEXT NOT NULL,
    spec_json       TEXT NOT NULL,
    compiler        TEXT NOT NULL,
    dialect         TEXT NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_conversations_project_id ON conversations(project_id);
CREATE INDEX IF NOT EXISTS ix_conversation_turns_conversation_id ON conversation_turns(conversation_id);
CREATE INDEX IF NOT EXISTS ix_output_bundles_conversation_id ON output_bundles(conversation_id);
