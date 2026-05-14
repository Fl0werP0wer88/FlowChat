import { Navigate, Route, Routes, useLocation, useSearchParams } from "react-router-dom";
import { AuthFeature } from "./features/auth";
import { ChatFeature } from "./features/chat";
import { EmailVerificationFeature } from "./features/emailVerification";
import { useSessionRefresher } from "./hooks/useSessionRefresher";
import { AppBackgroundLayout } from "./layouts/AppBackgroundLayout";
import { useAuthStore } from "./store/authStore";

interface RootRedirectProps {
  isAuthenticated: boolean;
}

function RootRedirect({ isAuthenticated }: RootRedirectProps) {
  return <Navigate to={isAuthenticated ? "/chat" : "/login"} replace />;
}

interface AuthRouteProps {
  isAuthenticated: boolean;
  mode: "login" | "register";
}

function AuthRoute({ isAuthenticated, mode }: AuthRouteProps) {
  if (isAuthenticated) {
    return <Navigate to="/chat" replace />;
  }

  return <AuthFeature mode={mode} />;
}

interface ChatRouteProps {
  isAuthenticated: boolean;
}

function ChatRoute({ isAuthenticated }: ChatRouteProps) {
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <ChatFeature />;
}

function EmailVerificationRoute() {
  const [searchParams] = useSearchParams();

  return <EmailVerificationFeature token={searchParams.get("token")} />;
}

export default function App() {
  useSessionRefresher();

  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  return (
    <AppBackgroundLayout>
      <Routes>
        <Route path="/" element={<RootRedirect isAuthenticated={isAuthenticated} />} />
        <Route path="/login" element={<AuthRoute isAuthenticated={isAuthenticated} mode="login" />} />
        <Route path="/register" element={<AuthRoute isAuthenticated={isAuthenticated} mode="register" />} />
        <Route path="/email-verification" element={<EmailVerificationRoute />} />
        <Route path="/chat" element={<ChatRoute isAuthenticated={isAuthenticated} />} />
        <Route path="*" element={<RootRedirect isAuthenticated={isAuthenticated} />} />
      </Routes>
    </AppBackgroundLayout>
  );
}
