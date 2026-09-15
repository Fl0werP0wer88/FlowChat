import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowRight } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import { InputField } from '@/components/ui/input-field';
import { paths } from '@/config/paths';
import { toApiError } from '@/lib/api-error';
import {
  registerInputSchema,
  type NormalizedRegisterInput,
  type RegisterInput,
} from '@/types/auth';

import { registerUser } from '../api/register';

export function RegisterForm() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const redirectTo = searchParams.get('redirectTo');
  const form = useForm<RegisterInput, unknown, NormalizedRegisterInput>({
    resolver: zodResolver(registerInputSchema),
    defaultValues: {
      email: '',
      friendlyUserId: '',
      password: '',
      firstName: '',
      lastName: '',
      organization: '',
    },
  });

  const submit = form.handleSubmit(async (input) => {
    try {
      await registerUser(input);
      toast.success('Account created. Confirm your email, then sign in.');
      navigate(paths.auth.login.getHref(redirectTo), {
        replace: true,
        state: { prefillLogin: input.email },
      });
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  });

  return (
    <div className="grid gap-6">
      <form className="grid gap-5" onSubmit={submit} noValidate>
        <InputField
          {...form.register('email')}
          id="register-email"
          label="Email"
          type="email"
          autoComplete="email"
          autoCapitalize="none"
          placeholder="you@example.com"
          error={form.formState.errors.email?.message}
        />
        <InputField
          {...form.register('friendlyUserId')}
          id="register-friendly-id"
          label="FriendlyUserId"
          autoComplete="username"
          autoCapitalize="none"
          spellCheck={false}
          placeholder="alex.morgan"
          hint="Lowercase letters, numbers, periods, and hyphens."
          error={form.formState.errors.friendlyUserId?.message}
        />
        <div className="grid gap-5 sm:grid-cols-2">
          <InputField
            {...form.register('firstName')}
            id="register-first-name"
            label="First name"
            autoComplete="given-name"
            placeholder="Alex"
            error={form.formState.errors.firstName?.message}
          />
          <InputField
            {...form.register('lastName')}
            id="register-last-name"
            label="Last name"
            autoComplete="family-name"
            placeholder="Morgan"
            error={form.formState.errors.lastName?.message}
          />
        </div>
        <InputField
          {...form.register('organization')}
          id="register-organization"
          label="Organization"
          autoComplete="organization"
          placeholder="Optional"
          error={form.formState.errors.organization?.message}
        />
        <InputField
          {...form.register('password')}
          id="register-password"
          label="Password"
          type="password"
          autoComplete="new-password"
          placeholder="At least 8 characters"
          error={form.formState.errors.password?.message}
        />
        <Button type="submit" isLoading={form.formState.isSubmitting} className="mt-1 w-full">
          Create account
          <ArrowRight className="size-4" aria-hidden="true" />
        </Button>
      </form>

      <p className="m-0 text-center text-sm text-slate-600">
        Already have an account?{' '}
        <Link
          to={paths.auth.login.getHref(redirectTo)}
          className="font-semibold text-blue-800 underline decoration-blue-200 underline-offset-4 transition hover:decoration-blue-700 focus-visible:rounded focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-600"
        >
          Sign in
        </Link>
      </p>
    </div>
  );
}
