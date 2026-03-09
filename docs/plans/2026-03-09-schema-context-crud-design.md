# Schema Context CRUD Design

## Goal

Replace the ingestion-coupled project creation with clean CRUD endpoints for projects and their schema contexts. Add soft-deletes on projects.

## Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/projects` | Create project (Name, Description only) |
| `GET` | `/projects` | List active projects |
| `GET` | `/projects/{id}` | Get project with schema context |
| `PUT` | `/projects/{id}` | Update project Name/Description |
| `DELETE` | `/projects/{id}` | Soft-delete project (sets deleted_at) |
| `PUT` | `/projects/{id}/schema` | Replace entire schema context |
| `PATCH` | `/projects/{id}/schema` | Merge specific keys into schema context |
| `GET` | `/projects/{id}/schema` | Get schema context only |

## PATCH Semantics

Request body is a partial `SchemaContext` JSON. Only keys present in the request replace existing values; absent keys are untouched.

```json
{ "Tables": [...] }
```

Replaces `Tables`, leaves `Relationships`, `BusinessTerms`, `Glossary`, `CalculationLibrary`, `PermissionRules` unchanged.

## Soft-Delete

- Add `deleted_at TIMESTAMPTZ` column to `projects` (nullable, null = active)
- All queries filter `WHERE deleted_at IS NULL`
- `DELETE /projects/{id}` sets `deleted_at = now()`

## DB Schema Changes

- Add `deleted_at TIMESTAMPTZ` to `projects`
- Add `updated_at TIMESTAMPTZ NOT NULL DEFAULT now()` to `projects`

## What Changes

- `CreateProjectEndpoint` simplified: accepts only Name/Description, no ingestion
- `SchemaContextBuilder` removed from `CreateProjectEndpoint` DI
- New endpoints: ListProjects, UpdateProject, DeleteProject, PutSchema, PatchSchema, GetSchema
- `IProjectRepository` expanded with new methods
- `ProjectRecord` gets `DeletedAt` and `UpdatedAt` fields

## What Stays

- Ingestion adapters (DDLAdapter, MarkdownAdapter, etc.) remain in codebase, decoupled from project creation
- SchemaContext model unchanged
- Conversation endpoints unchanged
