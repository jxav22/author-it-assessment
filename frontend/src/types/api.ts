/** Types matching docs/api-contract.md exactly. */

export type Topic = {
  id: number
  title: string
  body: string
  tags: string[]
  createdAt: string
  updatedAt: string
  version: number
}

export type BookSummary = {
  id: number
  title: string
  description: string | null
  createdAt: string
}

export type BookReference = {
  referenceId: number
  topicId: number
  topicTitle: string
  level: number
  position: number
}

export type BookDetail = BookSummary & {
  contents: BookReference[]
}

export type TopicUsage = {
  topicId: number
  topicTitle: string
  books: {
    bookId: number
    bookTitle: string
    references: {
      referenceId: number
      level: number
      position: number
    }[]
  }[]
  totalReferences: number
  bookCount: number
}

export type CreateTopicRequest = {
  title: string
  body: string
  tags: string[]
}

export type UpdateTopicRequest = {
  title: string
  body: string
  tags: string[]
  version: number
}

export type CreateBookRequest = {
  title: string
  description?: string
}

export type CreateBookReferenceRequest = {
  topicId: number
  level: number
  position?: number
}

/** ASP.NET Core Problem Details (camelCase field errors). */
export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}
