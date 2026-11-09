import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  addBookReference,
  getBook,
  removeBookReference,
} from '../api/books'
import { ApiError } from '../api/client'
import { queryKeys } from '../api/queryKeys'
import { listTopics } from '../api/topics'
import { AppNav } from '../components/AppNav'
import { errorMessage } from '../lib/errors'
import { fieldErrors } from '../lib/fieldErrors'

function Shell({ children }: { children: ReactNode }) {
  return (
    <main className="page">
      <AppNav />
      <header className="page-header">
        <p>
          <Link to="/books">← Books</Link>
        </p>
        <h1>Book outline</h1>
      </header>
      {children}
    </main>
  )
}

export function BookOutlinePage() {
  const { id: idParam } = useParams<{ id: string }>()
  const bookId = Number(idParam)
  const bookIdValid = Number.isInteger(bookId) && bookId > 0

  const bookQuery = useQuery({
    queryKey: queryKeys.books.detail(bookId),
    queryFn: () => getBook(bookId),
    enabled: bookIdValid,
    retry: (failureCount, error) => {
      if (error instanceof ApiError && error.status === 404) {
        return false
      }
      return failureCount < 1
    },
  })

  if (!bookIdValid) {
    return (
      <Shell>
        <div className="error" role="alert">
          <p>Invalid book id “{idParam}”.</p>
        </div>
      </Shell>
    )
  }

  if (bookQuery.isLoading) {
    return (
      <Shell>
        <p className="muted">Loading book…</p>
      </Shell>
    )
  }

  if (bookQuery.isError || !bookQuery.data) {
    const notFound =
      bookQuery.error instanceof ApiError && bookQuery.error.status === 404
    return (
      <Shell>
        <div className="error" role="alert">
          <p>
            {notFound
              ? `Book ${bookId} was not found.`
              : errorMessage(bookQuery.error, 'Failed to load book')}
          </p>
          {!notFound && (
            <button type="button" onClick={() => void bookQuery.refetch()}>
              Retry
            </button>
          )}
        </div>
      </Shell>
    )
  }

  return <BookOutlineView key={bookQuery.data.id} bookId={bookQuery.data.id} />
}

