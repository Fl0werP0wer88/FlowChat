import type { InputHTMLAttributes } from 'react';

import { cn } from '@/utils/cn';

interface InputFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'id'> {
  id: string;
  label: string;
  error?: string;
  hint?: string;
  variant?: 'default' | 'compact';
}

export function InputField({
  id,
  label,
  error,
  hint,
  variant = 'default',
  className,
  ...props
}: InputFieldProps) {
  const descriptionId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [descriptionId, errorId].filter(Boolean).join(' ') || undefined;
  const isCompact = variant === 'compact';

  return (
    <div className={cn('grid', isCompact ? 'gap-1.5' : 'gap-2')}>
      <label
        htmlFor={id}
        className={
          isCompact ? 'text-xs font-bold text-slate-700' : 'text-sm font-semibold text-slate-800'
        }
      >
        {label}
      </label>
      <input
        id={id}
        className={cn(
          isCompact
            ? 'min-h-10 w-full rounded-xl border bg-white px-3 text-sm text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-blue-600 focus:ring-2 focus:ring-blue-100 disabled:bg-slate-100 disabled:text-slate-500'
            : 'min-h-12 w-full rounded-2xl border bg-white/80 px-4 text-[0.95rem] text-slate-950 outline-none transition duration-200 placeholder:text-slate-400 focus:-translate-y-px focus:border-blue-600 focus:ring-4 focus:ring-blue-100',
          error ? 'border-rose-500' : 'border-slate-200',
          className,
        )}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy}
        {...props}
      />
      {hint ? (
        <p id={descriptionId} className="text-xs leading-5 text-slate-500">
          {hint}
        </p>
      ) : null}
      {error ? (
        <p id={errorId} role="alert" className="text-xs font-medium leading-5 text-rose-700">
          {error}
        </p>
      ) : null}
    </div>
  );
}
