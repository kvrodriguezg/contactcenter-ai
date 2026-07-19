import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from './useAuth';

const ADMIN_ROLES = ['SuperAdmin', 'CompanyAdmin'];

/**
 * Guards routes that manage Companies/Users to SuperAdmin and CompanyAdmin roles.
 * Assumes it is nested under `ProtectedRoute`, so authentication is already resolved.
 */
export function AdminRoute() {
  const { user } = useAuth();

  if (!user || !ADMIN_ROLES.includes(user.role)) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
}
