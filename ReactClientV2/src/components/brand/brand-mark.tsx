import { cn } from '@/utils/cn';

interface BrandMarkProps {
  className?: string;
}

export function BrandMark({ className }: BrandMarkProps) {
  return (
    <span className={cn('brand-mark', className)} aria-hidden="true">
      <svg className="brand-mark__symbol" viewBox="0 0 64 64" focusable="false">
        <g className="brand-mark__segment brand-mark__segment--one">
          <path d="M32 1L42 11L32 21L22 11Z" />
        </g>
        <g className="brand-mark__segment brand-mark__segment--two">
          <path d="M32 1L42 11L32 21L22 11Z" />
        </g>
        <g className="brand-mark__segment brand-mark__segment--three">
          <path d="M32 1L42 11L32 21L22 11Z" />
        </g>
        <g className="brand-mark__segment brand-mark__segment--four">
          <path d="M32 1L42 11L32 21L22 11Z" />
        </g>
        <g className="brand-mark__segment brand-mark__segment--five">
          <path d="M32 1L42 11L32 21L22 11Z" />
        </g>
      </svg>
    </span>
  );
}