function BookOutlineView({ bookId }: { bookId: number }) {
  const queryClient = useQueryClient()
  const [topicId, setTopicId] = useState('')
  const [level, setLevel] = useState('1')
  const [position, setPosition] = useState('')
  const [removingId, setRemovingId] = useState<number | null>(null)

  const bookQuery = useQuery({
    queryKey: queryKeys.books.detail(bookId),
    queryFn: () => getBook(bookId),
  })

  const topicsQuery = useQuery({
    queryKey: queryKeys.topics.all,
    queryFn: listTopics,
  })

  const addMutation = useMutation({
    mutationFn: (payload: {
      topicId: number
      level: number
      position?: number
    }) => addBookReference(bookId, payload),
    onSuccess: async (reference) => {
      setPosition('')
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: queryKeys.books.detail(bookId),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.books.all }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.topics.usage(reference.topicId),
        }),
      ])
    },
  })

  const removeMutation = useMutation({
    mutationFn: ({
      referenceId,
    }: {
      referenceId: number
      topicId: number
    }) => removeBookReference(bookId, referenceId),
    onSuccess: async (_void, variables) => {
      setRemovingId(null)
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: queryKeys.books.detail(bookId),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.books.all }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.topics.usage(variables.topicId),
        }),
      ])
    },
    onError: () => {
      setRemovingId(null)
    },
  })

  const book = bookQuery.data
  const contents = book?.contents ?? []
  const appendPosition = contents.length + 1

  const addError =
    addMutation.error instanceof ApiError ? addMutation.error : null
  const topicIdErrors = fieldErrors(addError?.problem.errors, 'topicId')
  const levelErrors = fieldErrors(addError?.problem.errors, 'level')
  const positionErrors = fieldErrors(addError?.problem.errors, 'position')
  const generalAddError =
    addMutation.isError
      ? errorMessage(addMutation.error, 'Failed to add reference')
      : null

  function handleAdd(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const parsedTopicId = Number(topicId)
    const parsedLevel = Number(level)
    const parsedPosition =
      position.trim() === '' ? undefined : Number(position)

    addMutation.mutate({
      topicId: parsedTopicId,
      level: parsedLevel,
      ...(parsedPosition !== undefined ? { position: parsedPosition } : {}),
    })
  }

  if (bookQuery.isLoading && !book) {
    return (
      <Shell>
        <p className="muted">Loading book…</p>
      </Shell>
    )
  }

  if ((bookQuery.isError || !book) && !bookQuery.isFetching) {
    return (
      <Shell>
        <div className="error" role="alert">
          <p>{errorMessage(bookQuery.error, 'Failed to load book')}</p>
          <button type="button" onClick={() => void bookQuery.refetch()}>
            Retry
          </button>
        </div>
      </Shell>
    )
  }

  if (!book) {
    return (
      <Shell>
        <p className="muted">Loading book…</p>
      </Shell>
    )
  }

  return (
    <main className="page">
      <AppNav />
      <header className="page-header">
        <p>
          <Link to="/books">← Books</Link>
        </p>
        <h1>{book.title}</h1>
        {book.description ? (
          <p className="muted">{book.description}</p>
        ) : (
          <p className="muted">No description.</p>
        )}
      </header>

      <section className="panel" aria-labelledby="outline-heading">
        <h2 id="outline-heading">Outline</h2>

        {contents.length === 0 && (
          <p className="muted">This book has no references yet.</p>
        )}

        {contents.length > 0 && (
          <ol className="outline-list">
            {contents.map((entry) => (
              <li
                key={entry.referenceId}
                className="outline-item"
                style={{ paddingLeft: `${(entry.level - 1) * 1.5}rem` }}
              >
                <div className="outline-row">
                  <span className="outline-meta muted">
                    #{entry.position} · L{entry.level} · ref {entry.referenceId}
                  </span>
                  <Link to={`/topics/${entry.topicId}`}>
                    {entry.topicTitle}
                  </Link>
                  <button
                    type="button"
                    className="button-secondary"
                    disabled={removeMutation.isPending}
                    onClick={() => {
                      setRemovingId(entry.referenceId)
                      removeMutation.mutate({
                        referenceId: entry.referenceId,
                        topicId: entry.topicId,
                      })
                    }}
                  >
                    {removeMutation.isPending &&
                    removingId === entry.referenceId
                      ? 'Removing…'
                      : 'Remove'}
                  </button>
                </div>
              </li>
            ))}
          </ol>
        )}

        {removeMutation.isError && (
          <div className="error" role="alert">
            <p>
              {errorMessage(
                removeMutation.error,
                'Failed to remove reference',
              )}
            </p>
          </div>
        )}
      </section>

      <section className="panel" aria-labelledby="add-heading">
        <h2 id="add-heading">Add reference</h2>
        <form className="create-form" onSubmit={handleAdd}>
          <label>
            Topic
            <select
              value={topicId}
              onChange={(event) => {
                setTopicId(event.target.value)
                if (addMutation.isError) {
                  addMutation.reset()
                }
              }}
              required
              disabled={addMutation.isPending || topicsQuery.isLoading}
              aria-invalid={topicIdErrors.length > 0}
            >
              <option value="">
                {topicsQuery.isLoading
                  ? 'Loading topics…'
                  : 'Select an existing topic'}
              </option>
              {(topicsQuery.data ?? []).map((topic) => (
                <option key={topic.id} value={topic.id}>
                  {topic.title}
                </option>
              ))}
            </select>
            {topicIdErrors.map((message) => (
              <span key={message} className="field-error">
                {message}
              </span>
            ))}
          </label>

          <label>
            Level
            <input
              type="number"
              min={1}
              step={1}
              value={level}
              onChange={(event) => {
                setLevel(event.target.value)
                if (addMutation.isError) {
                  addMutation.reset()
                }
              }}
              required
              disabled={addMutation.isPending}
              aria-invalid={levelErrors.length > 0}
            />
            {levelErrors.map((message) => (
              <span key={message} className="field-error">
                {message}
              </span>
            ))}
          </label>

          <label>
            Position
            <select
              value={position}
              onChange={(event) => {
                setPosition(event.target.value)
                if (addMutation.isError) {
                  addMutation.reset()
                }
              }}
              disabled={addMutation.isPending}
              aria-invalid={positionErrors.length > 0}
            >
              <option value="">Append (position {appendPosition})</option>
              {Array.from({ length: appendPosition }, (_, index) => {
                const value = index + 1
                return (
                  <option key={value} value={value}>
                    Insert at {value}
                  </option>
                )
              })}
            </select>
            {positionErrors.map((message) => (
              <span key={message} className="field-error">
                {message}
              </span>
            ))}
          </label>

          {topicsQuery.isError && (
            <div className="error" role="alert">
              <p>
                {errorMessage(
                  topicsQuery.error,
                  'Failed to load topics for picker',
                )}
              </p>
              <button type="button" onClick={() => void topicsQuery.refetch()}>
                Retry topics
              </button>
            </div>
          )}

          {generalAddError && (
            <div className="error" role="alert">
              <p>{generalAddError}</p>
              {addError?.problem.errors &&
                topicIdErrors.length === 0 &&
                levelErrors.length === 0 &&
                positionErrors.length === 0 && (
                  <ul>
                    {Object.entries(addError.problem.errors).flatMap(
                      ([field, messages]) =>
                        messages.map((message) => (
                          <li key={`${field}-${message}`}>
                            {field}: {message}
                          </li>
                        )),
                    )}
                  </ul>
                )}
            </div>
          )}

          <button
            type="submit"
            disabled={
              addMutation.isPending ||
              topicsQuery.isLoading ||
              topicsQuery.isError ||
              !topicId
            }
          >
            {addMutation.isPending ? 'Adding…' : 'Add reference'}
          </button>
        </form>
      </section>
    </main>
  )
}
