# Author-it API Contract — v1

This file is the agreed implementation contract. The assessment PDF remains authoritative on requirements. Changes require coordination between backend and frontend owners.

## Conventions

- Backend: [http://localhost:5080](http://localhost:5080)

- Frontend: [http://localhost:5173](http://localhost:5173)

- JSON: camelCase; timestamps: UTC ISO 8601 strings

- IDs: integers; Topic version starts at 1 and increments on every successful PUT

- Positions: 1-based; a new reference may be inserted at positions 1 through N+1

- Errors: [ASP.NET](http://ASP.NET) Core Problem Details, with field-keyed errors for validation

## DTOs

Topic = {

  id: number;

  title: string;

  body: string;

  tags: string[];

  createdAt: string;

  updatedAt: string;

  version: number;

}

BookSummary = {

  id: number;

  title: string;

  description: string | null;

  createdAt: string;

}

BookReference = {

  referenceId: number;

  topicId: number;

  topicTitle: string;

  level: number;

  position: number;

}

BookDetail = BookSummary & {

  contents: BookReference[];

}

TopicUsage = {

  topicId: number;

  topicTitle: string;

  books: {

    bookId: number;

    bookTitle: string;

    references: {

      referenceId: number;

      level: number;

      position: number;

    }[];

  }[];

  totalReferences: number;

  bookCount: number;

}

## Endpoints

GET    /topics                              -> 200 Topic[]

GET    /topics/{id}                         -> 200 Topic

POST   /topics                              -> 201 Topic

PUT    /topics/{id}                         -> 200 Topic

GET    /books                               -> 200 BookSummary[]

GET    /books/{id}                          -> 200 BookDetail

POST   /books                               -> 201 BookSummary

POST   /books/{id}/references               -> 201 BookReference

DELETE /books/{id}/references/{referenceId} -> 204 empty

GET    /topics/{id}/usage                   -> 200 TopicUsage

Request bodies:

POST /topics: {title, body, tags}

PUT /topics/{id}: {title, body, tags, version}

POST /books: {title, description?}

POST /books/{id}/references: {topicId, level, position?}

## Errors

400: invalid request or domain validation

404: missing resource, including referenced Topic

409: stale Topic version; reject without mutation

Errors use Problem Details:

{ type, title, status, detail?, errors? }

Field errors use camelCase field names and string[] values.

Missing-resource and conflict errors do not require an errors map.

## Domain rules

- Topic title required, max 200 characters.

- Each tag nonempty, max 50 characters; tags unique ignoring case.

- Trim title and tags before validation. Body is plain text.

- Books reference Topics through BookReference entries; never duplicate Topic bodies.

- Resolve topicTitle dynamically from the Topic.

- Same Topic can appear repeatedly within the same Book.

- First Book reference must have level 1.

- Each subsequent reference may descend by at most one level relative to the previous entry.

- Omitted insertion position means append.

- Validate the entire proposed outline after insertion before mutating the store.

- Removing a reference never deletes its Topic.

- On removal, promote its contiguous descendants by one level and close position gaps.

- totalReferences counts all occurrences; bookCount counts distinct Books.

- Seed Topic 3 must report 3 references across 2 Books.

- Protect read-modify-write operations on singleton collections.

- Frontend uses TanStack Query for server state and local React state for drafts/UI.

- No mock API in the final application.