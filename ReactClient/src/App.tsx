import { AuthFeature } from "./features/auth";
import { ChatFeature } from "./features/chat";
import { useSessionState } from "./hooks/useSessionState";
import { AppBackgroundLayout } from "./layouts/AppBackgroundLayout";
import type { AppScreen } from "./types/common";

export default function App() {
  const { session, isAuthenticated, signIn, signOut } = useSessionState();
  const screen: AppScreen = isAuthenticated ? "chat" : "auth";

  return (
    <AppBackgroundLayout>
      {screen === "auth" ? (
        <AuthFeature onLoginSuccess={signIn} />
      ) : (
        <ChatFeature userLogin={session.login ?? "Uzytkownik"} onLogout={signOut} />
      )}
    </AppBackgroundLayout>
  );
}
