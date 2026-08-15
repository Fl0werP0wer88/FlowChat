import { cn } from '@/utils/cn';

interface BrandMarkProps {
  className?: string;
}

export function BrandMark({ className }: BrandMarkProps) {
  return (
    <span className={cn('brand-mark', className)} aria-hidden="true">
      <span className="brand-mark__ring" />
      <span className="brand-mark__core" />
      <span className="brand-mark__tail" />
    </span>
  );
}
