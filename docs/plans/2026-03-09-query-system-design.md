# Query System Design
**Date:** 2026-03-09
**Status:** Approved

---

## Overview

A multi-turn conversational query system that translates business language into deterministic, compiled outputs (initially SQL). Non-technical users (business analysts, financial analysts) describe what they want in plain language. The system asks targeted clarifying questions, builds a canonical QuerySpec, enforces row-level security, and compiles the spec to SQL with a human-readable explanation.

The LLM is used **only** for intent extraction (temperature=0, structured output). Everything downstream is deterministic C# code.

---

## 1. Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    INGESTION LAYER                       │
│   DDL files │ Markdown docs │ permissions.yaml          │
│   CodebaseAdapter (future) │ LiveDBAdapter (future)     │
│                 └── SchemaContext ──────────────────────┤
├─────────────────────────────────────────────────────────┤
│                  CONVERSATION ENGINE                     │
│   Multi-turn chat → structured intent extraction        │
│   State machine │ Disambiguation │ Formula detection    │
│                 └── QuerySpec ──────────────────────────┤
├─────────────────────────────────────────────────────────┤
│                  COMPILER REGISTRY                       │
│   SQLCompiler │ MarkdownCompiler │ HTMLCompiler │ ...   │
│   RLS filters injected unconditionally by compiler      │
├─────────────────────────────────────────────────────────┤
│                   OUTPUT BUNDLE                          │
│   raw output │ plain-English explanation │ QuerySpec    │
└─────────────────────────────────────────────────────────┘
```

---

## 2. SchemaContext & Ingestion Layer

The `SchemaContext` is built once per project and referenced throughout every conversation. Sources are loaded via a pluggable **adapter registry** — adding a new source type requires implementing one adapter interface.

### Adapters

| Adapter | Input | Extracts |
|---|---|---|
| `DDLAdapter` | `.sql` DDL files | Tables, columns, types, PKs, FKs, indexes |
| `MarkdownAdapter` | `.md` documentation | Business term → table/column mappings, rules, descriptions |
| `PermissionAdapter` | `permissions.yaml` | Admin-defined RLS rules per table, parameterised by `user_id` |
| `CodebaseAdapter` *(future)* | C# / source files | ORM models, query patterns, domain logic |
| `LiveDBAdapter` *(future)* | Live DB connection | Live schema introspection, sample values |

### SchemaContext Shape

```
SchemaContext
  tables[]
    name, description
    columns[]
      name, type, description, business_aliases[]
  relationships[]       ← inferred from FK constraints + markdown docs
  business_terms{}      ← "churn" → definition + table hint
  calculation_library{} ← admin-defined named calculations
  glossary{}            ← domain-specific language mappings
```

### Calculation Library (admin-defined, reusable)

Admins define named calculations alongside the schema:

```yaml
# calculations.yaml
calculations:
  gross_margin:
    expression: "(SUM(revenue) - SUM(cost)) / SUM(revenue)"
    description: "Gross profit as a percentage of revenue"
    applies_to: [orders]
  ltv:
    expression: "SUM(revenue) FILTER (WHERE status = 'completed')"
    description: "Lifetime value of a customer"
```

These are resolved silently during conversation without asking the analyst.

---

## 3. Security & Permission Layer

### SSO Token Ingestion

Every conversation session is initiated with an SSO token. The system extracts the `user_id` claim from the token and constructs a `PermissionContext`.

```
SSO Token → extract user_id claim → PermissionContext { user_id, resolved_rules[] }
```

### Admin-Defined RLS Rules

```yaml
# permissions.yaml (alongside DDL)
rules:
  - table: orders
    filter: "region_id IN (SELECT region_id FROM user_regions WHERE user_id = :user_id)"
  - table: salaries
    filter: "department_id = (SELECT department_id FROM employees WHERE user_id = :user_id)"
