import { useSearchParams } from 'react-router-dom';

import { AuthLayout } from '@/app/layouts/auth-layout';
import { EmailVerificationStatus } from '@/features/email-verification/components/email-verification-status';
import { useDocumentTitle } from '@/hooks/use-document-title';

export function Component() {
  useDocumentTitle('Verify email');
  const [searchParams] = useSearchParams();

  return (
    <AuthLayout
      eyebrow="Account activation"
      title="Verify your email"
      description="One final check keeps your FlowChat identity secure."
    >
      <EmailVerificationStatus token={searchParams.get('token')} />
    </AuthLayout>
  );
}
