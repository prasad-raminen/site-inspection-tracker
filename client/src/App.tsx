import { NavLink, Route, Routes } from 'react-router-dom';
import { DashboardPage } from './pages/DashboardPage';
import { FindingsPage } from './pages/FindingsPage';
import { InspectionDetailPage } from './pages/InspectionDetailPage';
import { InspectionFormPage } from './pages/InspectionFormPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { SiteDetailPage } from './pages/SiteDetailPage';
import { SitesPage } from './pages/SitesPage';

export default function App() {
  return (
    <div className="app">
      <header className="app-header">
        <span className="brand">Site Inspection Tracker</span>
        <nav aria-label="Main">
          <NavLink to="/" end>
            Dashboard
          </NavLink>
          <NavLink to="/sites">Sites</NavLink>
          <NavLink to="/findings">Findings</NavLink>
        </nav>
      </header>
      <main className="app-main">
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/sites" element={<SitesPage />} />
          <Route path="/sites/:siteId" element={<SiteDetailPage />} />
          <Route path="/inspections/new" element={<InspectionFormPage />} />
          <Route path="/inspections/:inspectionId" element={<InspectionDetailPage />} />
          <Route path="/inspections/:inspectionId/edit" element={<InspectionFormPage />} />
          <Route path="/findings" element={<FindingsPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </main>
    </div>
  );
}
