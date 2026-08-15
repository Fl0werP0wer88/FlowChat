import { AuthLayout } from '@/components/layouts/auth-layout';
import { RegisterForm } from '@/features/auth/components/register-form';
import { useDocumentTitle } from '@/hooks/use-document-title';

export function Component() {
  useDocumentTitle('Create account');

  return (
    <AuthLayout
      eyebrow="Join FlowChat"
      title="Create your account"
      description="Choose your identity, then confirm your email to start."
    >
      <RegisterForm />
    </AuthLayout>
  );
}
