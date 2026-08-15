import { Navigate, Outlet, useLocation } from 'react-router-dom';

import { paths } from '@/config/paths';
import { useAuthStore } from '@/stores/auth-store';

export function GuestRoute() {
  const isAuthenticated = useAuthStore((state) => Boolean(state.session));
  return isAuthenticated ? <Navigate to={paths.chat.getHref()} replace /> : <Outlet />;
}

export function ProtectedRoute() {
  const isAuthenticated = useAuthStore((state) => Boolean(state.session));
  const location = useLocation();

  if (!isAuthenticated) {
    return (
      <Navigate to={paths.auth.login.getHref(`${location.pathname}${location.search}`)} replace />
    );
  }

  return <Outlet />;
}