```

Rules are schema-specific but always parameterised by `user_id`. The permission structure is extensible — each schema defines its own rules; the `user_id` claim is the universal binding point.

### Enforcement Guarantee

Permission filters are injected by the **SQLCompiler**, not the conversation engine. The LLM never sees them and cannot be prompted to bypass them. Every generated query has RLS filters unconditionally AND-appended to the WHERE clause.

---

## 4. QuerySpec (Canonical Intermediate Representation)

The QuerySpec is the structured output of the conversation and the input to every compiler. It is schema-agnostic JSON, fully serializable, versioned, and stored alongside every output for auditability and replay.

```json
{
  "version": "1.0",
  "intent": "aggregation",
  "output_format": "sql",
  "entities": [
    { "table": "orders", "alias": "o" }
  ],
  "joins": [
    { "table": "customers", "alias": "c", "on": "o.customer_id = c.id" }
  ],
  "measures": [
    { "expression": "SUM(o.revenue)", "alias": "total_revenue" }
  ],
  "dimensions": [
    { "expression": "c.region", "alias": "region" }
  ],
  "filters": [
    { "expression": "o.status", "operator": "=", "value": "completed" }
  ],
  "time_range": {
    "column": "o.created_at", "from": "2024-01-01", "to": "2024-12-31"
  },
  "calculations": [
    {
      "name": "net_revenue",
      "type": "derived",
      "expression": "SUM(o.revenue) - SUM(r.amount)",
      "formula_source": { "handler": "infix", "raw": "(revenue - refunds)" },
      "requires_join": { "table": "refunds", "alias": "r", "on": "o.id = r.order_id" },
      "filter": "r.created_at <= o.created_at + INTERVAL '30 days'"
    }
  ],
  "ctes": [
    {
      "name": "refund_window",
      "definition": "SELECT order_id, SUM(amount) AS refund_total FROM refunds WHERE ..."
    }
  ],
  "order_by": [{ "expression": "total_revenue", "direction": "DESC" }],
  "limit": null,
  "permission_slot": "__INJECTED_BY_COMPILER__"
}
```

**Key invariants:**
- `filters` never contains permission rules
- `formula_source` preserves the original input for auditability
- `resolved_expression` is what compilers consume
- `permission_slot` is a marker — compilers replace it, never the LLM

---

## 5. Formula Handler Registry

A pluggable registry of `IFormulaHandler` implementations. Each handler recognises a formula syntax and compiles it to a `FormulaAST`, which all compilers consume.

### Pipeline

```
Input detected as formula
        ↓
FormulaHandlerRegistry.Detect(input) → selects handler
        ↓
handler.Parse(input, schemaContext) → FormulaAST
        ↓
FormulaAST.Resolve(schemaContext) → SQL expression fragment
        ↓
