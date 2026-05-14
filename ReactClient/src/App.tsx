import { Navigate, Route, Routes, useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { AuthFeature } from "./features/auth";
import { ChatFeature } from "./features/chat";
import { EmailVerificationFeature } from "./features/emailVerification";
import { useSessionRefresher } from "./hooks/useSessionRefresher";
import { AppBackgroundLayout } from "./layouts/AppBackgroundLayout";
import { useAuthStore } from "./store/authStore";
import type { AuthSession } from "./types/auth";

interface RootRedirectProps {
  isAuthenticated: boolean;
}

function RootRedirect({ isAuthenticated }: RootRedirectProps) {
  return <Navigate to={isAuthenticated ? "/chat" : "/login"} replace />;
}

interface AuthRouteProps {
  isAuthenticated: boolean;
  mode: "login" | "register";
  onLoginSuccess: (session: AuthSession) => void;
}

function AuthRoute({ isAuthenticated, mode, onLoginSuccess }: AuthRouteProps) {
  if (isAuthenticated) {
    return <Navigate to="/chat" replace />;
  }

  return <AuthFeature mode={mode} onLoginSuccess={onLoginSuccess} />;
}

interface ChatRouteProps {
  isAuthenticated: boolean;
  onLogout: () => void;
}

function ChatRoute({ isAuthenticated, onLogout }: ChatRouteProps) {
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <ChatFeature onLogout={onLogout} />;
}

function EmailVerificationRoute() {
  const [searchParams] = useSearchParams();

  return <EmailVerificationFeature token={searchParams.get("token")} />;
}

export default function App() {
  useSessionRefresher();

  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const signIn = useAuthStore((s) => s.signIn);
  const signOut = useAuthStore((s) => s.signOut);
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
          element={<ChatRoute isAuthenticated={isAuthenticated} onLogout={handleLogout} />}
        />
        <Route path="*" element={<RootRedirect isAuthenticated={isAuthenticated} />} />
      </Routes>
    </AppBackgroundLayout>
  );
}
