import { LogOut } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';

import { BrandMark } from '@/components/brand/brand-mark';
import { Button } from '@/components/ui/button';
import { paths } from '@/config/paths';
import { logoutSession } from '@/lib/auth-session';
import { useAuthStore } from '@/stores/auth-store';

export function AppHeader() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.session?.user);

  const logout = async () => {
    try {
      await logoutSession();
    } catch {
      toast.error(
        'The server could not clear the refresh cookie. You have been signed out locally.',
      );
    } finally {
      navigate(paths.auth.login.getHref(), { replace: true });
    }
  };

  return (
    <header className="flex min-h-16 items-center justify-between gap-4 border-b border-slate-200 px-4 py-3 sm:px-6">
      <div className="flex min-w-0 items-center gap-3 text-slate-950">
        <BrandMark className="size-8" />
        <span className="font-display text-lg font-bold tracking-[-0.025em]">FlowChat</span>
      </div>

      <div className="flex min-w-0 items-center justify-end gap-2 sm:gap-3">
        <span className="hidden max-w-48 truncate text-sm font-semibold text-slate-500 sm:block">
          {user?.email ?? 'FlowChat user'}
        </span>
        <Button type="button" variant="ghost" onClick={() => void logout()}>
          <LogOut className="size-4" aria-hidden="true" />
          <span className="hidden sm:inline">Sign out</span>
        </Button>
      </div>
    </header>
  );
}
