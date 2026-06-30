import type { PropsWithChildren } from "react";

export function AppBackgroundTemplate({ children }: PropsWithChildren) {
  return (
    <main className="app-container">
      <div className="background-glow background-glow-left" />
      <div className="background-glow background-glow-right" />
      {children}
    </main>
  );
}
