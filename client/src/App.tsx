import { NavLink, Route, Routes } from 'react-router-dom';

export default function App() {
  return (
    <div className="app">
      <header className="app-header">
        <span className="brand">Site Inspection Tracker</span>
        <nav>
          <NavLink to="/" end>
            Dashboard
          </NavLink>
        </nav>
      </header>
      <main className="app-main">
        <Routes>
          <Route path="*" element={<p>Pages coming soon.</p>} />
        </Routes>
      </main>
    </div>
  );
}
