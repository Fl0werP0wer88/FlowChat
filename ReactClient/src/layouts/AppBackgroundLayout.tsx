import type { PropsWithChildren } from "react";

export function AppBackgroundLayout({ children }: PropsWithChildren) {
  return (
    <main className="app-container">
      <div className="background-glow background-glow-left" />
      <div className="background-glow background-glow-right" />
      {children}
    </main>
  );
}
