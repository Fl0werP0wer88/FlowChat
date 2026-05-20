import { Navigate, Route, Routes, useLocation, useSearchParams } from "react-router-dom";
import { AuthFeature } from "./features/auth";
import { ChatFeature } from "./features/conversations/duet";
import { EmailVerificationFeature } from "./features/emailVerification";
import { useSessionRefresher } from "./hooks/useSessionRefresher";
import { AppBackgroundLayout } from "./layouts/AppBackgroundLayout";
import { useAuthStore } from "./store/authStore";

function RootRedirect() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  return <Navigate to={isAuthenticated ? "/chat" : "/login"} replace />;
}

function AuthRoute({ mode }: { mode: "login" | "register" }) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  if (isAuthenticated) {
    return <Navigate to="/chat" replace />;
  }

  return <AuthFeature mode={mode} />;
}

function ChatRoute() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
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

  return (
    <AppBackgroundLayout>
      <Routes>
        <Route path="/" element={<RootRedirect />} />
        <Route path="/login" element={<AuthRoute mode="login" />} />
        <Route path="/register" element={<AuthRoute mode="register" />} />
        <Route path="/email-verification" element={<EmailVerificationRoute />} />
        <Route path="/chat" element={<ChatRoute />} />
        <Route path="*" element={<RootRedirect />} />
      </Routes>
    </AppBackgroundLayout>
  );
}
