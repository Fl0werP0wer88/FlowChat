import { Button } from "../atoms/Button";

interface AuthActionsProps {
  pending: boolean;
  submitLabel: string;
  pendingSubmitLabel: string;
  secondaryLabel: string;
  onSecondaryClick: () => void;
}

export function AuthActions({
  pending,
  submitLabel,
  pendingSubmitLabel,
  secondaryLabel,
  onSecondaryClick
}: AuthActionsProps) {
  return (
    <div className="actions">
      <Button disabled={pending} type="submit">
        {pending ? pendingSubmitLabel : submitLabel}
      </Button>
      <Button disabled={pending} type="button" variant="link" onClick={onSecondaryClick}>
        {secondaryLabel}
      </Button>
    </div>
  );
}
