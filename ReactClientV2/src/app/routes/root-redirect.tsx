import { Navigate } from 'react-router-dom';

import { paths } from '@/config/paths';
import { useAuthStore } from '@/stores/auth-store';

export function Component() {
  const isAuthenticated = useAuthStore((state) => Boolean(state.session));
  return (
    <Navigate to={isAuthenticated ? paths.chat.getHref() : paths.auth.login.getHref()} replace />
  );
}
