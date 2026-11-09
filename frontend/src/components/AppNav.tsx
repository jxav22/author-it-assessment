import { NavLink } from 'react-router-dom'

export function AppNav() {
  return (
    <nav className="app-nav" aria-label="Primary">
      <NavLink to="/" end>
        Topics
      </NavLink>
      <NavLink to="/books">Books</NavLink>
    </nav>
  )
}
