# queryapi Database Schema

PostgreSQL database backing the Query API.

## Tables

### projects

Represents a registered data source with its schema context for LLM-powered query generation.

| Column | Type | Constraints | Description |
|---|---|---|---|
| id | UUID | PK | Project identifier |
| name | TEXT | NOT NULL | Display name |
| description | TEXT | nullable | Optional project description |
| schema_context_json | TEXT | nullable | JSON blob describing the target database schema for LLM prompts |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT now() | Creation timestamp |

### conversations

A user's query-building session against a project.

| Column | Type | Constraints | Description |
|---|---|---|---|
| id | UUID | PK | Conversation identifier |
| project_id | UUID | NOT NULL, FK → projects(id) | Parent project |
| user_id | TEXT | NOT NULL | Identifies the user who started the conversation |
| state | TEXT | NOT NULL, DEFAULT 'SchemaLoaded' | Workflow state machine value |
| current_spec_json | TEXT | nullable | JSON spec representing the current query being refined |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT now() | Creation timestamp |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT now() | Last modification timestamp |

### conversation_turns

Individual messages exchanged within a conversation (user prompts and assistant responses).

| Column | Type | Constraints | Description |
|---|---|---|---|
| id | UUID | PK | Turn identifier |
| conversation_id | UUID | NOT NULL, FK → conversations(id) | Parent conversation |
| role | TEXT | NOT NULL | Message author role (e.g. `user`, `assistant`) |
| content | TEXT | NOT NULL | Message body |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT now() | Creation timestamp |

### output_bundles

Compiled query outputs produced from a conversation, preserving the generated SQL/output alongside its explanation and spec.

| Column | Type | Constraints | Description |
|---|---|---|---|
| id | UUID | PK | Bundle identifier |
| conversation_id | UUID | NOT NULL, FK → conversations(id) | Parent conversation |
| raw_output | TEXT | NOT NULL | The compiled output (e.g. raw SQL) |
| explanation | TEXT | NOT NULL | Human-readable explanation of the output |
| spec_json | TEXT | NOT NULL | JSON spec the output was compiled from |
| compiler | TEXT | NOT NULL | Compiler used (e.g. `markdown`, `html`) |
| dialect | TEXT | NOT NULL | Target SQL dialect (e.g. `postgres`) |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT now() | Creation timestamp |

## Indexes

| Index | Table | Column(s) |
|---|---|---|
| ix_conversations_project_id | conversations | project_id |
| ix_conversation_turns_conversation_id | conversation_turns | conversation_id |
| ix_output_bundles_conversation_id | output_bundles | conversation_id |

## Relationships

```
projects 1──* conversations 1──* conversation_turns
                             1──* output_bundles
```

A **project** has many **conversations**. Each **conversation** has an ordered list of **conversation_turns** and zero or more **output_bundles** representing compiled results.
