import { Link, useParams } from 'react-router-dom'

/** Placeholder route for Wave 2 — full editor lands later. */
export function TopicEditorPage() {
  const { id } = useParams<{ id: string }>()

  return (
    <main className="page">
      <header className="page-header">
        <p>
          <Link to="/">← Topic library</Link>
        </p>
        <h1>Topic editor</h1>
        <p className="muted">
          Topic {id} — editor implementation arrives in Wave 2.
        </p>
      </header>
    </main>
  )
}
