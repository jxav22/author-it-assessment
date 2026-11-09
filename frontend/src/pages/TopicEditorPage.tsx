import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiError } from '../api/client'
import { queryKeys } from '../api/queryKeys'
import { getTopic, getTopicUsage, updateTopic } from '../api/topics'
import { AppNav } from '../components/AppNav'
import { errorMessage } from '../lib/errors'
import { fieldErrors } from '../lib/fieldErrors'
import { formatTags, parseTags, tagsEqual } from '../lib/tags'
import type { Topic } from '../types/api'

function EditorShell({
  title,
  children,
}: {
  title?: string
  children: ReactNode
}) {
  return (
    <main className="page">
      <AppNav />
      <header className="page-header">
        <p>
          <Link to="/">← Topic library</Link>
        </p>
        <h1>{title ?? 'Topic editor'}</h1>
      </header>
      {children}
    </main>
  )
}

export function TopicEditorPage() {
  const { id: idParam } = useParams<{ id: string }>()
  const topicId = Number(idParam)
  const topicIdValid = Number.isInteger(topicId) && topicId > 0

  const topicQuery = useQuery({
    queryKey: queryKeys.topics.detail(topicId),
    queryFn: () => getTopic(topicId),
    enabled: topicIdValid,
    retry: (failureCount, error) => {
      if (error instanceof ApiError && error.status === 404) {
        return false
      }
      return failureCount < 1
    },
  })

  if (!topicIdValid) {
    return (
      <EditorShell>
        <div className="error" role="alert">
          <p>Invalid topic id “{idParam}”.</p>
        </div>
      </EditorShell>
    )
  }

  if (topicQuery.isLoading) {
    return (
      <EditorShell>
        <p className="muted">Loading topic…</p>
      </EditorShell>
    )
  }

  if (topicQuery.isError || !topicQuery.data) {
    const notFound =
      topicQuery.error instanceof ApiError && topicQuery.error.status === 404
    return (
      <EditorShell>
        <div className="error" role="alert">
          <p>
            {notFound
              ? `Topic ${topicId} was not found.`
              : errorMessage(topicQuery.error, 'Failed to load topic')}
          </p>
          {!notFound && (
            <button type="button" onClick={() => void topicQuery.refetch()}>
              Retry
            </button>
          )}
        </div>
      </EditorShell>
    )
  }

  return (
    <TopicEditorForm key={topicQuery.data.id} initialTopic={topicQuery.data} />
  )
}

