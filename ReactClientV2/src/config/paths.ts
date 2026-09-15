export const paths = {
  home: {
    path: '/',
    getHref: () => '/',
  },
  auth: {
    login: {
      path: '/login',
      getHref: (redirectTo?: string | null) =>
        `/login${redirectTo ? `?redirectTo=${encodeURIComponent(redirectTo)}` : ''}`,
    },
    register: {
      path: '/register',
      getHref: (redirectTo?: string | null) =>
        `/register${redirectTo ? `?redirectTo=${encodeURIComponent(redirectTo)}` : ''}`,
    },
    emailVerification: {
      path: '/email-verification',
      getHref: (token?: string) =>
        `/email-verification${token ? `?token=${encodeURIComponent(token)}` : ''}`,
    },
  },
  chat: {
    path: '/chat',
    getHref: () => '/chat',
  },
} as const;
