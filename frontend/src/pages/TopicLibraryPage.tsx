import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { queryKeys } from '../api/queryKeys'
import { createTopic, listTopics } from '../api/topics'
import { parseTags } from '../lib/tags'
import type { Topic } from '../types/api'

function formatUpdatedAt(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return iso
  }
  return date.toLocaleString(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

function matchesFilter(topic: Topic, filter: string): boolean {
  const needle = filter.trim().toLowerCase()
  if (!needle) {
    return true
  }
  if (topic.title.toLowerCase().includes(needle)) {
    return true
  }
  return topic.tags.some((tag) => tag.toLowerCase().includes(needle))
}

export function TopicLibraryPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [filter, setFilter] = useState('')
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [tagsInput, setTagsInput] = useState('')

  const topicsQuery = useQuery({
    queryKey: queryKeys.topics.all,
    queryFn: listTopics,
  })

  const createMutation = useMutation({
    mutationFn: createTopic,
    onSuccess: async (topic) => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.topics.all })
      navigate(`/topics/${topic.id}`)
    },
  })

  const filteredTopics = useMemo(() => {
    const topics = topicsQuery.data ?? []
    return topics.filter((topic) => matchesFilter(topic, filter))
  }, [topicsQuery.data, filter])

  function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    createMutation.mutate({
      title: title.trim(),
      body,
      tags: parseTags(tagsInput),
    })
  }

  const createErrorMessage = createMutation.error
    ? createMutation.error instanceof ApiError
      ? createMutation.error.message
      : createMutation.error instanceof Error
        ? createMutation.error.message
        : 'Failed to create topic'
    : null
  const createFieldErrors =
    createMutation.error instanceof ApiError
      ? createMutation.error.problem.errors
      : undefined

  return (
    <main className="page">
      <header className="page-header">
        <h1>Topic library</h1>
        <p className="muted">Browse and create reusable Topics.</p>
      </header>

      <section className="panel" aria-labelledby="create-heading">
        <h2 id="create-heading">Create topic</h2>
        <form className="create-form" onSubmit={handleCreate}>
          <label>
            Title
            <input
              type="text"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              maxLength={200}
              required
              disabled={createMutation.isPending}
            />
          </label>
          <label>
            Body
            <textarea
              value={body}
              onChange={(event) => setBody(event.target.value)}
              rows={4}
              disabled={createMutation.isPending}
            />
          </label>
          <label>
            Tags
            <input
              type="text"
              value={tagsInput}
              onChange={(event) => setTagsInput(event.target.value)}
              placeholder="comma-separated"
              disabled={createMutation.isPending}
            />
          </label>
          {createErrorMessage && (
            <div className="error" role="alert">
              <p>{createErrorMessage}</p>
              {createFieldErrors && (
                <ul>
                  {Object.entries(createFieldErrors).flatMap(
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
          <button type="submit" disabled={createMutation.isPending}>
            {createMutation.isPending ? 'Creating…' : 'Create topic'}
          </button>
        </form>
      </section>

      <section className="panel" aria-labelledby="list-heading">
        <div className="list-toolbar">
          <h2 id="list-heading">Topics</h2>
          <label className="filter">
            Filter
            <input
              type="search"
              value={filter}
              onChange={(event) => setFilter(event.target.value)}
              placeholder="Filter by title or tag"
            />
          </label>
        </div>

        {topicsQuery.isLoading && <p className="muted">Loading topics…</p>}

        {topicsQuery.isError && (
          <div className="error" role="alert">
            <p>
              {topicsQuery.error instanceof ApiError
                ? topicsQuery.error.message
                : topicsQuery.error instanceof Error
                  ? topicsQuery.error.message
                  : 'Failed to load topics'}
            </p>
            <button type="button" onClick={() => void topicsQuery.refetch()}>
              Retry
            </button>
          </div>
        )}

        {topicsQuery.isSuccess && topicsQuery.data.length === 0 && (
          <p className="muted">No topics yet. Create one above.</p>
        )}

        {topicsQuery.isSuccess &&
          topicsQuery.data.length > 0 &&
          filteredTopics.length === 0 && (
            <p className="muted">No topics match “{filter.trim()}”.</p>
          )}

        {filteredTopics.length > 0 && (
          <table className="topic-table">
            <thead>
              <tr>
                <th scope="col">Title</th>
                <th scope="col">Tags</th>
                <th scope="col">Updated</th>
              </tr>
            </thead>
            <tbody>
              {filteredTopics.map((topic) => (
                <tr key={topic.id}>
                  <td>
                    <Link to={`/topics/${topic.id}`}>{topic.title}</Link>
                  </td>
                  <td>
                    {topic.tags.length === 0 ? (
                      <span className="muted">—</span>
                    ) : (
                      topic.tags.join(', ')
                    )}
                  </td>
                  <td>{formatUpdatedAt(topic.updatedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </main>
  )
}
