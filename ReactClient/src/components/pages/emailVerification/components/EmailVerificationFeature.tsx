import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "../../../UI/atoms/Button";
import { AlertMessage } from "../../../UI/molecules/AlertMessage";
import { AuthTemplate } from "../../../templates";
import type { AuthNotice } from "../../../../types/auth";
import { confirmEmailVerification } from "../../../../api/userProfileApi";

type VerificationStatus = "pending" | "success" | "error";

interface EmailVerificationFeatureProps {
  token: string | null;
}

export function EmailVerificationFeature({ token }: EmailVerificationFeatureProps) {
  const navigate = useNavigate();
  const [status, setStatus] = useState<VerificationStatus>(token ? "pending" : "error");
  const [notice, setNotice] = useState<AuthNotice | null>(
    token
      ? { kind: "info", message: "Trwa potwierdzanie adresu email..." }
      : { kind: "error", message: "Link do potwierdzenia nie zawiera tokenu." },
  );
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    if (!token) {
      return;
    }

    let disposed = false;

    setStatus("pending");
    setNotice({ kind: "info", message: "Trwa potwierdzanie adresu email..." });

    void confirmEmailVerification(token)
      .then(() => {
        if (disposed) {
          return;
        }

        setStatus("success");
        setNotice({ kind: "info", message: "Email zostal potwierdzony. Mozesz wrocic do logowania." });
      })
      .catch((error: unknown) => {
        if (disposed) {
          return;
        }

        const message = error instanceof Error ? error.message : "Nie udalo sie potwierdzic adresu email.";
        setStatus("error");
        setNotice({ kind: "error", message });
      });

    return () => {
      disposed = true;
    };
  }, [attempt, token]);

  const navigateToLogin = () => {
    navigate("/login");
  };

  const retryConfirmation = () => {
    if (!token) {
      return;
    }

    setAttempt((current) => current + 1);
  };

  return (
    <AuthTemplate
      title="Potwierdzenie emaila"
      subtitle="Finalizujemy aktywacje adresu email dla Twojego konta FlowChat."
    >
      <div className="verification-panel">
        <p className="verification-status">
          {status === "pending" && "Sprawdzamy token i potwierdzamy adres email."}
          {status === "success" && "Wszystko gotowe. Konto moze sie teraz zalogowac."}
          {status === "error" && "Nie udalo sie zakonczyc potwierdzenia."}
        </p>
        <AlertMessage notice={notice} />
        <div className="actions verification-actions">
          <Button type="button" onClick={navigateToLogin}>
            Przejdz do logowania
          </Button>
          {status === "error" && token
            ? (
              <Button type="button" variant="secondary" onClick={retryConfirmation}>
                Sprobuj ponownie
              </Button>
            )
            : null}
        </div>
      </div>
    </AuthTemplate>
  );
}
