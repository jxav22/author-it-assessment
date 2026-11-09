import { Navigate, Route, Routes } from 'react-router-dom'
import { TopicEditorPage } from './pages/TopicEditorPage'
import { TopicLibraryPage } from './pages/TopicLibraryPage'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<TopicLibraryPage />} />
      <Route path="/topics" element={<Navigate to="/" replace />} />
      <Route path="/topics/:id" element={<TopicEditorPage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
