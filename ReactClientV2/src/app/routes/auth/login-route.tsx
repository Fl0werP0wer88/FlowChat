import { AuthLayout } from '@/components/layouts/auth-layout';
import { AboutCarousel } from '@/features/about/components/about-carousel';
import { LoginForm } from '@/features/auth/components/login-form';
import { useDocumentTitle } from '@/hooks/use-document-title';

export function Component() {
  useDocumentTitle('Sign in');

  return (
    <AuthLayout
      eyebrow="Welcome back"
      title="Sign in to FlowChat"
      description="Use your email or FriendlyUserId to continue."
      brandContent={<AboutCarousel />}
    >
      <LoginForm />
    </AuthLayout>
  );
}
