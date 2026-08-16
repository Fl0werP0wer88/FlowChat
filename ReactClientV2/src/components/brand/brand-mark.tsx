import { cn } from '@/utils/cn';

interface BrandMarkProps {
  className?: string;
}

export function BrandMark({ className }: BrandMarkProps) {
  return (
    <span className={cn('brand-mark', className)} aria-hidden="true">
      <svg className="brand-mark__symbol" viewBox="0 0 64 64" focusable="false">
        <g className="brand-mark__segment brand-mark__segment--one">
          <path d="M26.92 11.65A21 21 0 0 1 37.08 11.65" />
        </g>
        <g className="brand-mark__segment brand-mark__segment--two">
          <path d="M26.92 11.65A21 21 0 0 1 37.08 11.65" />
        </g>
        <g className="brand-mark__segment brand-mark__segment--three">
          <path d="M26.92 11.65A21 21 0 0 1 37.08 11.65" />
        </g>
        <g className="brand-mark__segment brand-mark__segment--four">
          <path d="M26.92 11.65A21 21 0 0 1 37.08 11.65" />
        </g>
        <g className="brand-mark__segment brand-mark__segment--five">
          <path d="M26.92 11.65A21 21 0 0 1 37.08 11.65" />
        </g>
      </svg>
    </span>
  );
}
