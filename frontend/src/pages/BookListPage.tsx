import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { listBooks } from '../api/books'
import { queryKeys } from '../api/queryKeys'
import { AppNav } from '../components/AppNav'
import { errorMessage } from '../lib/errors'

function formatCreatedAt(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return iso
  }
  return date.toLocaleString(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

export function BookListPage() {
  const booksQuery = useQuery({
    queryKey: queryKeys.books.all,
    queryFn: listBooks,
  })

  return (
    <main className="page">
      <AppNav />
      <header className="page-header">
        <h1>Books</h1>
        <p className="muted">Open a Book to view and edit its outline.</p>
      </header>

      <section className="panel" aria-labelledby="books-heading">
        <h2 id="books-heading">All books</h2>

        {booksQuery.isLoading && <p className="muted">Loading books…</p>}

        {booksQuery.isError && (
          <div className="error" role="alert">
            <p>{errorMessage(booksQuery.error, 'Failed to load books')}</p>
            <button type="button" onClick={() => void booksQuery.refetch()}>
              Retry
            </button>
          </div>
        )}

        {booksQuery.isSuccess && booksQuery.data.length === 0 && (
          <p className="muted">No books available.</p>
        )}

        {booksQuery.isSuccess && booksQuery.data.length > 0 && (
          <table className="topic-table">
            <thead>
              <tr>
                <th scope="col">Title</th>
                <th scope="col">Description</th>
                <th scope="col">Created</th>
              </tr>
            </thead>
            <tbody>
              {booksQuery.data.map((book) => (
                <tr key={book.id}>
                  <td>
                    <Link to={`/books/${book.id}`}>{book.title}</Link>
                  </td>
                  <td>
                    {book.description ? (
                      book.description
                    ) : (
                      <span className="muted">—</span>
                    )}
                  </td>
                  <td>{formatCreatedAt(book.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </main>
  )
}
