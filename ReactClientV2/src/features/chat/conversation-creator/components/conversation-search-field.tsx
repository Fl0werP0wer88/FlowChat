interface ConversationSearchFieldProps {
  id: string;
  label: string;
  value: string;
  maximumLength: number;
  disabled: boolean;
  onChange: (value: string) => void;
}

export function ConversationSearchField({
  id,
  label,
  value,
  maximumLength,
  disabled,
  onChange,
}: ConversationSearchFieldProps) {
  return (
    <div className="grid gap-1.5">
      <label className="text-xs font-bold text-slate-700" htmlFor={id}>
        {label}
      </label>
      <input
        className="min-h-10 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-blue-600 focus:ring-2 focus:ring-blue-100 disabled:bg-slate-100 disabled:text-slate-500"
        id={id}
        maxLength={maximumLength}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
        type="search"
        value={value}
      />
    </div>
  );
}
