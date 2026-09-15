import { createBrowserRouter, RouterProvider } from 'react-router-dom';

import { paths } from '@/config/paths';

import { GuestRoute, ProtectedRoute } from './route-guards';

const appRouter = createBrowserRouter([
  {
    path: paths.home.path,
    lazy: () => import('./routes/root-redirect'),
  },
  {
    element: <GuestRoute />,
    children: [
      {
        path: paths.auth.login.path,
        lazy: () => import('./routes/auth/login-route'),
      },
      {
        path: paths.auth.register.path,
        lazy: () => import('./routes/auth/register-route'),
      },
    ],
  },
  {
    path: paths.auth.emailVerification.path,
    lazy: () => import('./routes/auth/email-verification-route'),
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        path: paths.chat.path,
        lazy: () => import('./routes/chat/chat-route'),
      },
    ],
  },
  {
    path: '*',
    lazy: () => import('./routes/root-redirect'),
  },
]);

export function AppRouter() {
  return <RouterProvider router={appRouter} />;
}
