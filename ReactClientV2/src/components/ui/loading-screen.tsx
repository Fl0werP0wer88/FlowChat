import { LoaderCircle } from 'lucide-react';

import { BrandMark } from '@/components/brand/brand-mark';

interface LoadingScreenProps {
  label: string;
}

export function LoadingScreen({ label }: LoadingScreenProps) {
  return (
    <main
      className="grid min-h-svh place-items-center bg-slate-50 px-6"
      data-testid="loading-screen"
    >
      <div className="grid justify-items-center gap-4 text-center">
        <BrandMark className="scale-110" />
        <div className="flex items-center gap-2 text-sm font-medium text-slate-600">
          <LoaderCircle className="size-4 animate-spin text-blue-700" aria-hidden="true" />
          <span>{label}</span>
        </div>
      </div>
    </main>
  );
}
