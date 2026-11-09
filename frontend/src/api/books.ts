import type {
  BookDetail,
  BookReference,
  BookSummary,
  CreateBookReferenceRequest,
} from '../types/api'
import { apiFetch } from './client'

export function listBooks(): Promise<BookSummary[]> {
  return apiFetch<BookSummary[]>('/books')
}

export function getBook(id: number): Promise<BookDetail> {
  return apiFetch<BookDetail>(`/books/${id}`)
}

export function addBookReference(
  bookId: number,
  payload: CreateBookReferenceRequest,
): Promise<BookReference> {
  return apiFetch<BookReference>(`/books/${bookId}/references`, {
    method: 'POST',
    body: payload,
  })
}

export function removeBookReference(
  bookId: number,
  referenceId: number,
): Promise<void> {
  return apiFetch<void>(`/books/${bookId}/references/${referenceId}`, {
    method: 'DELETE',
  })
}
