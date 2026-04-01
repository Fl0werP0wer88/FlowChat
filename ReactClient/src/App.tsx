import { AuthFeature } from "./features/auth";
import { ChatFeature } from "./features/chat";
import { EmailVerificationFeature } from "./features/emailVerification";
import { useSessionState } from "./hooks/useSessionState";
import { AppBackgroundLayout } from "./layouts/AppBackgroundLayout";
import type { AppScreen } from "./types/common";

function resolveAppScreen(pathname: string, isAuthenticated: boolean): AppScreen {
  const normalizedPath = pathname.replace(/\/+$/, "") || "/";
  if (normalizedPath === "/email-verification") {
    return "emailVerification";
  }

  return isAuthenticated ? "chat" : "auth";
}

export default function App() {
  const { session, isAuthenticated, signIn, signOut } = useSessionState();
  const screen = resolveAppScreen(window.location.pathname, isAuthenticated);
  const verificationToken = new URLSearchParams(window.location.search).get("token");

  return (
    <AppBackgroundLayout>
      {screen === "emailVerification"
        ? <EmailVerificationFeature token={verificationToken} />
        : screen === "auth"
        ? <AuthFeature onLoginSuccess={signIn} />
        : (
          <ChatFeature
            accessToken={session.accessToken ?? ""}
            userLogin={session.login ?? "Uzytkownik"}
            onLogout={signOut}
          />
        )}
    </AppBackgroundLayout>
  );
}
