import { AuthLayout } from '@/app/layouts/auth-layout';
import { LoginForm } from '@/features/auth/components/login-form';
import { useDocumentTitle } from '@/hooks/use-document-title';

export function Component() {
  useDocumentTitle('Sign in');

  return (
    <AuthLayout
      eyebrow="Welcome back"
      title="Sign in to FlowChat"
      description="Use your email or FriendlyUserId to continue."
    >
      <LoginForm />
    </AuthLayout>
  );
}
