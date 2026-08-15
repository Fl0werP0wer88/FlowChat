import { useQuery } from '@tanstack/react-query';
import { AlertCircle, ArrowRight, CheckCircle2, LoaderCircle, RotateCcw } from 'lucide-react';
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import { paths } from '@/config/paths';
import { toApiError } from '@/lib/api-error';

import { confirmEmail } from '../api/confirm-email';

interface EmailVerificationStatusProps {
  token: string | null;
}

export function EmailVerificationStatus({ token }: EmailVerificationStatusProps) {
  const navigate = useNavigate();
  const verification = useQuery({
    queryKey: ['email-verification', token],
    queryFn: () => confirmEmail(token ?? ''),
    enabled: Boolean(token),
    staleTime: Number.POSITIVE_INFINITY,
    retry: false,
  });

  useEffect(() => {
    if (!token) {
      toast.error('This verification link does not contain a token.', {
        id: 'email-verification-missing-token',
      });
    } else if (verification.isSuccess) {
      toast.success('Your email has been confirmed.', {
        id: `email-verification-success-${token}`,
      });
    } else if (verification.isError) {
      toast.error(toApiError(verification.error).message, {
        id: `email-verification-error-${token}`,
      });
    }
  }, [token, verification.error, verification.isError, verification.isSuccess]);

  const isMissing = !token;
  const isPending = Boolean(token) && verification.isPending;
  const isSuccess = verification.isSuccess;
  const isError = isMissing || verification.isError;

  return (
    <div className="grid gap-6" aria-live="polite">
      <div className="flex items-start gap-4 rounded-3xl border border-slate-200 bg-white/70 p-5 shadow-[0_18px_50px_-38px_rgba(17,40,90,0.55)]">
        <span
          className={`grid size-11 shrink-0 place-items-center rounded-full ${
            isSuccess
              ? 'bg-emerald-100 text-emerald-700'
              : isError
                ? 'bg-rose-100 text-rose-700'
                : 'bg-blue-100 text-blue-700'
          }`}
        >
          {isPending ? <LoaderCircle className="animate-spin" aria-hidden="true" /> : null}
          {isSuccess ? <CheckCircle2 aria-hidden="true" /> : null}
          {isError ? <AlertCircle aria-hidden="true" /> : null}
        </span>
        <div className="grid gap-1.5">
          <h2 className="font-display m-0 text-lg font-semibold text-slate-950">
            {isPending ? 'Confirming your email' : null}
            {isSuccess ? 'Email confirmed' : null}
            {isMissing ? 'Verification token missing' : null}
            {verification.isError ? 'Verification failed' : null}
          </h2>
          <p className="m-0 text-sm leading-6 text-slate-600">
            {isPending ? 'We are checking this link and activating your email address.' : null}
            {isSuccess ? 'Everything is ready. You can now sign in to FlowChat.' : null}
            {isMissing
              ? 'Open the complete link from your verification email and try again.'
              : null}
            {verification.isError ? toApiError(verification.error).message : null}
          </p>
        </div>
      </div>

      <div className="flex flex-col gap-3 sm:flex-row">
        <Button type="button" onClick={() => navigate(paths.auth.login.getHref())}>
          Go to sign in
          <ArrowRight className="size-4" aria-hidden="true" />
        </Button>
        {verification.isError && token ? (
          <Button type="button" variant="secondary" onClick={() => void verification.refetch()}>
            <RotateCcw className="size-4" aria-hidden="true" />
            Try again
          </Button>
        ) : null}
      </div>
    </div>
  );
}
