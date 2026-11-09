export const queryKeys = {
  topics: {
    all: ['topics'] as const,
    detail: (id: number) => ['topics', id] as const,
    usage: (id: number) => ['topics', id, 'usage'] as const,
  },
  books: {
    all: ['books'] as const,
    detail: (id: number) => ['books', id] as const,
  },
}
