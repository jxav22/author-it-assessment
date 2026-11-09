import { Navigate, Route, Routes } from 'react-router-dom'
import { BookListPage } from './pages/BookListPage'
import { BookOutlinePage } from './pages/BookOutlinePage'
import { TopicEditorPage } from './pages/TopicEditorPage'
import { TopicLibraryPage } from './pages/TopicLibraryPage'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<TopicLibraryPage />} />
      <Route path="/topics" element={<Navigate to="/" replace />} />
      <Route path="/topics/:id" element={<TopicEditorPage />} />
      <Route path="/books" element={<BookListPage />} />
      <Route path="/books/:id" element={<BookOutlinePage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
