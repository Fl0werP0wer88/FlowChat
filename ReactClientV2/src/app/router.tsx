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
        lazy: () => import('./routes/login-route'),
      },
      {
        path: paths.auth.register.path,
        lazy: () => import('./routes/register-route'),
      },
    ],
  },
  {
    path: paths.auth.emailVerification.path,
    lazy: () => import('./routes/email-verification-route'),
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        path: paths.chat.path,
        lazy: () => import('./routes/chat-route'),
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
