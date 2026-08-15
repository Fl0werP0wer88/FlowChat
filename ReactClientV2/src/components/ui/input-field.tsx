import type { InputHTMLAttributes } from 'react';
import type { FieldError, UseFormRegisterReturn } from 'react-hook-form';

import { cn } from '@/utils/cn';

interface InputFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'id'> {
  id: string;
  label: string;
  error?: FieldError;
  hint?: string;
  registration: UseFormRegisterReturn;
}

export function InputField({
  id,
  label,
  error,
  hint,
  registration,
  className,
  ...props
}: InputFieldProps) {
  const descriptionId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [descriptionId, errorId].filter(Boolean).join(' ') || undefined;

  return (
    <div className="grid gap-2">
      <label htmlFor={id} className="text-sm font-semibold text-slate-800">
        {label}
      </label>
      <input
        id={id}
        className={cn(
          'min-h-12 w-full rounded-2xl border bg-white/80 px-4 text-[0.95rem] text-slate-950 outline-none transition duration-200 placeholder:text-slate-400 focus:-translate-y-px focus:border-blue-600 focus:ring-4 focus:ring-blue-100',
          error ? 'border-rose-500' : 'border-slate-200',
          className,
        )}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy}
        {...registration}
        {...props}
      />
      {hint ? (
        <p id={descriptionId} className="text-xs leading-5 text-slate-500">
          {hint}
        </p>
      ) : null}
      {error ? (
        <p id={errorId} role="alert" className="text-xs font-medium leading-5 text-rose-700">
          {error.message}
        </p>
      ) : null}
    </div>
  );
}
