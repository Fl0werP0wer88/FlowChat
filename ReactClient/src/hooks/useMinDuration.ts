import { useEffect, useRef, useState } from "react";

export function useMinDuration(active: boolean, minMs: number): boolean {
  const [visible, setVisible] = useState(active);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (active) {
      if (timerRef.current !== null) clearTimeout(timerRef.current);
      setVisible(true);
    } else {
      timerRef.current = setTimeout(() => setVisible(false), minMs);
    }

    return () => {
      if (timerRef.current !== null) clearTimeout(timerRef.current);
    };
  }, [active, minMs]);

  return visible;
}
