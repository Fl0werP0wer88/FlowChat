import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowRight } from 'lucide-react';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import { InputField } from '@/components/ui/input-field';
import { paths } from '@/config/paths';
import { toApiError } from '@/lib/api-error';
import { loginInputSchema, type LoginInput, type LoginNavigationState } from '@/types/auth';

import { loginUser } from '../api/login';

function getSafeRedirect(value: string | null): string {
  return value?.startsWith('/') && !value.startsWith('//') ? value : paths.chat.getHref();
}

export function LoginForm() {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const navigationState = location.state as LoginNavigationState | null;
  const redirectTo = searchParams.get('redirectTo');
  const form = useForm<LoginInput>({
    resolver: zodResolver(loginInputSchema),
    defaultValues: {
      login: navigationState?.prefillLogin ?? '',
      password: '',
    },
  });

  useEffect(() => {
    if (navigationState?.prefillLogin) {
      void navigate(`${location.pathname}${location.search}`, { replace: true, state: null });
    }
  }, [location.pathname, location.search, navigate, navigationState?.prefillLogin]);

  const submit = form.handleSubmit(async (input) => {
    try {
      await loginUser(input);
      toast.success('Welcome back.');
      navigate(getSafeRedirect(redirectTo), { replace: true });
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  });

  return (
    <div className="grid gap-6">
      <form className="grid gap-5" onSubmit={submit} noValidate>
        <InputField
          id="login"
          label="Email or FriendlyUserId"
          autoComplete="username"
          autoCapitalize="none"
          spellCheck={false}
          placeholder="you@example.com"
          registration={form.register('login')}
          error={form.formState.errors.login}
        />
        <InputField
          id="password"
          label="Password"
          type="password"
          autoComplete="current-password"
          placeholder="Enter your password"
          registration={form.register('password')}
          error={form.formState.errors.password}
        />
        <Button type="submit" isLoading={form.formState.isSubmitting} className="mt-1 w-full">
          Sign in
          <ArrowRight className="size-4" aria-hidden="true" />
        </Button>
      </form>

      <p className="m-0 text-center text-sm text-slate-600">
        New to FlowChat?{' '}
        <Link
          to={paths.auth.register.getHref(redirectTo)}
          className="font-semibold text-blue-800 underline decoration-blue-200 underline-offset-4 transition hover:decoration-blue-700 focus-visible:rounded focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-600"
        >
          Create an account
        </Link>
      </p>
    </div>
  );
}