Inserted into QuerySpec calculations[] or filters[]
```

### Built-in Handlers

| Handler | Input example | Output |
|---|---|---|
| `PlainTextHandler` | `"revenue minus cost divided by revenue"` | `(revenue - cost) / revenue` |
| `InfixHandler` | `"(revenue - cost) / revenue * 100"` | `(revenue - cost) / revenue * 100` |
| `LaTeXHandler` | `\frac{r - c}{r} \times 100` | `(r - c) / r * 100` |
| `ExcelHandler` *(future)* | `=((B2-C2)/B2)*100` | `(revenue - cost) / revenue * 100` |

All handlers are implemented using **Pidgin** (C# parser combinators). The `FormulaAST` is the shared intermediate — adding a new formula syntax means one new `IFormulaHandler` class, nothing else changes.

---

## 6. Conversation Engine

The only layer that uses the LLM. All calls are at **temperature=0** with structured JSON output.

### State Machine

```
INIT → SCHEMA_LOADED → INTENT_CAPTURE → DISAMBIGUATION → SPEC_CONFIRMED → DONE
```

| State | What happens |
|---|---|
| `SCHEMA_LOADED` | SchemaContext + PermissionContext bound to session |
| `INTENT_CAPTURE` | LLM extracts initial intent from analyst message into partial QuerySpec |
| `DISAMBIGUATION` | System detects gaps, asks one targeted question at a time |
| `SPEC_CONFIRMED` | Analyst confirms plain-English spec summary before compilation |
| `DONE` | Compiler invoked, OutputBundle returned |

### Disambiguation Logic

Before asking the analyst anything, the engine checks in order:
1. Is the term in the calculation library? → resolve silently
2. Is a formula pattern detected? → route to FormulaHandlerRegistry
3. Is the column mapping ambiguous (multiple candidates)? → ask
4. Is a time range implied but not specified? → ask
5. Is an aggregation implied but the grain is unclear? → ask

### Determinism Enforcement

1. Temperature=0 on all LLM calls
2. Structured prompt templates per state (version-controlled in source)
3. JSON schema validation on every LLM response via FluentValidation — non-conforming responses trigger a corrective retry prompt (max 3 attempts), then surface a user-facing error
4. All formula resolution done by FormulaHandlers (C# code, not LLM)

### Spec Confirmation (shown to analyst)

> *"Here's what I understood: Show total net revenue (revenue minus refunds within 30 days of order date) grouped by customer region, for completed orders in 2024, sorted highest first. Is that right?"*

This summary is generated deterministically from the QuerySpec using a template renderer — not an LLM call.

---

## 7. Compiler Registry

Deterministic C# only — no LLM involvement.

### Interface

```csharp
interface ICompiler
{
    OutputBundle Compile(QuerySpec spec, PermissionContext permissions);
}
```

### Built-in Compilers

| Compiler | Output | Notes |
|---|---|---|
| `SQLCompiler` | Raw SQL | Injects RLS filters, resolves CTEs, SqlKata dialect-aware |
| `MarkdownCompiler` | Markdown report | Table structure + narrative scaffold |
| `HTMLCompiler` | Styled HTML | Extends MarkdownCompiler |
| `ChartSpecCompiler` *(future)* | Vega-Lite JSON | Driven by `intent` field |

### SQLCompiler — Build Order

1. SELECT from measures + dimensions + resolved calculations
2. FROM + JOINs (including calculation-required joins)
3. CTEs → prepend WITH block
4. WHERE from `filters`
5. **Inject RLS filters** (AND-appended, non-removable)
6. GROUP BY, HAVING, ORDER BY, LIMIT
7. Validate output SQL via SqlKata parse (not LLM)

### Supported Databases

SqlKata compilers handle dialect differences transparently:
- Microsoft SQL Server
- PostgreSQL
- MySQL
- Oracle

### OutputBundle

```json
{
  "raw_output": "WITH refund_window AS (...) SELECT ...",
  "explanation": "This query returns net revenue by region for completed orders in 2024...",
  "spec": { "...full QuerySpec..." },
  "compiler": "sql",
  "dialect": "postgres",
  "warnings": []
}
```

The `explanation` is rendered deterministically from the QuerySpec using a template renderer.

---

## 8. API Layer (FastEndpoints)

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/projects` | Create project, upload DDL + docs + permissions.yaml + calculations.yaml |
| `GET` | `/projects/{id}` | Retrieve project schema summary |
| `GET` | `/projects/{id}/calculations` | Browse admin-defined calculation library |
| `POST` | `/projects/{id}/conversations` | Start conversation session (pass SSO token) |
| `POST` | `/conversations/{id}/messages` | Send message, receive response + partial QuerySpec |
| `POST` | `/conversations/{id}/confirm` | Confirm spec → compile → return OutputBundle |
| `GET` | `/conversations/{id}/spec` | Retrieve current QuerySpec at any point |
| `GET` | `/conversations/{id}/history` | Full conversation transcript |

---

## 9. Tech Stack

| Layer | Choice | Reason |
|---|---|---|
| Language | C# (.NET 9) | Type safety, strong ecosystem, Dapper/SqlKata native |
| API | FastEndpoints | Minimal, performant, clean endpoint model |
| LLM Client | Raw `HttpClient` + `ILLMProvider` interface | Full control, swap providers via config (URL + auth) |
| Formula Parsing | Pidgin | C# parser combinators, lightweight, extensible handler per syntax |
| SQL Generation | SqlKata | Multi-dialect (MSSQL, PostgreSQL, MySQL, Oracle), fluent API |
| ORM / Storage | Dapper | Lightweight, raw SQL control, no magic |
| Spec Validation | FluentValidation + C# records | Runtime enforcement of QuerySpec shape |
| Storage DB | PostgreSQL (projects, specs, conversations) | |

### ILLMProvider Interface

```csharp
interface ILLMProvider
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, LLMOptions options);
}

record LLMOptions(float Temperature, string ResponseFormat, int MaxTokens);
```

Provider configuration (URL, auth header, model name) is injected via `appsettings.json` — no code changes to switch between Claude, OpenAI, Azure OpenAI, Ollama, or any OpenAI-compatible endpoint.

---

## 10. Extensibility Summary

| Extension point | How to extend |
|---|---|
| New schema source | Implement `ISchemaAdapter`, register in adapter registry |
| New formula syntax | Implement `IFormulaHandler` with Pidgin grammar, register in handler registry |
| New output format | Implement `ICompiler`, register in compiler registry |
| New LLM provider | Implement `ILLMProvider`, configure URL + auth in settings |
| New database dialect | Add SqlKata compiler for that dialect |
| New permission scheme | Add new rule types to `permissions.yaml` schema + update `PermissionAdapter` |
