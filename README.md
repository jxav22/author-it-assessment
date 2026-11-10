# Author-it Assessment

## Contents

1. [How to run the project](#1-how-to-run-the-project)
2. [Design decisions and trade-offs](#2-design-decisions-and-trade-offs)
3. [Removing a reference with indented entries](#3-removing-a-reference-with-indented-entries)
4. [How to implement deleting a Topic that Books still reference](#4-how-to-implement-deleting-a-topic-that-books-still-reference)
5. [Unfinished work and what I would do with more time](#5-unfinished-work-and-what-i-would-do-with-more-time)

[Additional notes](#additional-notes): stack, domain model, code structure, tests and checks.

## 1. How to run the project

Requirements:

- .NET 10 SDK
- Node.js 20.19+ or 22.12+ (Vite 8's minimum)

```bash
# Terminal 1: API on http://localhost:5080
cd backend
dotnet run --project src/AuthorIt.Api --launch-profile http

# Terminal 2: UI on http://localhost:5173
cd frontend
npm ci
npm run dev
```

CORS (Development only) allows `http://localhost:5173`, so use `localhost` rather than `127.0.0.1`; the frontend's API address comes from `VITE_API_BASE_URL` (default `http://localhost:5080`).

Data is in memory and reseeded on every start (5 topics, 3 books).

## 2. Design decisions and trade-offs

### In-memory storage

The brief allows it, and it's simpler than a database. The trade-off is that data doesn't persist across restarts, and a single lock means requests run one at a time rather than concurrently.

### Edit conflicts: 409

Each topic has a version number, and a save sends back the version the editor started from. The API checks it and saves in one locked step, so if someone else saved first, the stale save gets 409 and nothing changes. I used 409 rather than 412 because 412 is for conditions in request headers, and this version is in the JSON body.

### Unsaved drafts

The editor keeps what the author is typing separate from the server's copy, so a background refresh can't overwrite it. After a 409 their text stays in the form, and Reload latest swaps in the current version.

### Reference responses: 201 and 204

Adding a reference returns 201 with the new entry; removing one returns 204 with no body. Neither returns the whole book, so the outline page fetches it again.

### Missing resources: 404

The contract treats anything missing as 404.

### Status codes

I used ASP.NET Core's built-in `Results` helpers, which made most codes unambiguous. The only real choices were 409 for edit conflicts and 404 for things that were missing, explained above. Every error comes back in the same Problem Details shape, and validation errors list messages by field name so the editor can show each one next to its input.

## 3. Removing a reference with indented entries

Removing a reference removes that occurrence only; the topic is untouched. Entries indented beneath it move up one level and positions close up:

```text
Before            After removing B
1  A              1  A
2  B              2  C      (was level 2)
3    C            3    D    (was level 3)
4      D          4  E
5  E
```

## 4. How to implement deleting a Topic that Books still reference

The main edge case is that the API should refuse the delete while any book still references it, since the book would be left pointing at a topic that no longer exists. I would make the API answer 409 Conflict if this happens and list the references so they can be manually removed first. If nothing points to it, it would delete it cleanly.

## 5. Unfinished work and what I would do with more time

I left out the stretch goals from the brief (deleting topics, moving references, an unsaved-changes warning, frontend tests and a one-command start), so I would probably do those first.

With more time I would also want to move to a better storage system like SQLite. I would target the concurrency issues, starting with giving books a version number like topics have, so adding a reference can't land in an outline the author hasn't seen. I would also look into using database transactions instead of one global lock. I would additionally develop more tests so I could have a better idea of the repository health, and ideally set up an environment for test-driven development.

---

## Additional notes

### Stack

| Folder | Stack |
| --- | --- |
| `backend/` | .NET 10, ASP.NET Core Minimal APIs, in-memory store, xUnit |
| `frontend/` | React, TypeScript, Vite, React Router, TanStack Query, plain CSS |

API shapes: [`docs/api-contract.md`](docs/api-contract.md).

### Domain model

- **Topic:** `title`, `body`, `tags`, timestamps, `version` (starts at 1, +1 per successful update).
- **Book:** `title`, optional `description`, `createdAt`, `contents`.
- **BookReference:** `referenceId` (this occurrence), `topicId`, `level` (indent depth), `position` (1-based).

### Code structure

- `backend/src/AuthorIt.Api/Endpoints/`: routes and HTTP results.
- `backend/src/AuthorIt.Api/Services/`: operations and validation rules.
- `backend/src/AuthorIt.Api/Store/LibraryStore.cs`: singleton store; one lock for all access, cloned snapshots on read.
- `frontend/src/api/`: typed `fetch` wrappers; errors become `ApiError` with Problem Details.
- `frontend/src/pages/`: Topic library, Topic editor, Book list, Book outline. TanStack Query caches server data; mutations invalidate affected queries.

### Tests and checks

```bash
cd backend && dotnet test                     # 14 xUnit API tests
cd frontend && npm run lint && npm run build  # oxlint; tsc + vite build
```

The API tests (`WebApplicationFactory`) cover field validation, stale updates (409, no change), version increments, ordered contents with live titles, unknown topic (404), invalid levels (400, outline unchanged), descendant promotion on delete, and usage counts including repeats in one book. No frontend tests.

Used A.I tooling where appropriate including Cursor for development, worktrees for parallelization, and GitHub Issues for coordination.
