import { Navigate, Route, Routes } from 'react-router-dom';
import { AdminRoute } from '../features/auth/AdminRoute';
import { AuthProvider } from '../features/auth/AuthContext';
import { GuestRoute } from '../features/auth/GuestRoute';
import { LoginPage } from '../features/auth/LoginPage';
import { ProtectedRoute } from '../features/auth/ProtectedRoute';
import { CompanySummaryPage } from '../features/bff/CompanySummaryPage';
import { ChatPage } from '../features/chat/ChatPage';
import { CompaniesPage } from '../features/companies/CompaniesPage';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { DocumentsPage } from '../features/documents/DocumentsPage';
import { TicketsPage } from '../features/tickets/TicketsPage';
import { UsersPage } from '../features/users/UsersPage';
import { PrivateLayout } from '../layouts/PrivateLayout';

export function AppRouter() {
  return (
    <AuthProvider>
      <Routes>
        <Route
          path="/login"
          element={(
            <GuestRoute>
              <LoginPage />
            </GuestRoute>
          )}
        />

        <Route element={<ProtectedRoute />}>
          <Route element={<PrivateLayout />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route element={<AdminRoute />}>
              <Route path="/companies" element={<CompaniesPage />} />
              <Route path="/users" element={<UsersPage />} />
              <Route path="/company-summary" element={<CompanySummaryPage />} />
            </Route>
            <Route path="/documents" element={<DocumentsPage />} />
            <Route path="/chat" element={<ChatPage />} />
            <Route path="/tickets" element={<TicketsPage />} />
          </Route>
        </Route>

        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </AuthProvider>
  );
}
