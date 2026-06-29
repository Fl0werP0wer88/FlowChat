interface UsersPickerFooterProps {
  disabled: boolean;
  isPending: boolean;
  label: string;
  onClick: () => void;
}

export function UsersPickerFooter({
  disabled,
  isPending,
  label,
  onClick,
}: UsersPickerFooterProps) {
  return (
    <div className="users-picker__footer">
      <button
        className="users-picker__confirm-button"
        disabled={disabled}
        onClick={onClick}
        type="button"
      >
        <span aria-hidden="true" className="material-symbols-rounded">
          {isPending ? "progress_activity" : "group_add"}
        </span>
        <span>{label}</span>
      </button>
    </div>
  );
}
