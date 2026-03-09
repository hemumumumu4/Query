# Query API Schema Context Design

## Goal

Create a SchemaContext JSON file describing the Query API's own database, enabling natural language queries about usage patterns and structural metadata (not content).

## Approach

Static JSON file at `docs/query-api-schema-context.json`, loadable via `PUT /projects/{id}/schema`.

## Tables (metadata only)

### projects
| Column | Type | Description |
|--------|------|-------------|
| id | uuid | Primary key |
| name | text | Project name |
| description | text | Optional project description |
| created_at | timestamptz | When the project was created |
| updated_at | timestamptz | Last modification time |
| deleted_at | timestamptz | Soft delete timestamp, null if active |

Excluded: `schema_context_json` (raw JSON content)

### conversations
| Column | Type | Description |
|--------|------|-------------|
| id | uuid | Primary key |
| project_id | uuid | FK to projects |
| user_id | text | User who started the conversation |
| state | text | Conversation state (SchemaLoaded, Disambiguation, SpecConfirmed, Done) |
| created_at | timestamptz | When the conversation started |
| updated_at | timestamptz | Last activity time |

Excluded: `current_spec_json` (raw JSON content)

### conversation_turns
| Column | Type | Description |
|--------|------|-------------|
| id | uuid | Primary key |
| conversation_id | uuid | FK to conversations |
| role | text | Message sender role (user or assistant) |
| created_at | timestamptz | When the turn was recorded |

Excluded: `content` (conversation content)

### output_bundles
| Column | Type | Description |
|--------|------|-------------|
| id | uuid | Primary key |
| conversation_id | uuid | FK to conversations |
| compiler | text | Compiler used (sql, markdown, html) |
| dialect | text | SQL dialect or output variant |
| created_at | timestamptz | When the output was generated |

Excluded: `raw_output`, `explanation`, `spec_json` (content)

## Relationships
- `conversations.project_id → projects.id`
- `conversation_turns.conversation_id → conversations.id`
- `output_bundles.conversation_id → conversations.id`

## Business Terms
- "active projects" → "projects where deleted_at is null"
- "deleted projects" → "projects where deleted_at is not null"
- "conversation length" → "number of turns in a conversation"
- "user activity" → "number of conversations started by a user"
- "completed conversations" → "conversations with state Done"
- "pending conversations" → "conversations not yet in Done state"

## Calculation Library
- `active_project_count` → `COUNT(*) WHERE deleted_at IS NULL` on projects
- `avg_turns_per_conversation` → average turn count per conversation
- `conversations_per_project` → `COUNT(*)` grouped by project_id
- `output_count_by_compiler` → `COUNT(*)` grouped by compiler
- `daily_conversation_count` → `COUNT(*)` grouped by date(created_at)

## Glossary
- "chat" → "conversation"
- "message" → "conversation_turn"
- "turn" → "conversation_turn"
- "result" → "output_bundle"
- "output" → "output_bundle"
- "project" → "projects"

## Excluded
- **Permission rules** — not applicable to Query API
- **Raw JSON content columns** — schema_context_json, current_spec_json, spec_json, raw_output
- **Conversation content** — content, explanation