function TopicEditorForm({ initialTopic }: { initialTopic: Topic }) {
  const topicId = initialTopic.id
  const queryClient = useQueryClient()

  const [baseline, setBaseline] = useState<Topic>(initialTopic)
  const [title, setTitle] = useState(initialTopic.title)
  const [body, setBody] = useState(initialTopic.body)
  const [tagsInput, setTagsInput] = useState(formatTags(initialTopic.tags))

  const topicQuery = useQuery({
    queryKey: queryKeys.topics.detail(topicId),
    queryFn: () => getTopic(topicId),
  })

  const usageQuery = useQuery({
    queryKey: queryKeys.topics.usage(topicId),
    queryFn: () => getTopicUsage(topicId),
    retry: false,
  })

  function applyTopic(topic: Topic) {
    setBaseline(topic)
    setTitle(topic.title)
    setBody(topic.body)
    setTagsInput(formatTags(topic.tags))
  }

  const saveMutation = useMutation({
    mutationFn: (payload: {
      title: string
      body: string
      tags: string[]
      version: number
    }) => updateTopic(topicId, payload),
    onSuccess: async (topic) => {
      applyTopic(topic)
      queryClient.setQueryData(queryKeys.topics.detail(topic.id), topic)
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.topics.all }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.topics.usage(topic.id),
        }),
        queryClient.invalidateQueries({ queryKey: ['books'] }),
      ])
    },
  })

  const draftTags = parseTags(tagsInput)
  const isDirty =
    title !== baseline.title ||
    body !== baseline.body ||
    !tagsEqual(draftTags, baseline.tags)

  const saveError =
    saveMutation.error instanceof ApiError ? saveMutation.error : null
  const isConflict = saveError?.status === 409
  const titleErrors = fieldErrors(saveError?.problem.errors, 'title')
  const bodyErrors = fieldErrors(saveError?.problem.errors, 'body')
  const tagsErrors = fieldErrors(saveError?.problem.errors, 'tags')
  const generalSaveError =
    saveMutation.isError && !isConflict
      ? errorMessage(saveMutation.error, 'Failed to save topic')
      : null

  async function reloadLatest() {
    const result = await topicQuery.refetch()
    if (result.data) {
      applyTopic(result.data)
      saveMutation.reset()
      await queryClient.invalidateQueries({
        queryKey: queryKeys.topics.usage(topicId),
      })
    }
  }

  function handleSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    saveMutation.mutate({
      title: title.trim(),
      body,
      tags: parseTags(tagsInput),
      version: baseline.version,
    })
  }

  return (
    <main className="page">
      <AppNav />
      <header className="page-header">
        <p>
          <Link to="/">← Topic library</Link>
        </p>
        <div className="editor-heading">
          <h1>Topic editor</h1>
          <p className={isDirty ? 'status-dirty' : 'muted'}>
            {isDirty ? 'Unsaved changes' : 'All changes saved'}
            {` · version ${baseline.version}`}
          </p>
        </div>
      </header>

      <section className="panel" aria-labelledby="usage-heading">
        <h2 id="usage-heading">Reuse</h2>
        {usageQuery.isLoading && (
          <p className="muted">Checking where this topic is used…</p>
        )}
        {usageQuery.isError && (
          <p className="muted" role="status">
            Usage report unavailable
            {usageQuery.error instanceof ApiError
              ? ` (${usageQuery.error.status}). `
              : '. '}
            Editing is still available.
          </p>
        )}
        {usageQuery.isSuccess && (
          <>
            {usageQuery.data.bookCount > 1 && (
              <div className="warning" role="status">
                This topic is used in {usageQuery.data.bookCount} books (
                {usageQuery.data.totalReferences} references). Saving changes
                updates the content in all of them.
              </div>
            )}
            {usageQuery.data.bookCount === 0 && (
              <p className="muted">Not referenced by any books.</p>
            )}
            {usageQuery.data.bookCount === 1 && (
              <p className="muted">
                Used in 1 book ({usageQuery.data.totalReferences}{' '}
                {usageQuery.data.totalReferences === 1
                  ? 'reference'
                  : 'references'}
                ).
              </p>
            )}
            {usageQuery.data.books.length > 0 && (
              <ul className="usage-list">
                {usageQuery.data.books.map((book) => (
                  <li key={book.bookId}>
                    {book.bookTitle}
                    <span className="muted">
                      {' '}
                      — {book.references.length}{' '}
                      {book.references.length === 1
                        ? 'reference'
                        : 'references'}{' '}
                      (
                      {book.references
                        .map((ref) => `pos ${ref.position}`)
                        .join(', ')}
                      )
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </section>

      <section className="panel" aria-labelledby="edit-heading">
        <h2 id="edit-heading">Edit topic</h2>
        <form className="create-form" onSubmit={handleSave}>
          <label>
            Title
            <input
              type="text"
              value={title}
              onChange={(event) => {
                setTitle(event.target.value)
                if (saveMutation.isError) {
                  saveMutation.reset()
                }
              }}
              maxLength={200}
              required
              disabled={saveMutation.isPending}
              aria-invalid={titleErrors.length > 0}
            />
            {titleErrors.map((message) => (
              <span key={message} className="field-error">
                {message}
              </span>
            ))}
          </label>

          <label>
            Body
            <textarea
              value={body}
              onChange={(event) => {
                setBody(event.target.value)
                if (saveMutation.isError) {
                  saveMutation.reset()
                }
              }}
              rows={12}
              disabled={saveMutation.isPending}
              aria-invalid={bodyErrors.length > 0}
            />
            {bodyErrors.map((message) => (
              <span key={message} className="field-error">
                {message}
              </span>
            ))}
          </label>

          <label>
            Tags
            <input
              type="text"
              value={tagsInput}
              onChange={(event) => {
                setTagsInput(event.target.value)
                if (saveMutation.isError) {
                  saveMutation.reset()
                }
              }}
              placeholder="comma-separated"
              disabled={saveMutation.isPending}
              aria-invalid={tagsErrors.length > 0}
            />
            {tagsErrors.map((message) => (
              <span key={message} className="field-error">
                {message}
              </span>
            ))}
          </label>

          {isConflict && (
            <div className="error" role="alert">
              <p>
                This topic changed since you loaded it. Your edits are still in
                the form. Reload the latest version to continue, or keep editing
                and try again after reviewing.
              </p>
              <p>{saveError.message}</p>
              <button
                type="button"
                onClick={() => void reloadLatest()}
                disabled={topicQuery.isFetching}
              >
                {topicQuery.isFetching ? 'Reloading…' : 'Reload latest'}
              </button>
            </div>
          )}

          {generalSaveError && (
            <div className="error" role="alert">
              <p>{generalSaveError}</p>
              {saveError?.problem.errors &&
                titleErrors.length === 0 &&
                bodyErrors.length === 0 &&
                tagsErrors.length === 0 && (
                  <ul>
                    {Object.entries(saveError.problem.errors).flatMap(
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

          <div className="form-actions">
            <button
              type="submit"
              disabled={saveMutation.isPending || !isDirty}
            >
              {saveMutation.isPending ? 'Saving…' : 'Save topic'}
            </button>
            {isDirty && (
              <button
                type="button"
                className="button-secondary"
                disabled={saveMutation.isPending}
                onClick={() => {
                  applyTopic(baseline)
                  saveMutation.reset()
                }}
              >
                Discard changes
              </button>
            )}
          </div>
        </form>
      </section>
    </main>
  )
}
