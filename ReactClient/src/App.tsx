import { Navigate, Route, Routes, useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { AuthFeature } from "./features/auth";
import { ChatFeature } from "./features/chat";
import { EmailVerificationFeature } from "./features/emailVerification";
import { useSessionState } from "./hooks/useSessionState";
import { AppBackgroundLayout } from "./layouts/AppBackgroundLayout";
import type { AuthMode, AuthSession } from "./types/auth";

interface RootRedirectProps {
  isAuthenticated: boolean;
}

function RootRedirect({ isAuthenticated }: RootRedirectProps) {
  return <Navigate to={isAuthenticated ? "/chat" : "/login"} replace />;
}

interface AuthRouteProps {
  isAuthenticated: boolean;
  mode: AuthMode;
  onLoginSuccess: (session: AuthSession) => void;
}

function AuthRoute({ isAuthenticated, mode, onLoginSuccess }: AuthRouteProps) {
  if (isAuthenticated) {
    return <Navigate to="/chat" replace />;
  }

  return <AuthFeature mode={mode} onLoginSuccess={onLoginSuccess} />;
}

interface ChatRouteProps {
  accessToken: string | null;
  isAuthenticated: boolean;
  onLogout: () => void;
  userLogin: string | null;
}

function ChatRoute({ accessToken, isAuthenticated, onLogout, userLogin }: ChatRouteProps) {
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return (
    <ChatFeature
      accessToken={accessToken ?? ""}
      userLogin={userLogin ?? "Uzytkownik"}
      onLogout={onLogout}
    />
  );
}

function EmailVerificationRoute() {
  const [searchParams] = useSearchParams();

  return <EmailVerificationFeature token={searchParams.get("token")} />;
}

export default function App() {
  const { session, isAuthenticated, signIn, signOut } = useSessionState();
  const navigate = useNavigate();

  const handleLoginSuccess = (nextSession: AuthSession) => {
    signIn(nextSession);
    navigate("/chat", { replace: true });
  };

  const handleLogout = () => {
    signOut();
    navigate("/login", { replace: true });
  };

  return (
    <AppBackgroundLayout>
      <Routes>
        <Route path="/" element={<RootRedirect isAuthenticated={isAuthenticated} />} />
        <Route
          path="/login"
          element={<AuthRoute isAuthenticated={isAuthenticated} mode="login" onLoginSuccess={handleLoginSuccess} />}
        />
        <Route
          path="/register"
          element={<AuthRoute isAuthenticated={isAuthenticated} mode="register" onLoginSuccess={handleLoginSuccess} />}
        />
        <Route path="/email-verification" element={<EmailVerificationRoute />} />
        <Route
          path="/chat"
          element={(
            <ChatRoute
              accessToken={session.accessToken}
              isAuthenticated={isAuthenticated}
              userLogin={session.login}
              onLogout={handleLogout}
            />
          )}
        />
        <Route path="*" element={<RootRedirect isAuthenticated={isAuthenticated} />} />
      </Routes>
    </AppBackgroundLayout>
  );
}
