import { useEffect, useEffectEvent, useRef, useState } from "react";
import { changePresenceStatus, fetchPresencePreferences } from "../api";
import type { ManualUserStatus, UserStatus } from "../../../types/realtime";

const afkTimeoutMs = 2 * 60 * 1000;
const mouseActivityEvents: Array<keyof WindowEventMap> = ["mousemove", "mousedown", "wheel"];

interface UsePresenceStatusResult {
  changeManualPresenceStatus: (status: ManualUserStatus) => Promise<void>;
  currentStatus: UserStatus;
  errorMessage: string | null;
  isUpdatingStatus: boolean;
  preferredStatus: UserStatus | null;
}

function resolveErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "Nie udalo sie zaktualizowac statusu Presence.";
}

export function usePresenceStatus(accessToken: string | null): UsePresenceStatusResult {
  const [preferredStatus, setPreferredStatus] = useState<UserStatus | null>(null);
  const [currentStatus, setCurrentStatus] = useState<UserStatus>("Active");
  const [isUpdatingStatus, setIsUpdatingStatus] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isAfkEnabled, setIsAfkEnabled] = useState(true);
  const afkTimeoutRef = useRef<number | null>(null);
  const currentStatusRef = useRef<UserStatus>("Active");
  const preferredStatusRef = useRef<UserStatus | null>(null);
  const isAfkEnabledRef = useRef(true);
  const isStatusUpdateInFlightRef = useRef(false);

  const clearAfkTimeout = useEffectEvent(() => {
    if (afkTimeoutRef.current !== null) {
      window.clearTimeout(afkTimeoutRef.current);
      afkTimeoutRef.current = null;
    }
  });

  const applyLocalPresenceState = useEffectEvent((
    nextCurrentStatus: UserStatus,
    nextPreferredStatus: UserStatus | null,
    nextIsAfkEnabled: boolean,
  ) => {
    currentStatusRef.current = nextCurrentStatus;
    preferredStatusRef.current = nextPreferredStatus;
    isAfkEnabledRef.current = nextIsAfkEnabled;

    setCurrentStatus(nextCurrentStatus);
    setPreferredStatus(nextPreferredStatus);
    setIsAfkEnabled(nextIsAfkEnabled);
  });

  const performStatusChange = useEffectEvent(async (
    nextStatus: UserStatus,
    nextPreferredStatus: UserStatus | null,
    nextIsAfkEnabled: boolean,
  ) => {
    if (!accessToken || isStatusUpdateInFlightRef.current) {
      return;
    }

    const previousCurrentStatus = currentStatusRef.current;
    const previousPreferredStatus = preferredStatusRef.current;
    const previousIsAfkEnabled = isAfkEnabledRef.current;

    clearAfkTimeout();
    applyLocalPresenceState(nextStatus, nextPreferredStatus, nextIsAfkEnabled);
    setErrorMessage(null);
    setIsUpdatingStatus(true);
    isStatusUpdateInFlightRef.current = true;

    try {
      await changePresenceStatus(nextStatus, accessToken);
    } catch (error) {
      applyLocalPresenceState(previousCurrentStatus, previousPreferredStatus, previousIsAfkEnabled);
      setErrorMessage(resolveErrorMessage(error));
    } finally {
      isStatusUpdateInFlightRef.current = false;
      setIsUpdatingStatus(false);
    }
  });

  const applyPresencePreferences = useEffectEvent((preferredStatus: UserStatus | null) => {
    const nextPreferredStatus = preferredStatus;
    const nextCurrentStatus = nextPreferredStatus ?? "Active";

    clearAfkTimeout();
    applyLocalPresenceState(nextCurrentStatus, nextPreferredStatus, nextCurrentStatus === "Active");
    setErrorMessage(null);
  });

  const changeManualPresenceStatus = useEffectEvent(async (status: ManualUserStatus) => {
    await performStatusChange(
      status,
      status === "Active" ? null : status,
      status === "Active",
    );
  });

  const handleMouseActivity = useEffectEvent(() => {
    if (!accessToken || !isAfkEnabledRef.current || isStatusUpdateInFlightRef.current) {
      return;
    }

    if (currentStatusRef.current === "AFK") {
      void performStatusChange("Active", preferredStatusRef.current, true);
      return;
    }

    if (currentStatusRef.current !== "Active") {
      return;
    }

    clearAfkTimeout();
    afkTimeoutRef.current = window.setTimeout(() => {
      void performStatusChange("AFK", preferredStatusRef.current, true);
    }, afkTimeoutMs);
  });

  useEffect(() => {
    if (!accessToken) {
      clearAfkTimeout();
      isStatusUpdateInFlightRef.current = false;
      applyLocalPresenceState("Active", null, true);
      setErrorMessage(null);
      setIsUpdatingStatus(false);
      return;
    }

    let isActive = true;

    const loadPresencePreferences = async () => {
      try {
        const preferences = await fetchPresencePreferences(accessToken);
        if (isActive && !isStatusUpdateInFlightRef.current) {
          applyPresencePreferences(preferences);
        }
      } catch (error) {
        if (isActive) {
          setErrorMessage(resolveErrorMessage(error));
        }
      }
    };

    void loadPresencePreferences();

    const unsubscribe = mouseActivityEvents.map((eventName) => {
      const handler = () => {
        handleMouseActivity();
      };

      window.addEventListener(eventName, handler, { passive: true });
      return () => window.removeEventListener(eventName, handler);
    });

    return () => {
      isActive = false;
      clearAfkTimeout();
      unsubscribe.forEach((dispose) => dispose());
    };
  }, [accessToken]);

  useEffect(() => {
    clearAfkTimeout();

    if (!accessToken || !isAfkEnabled || currentStatus !== "Active" || isStatusUpdateInFlightRef.current) {
      return;
    }

    afkTimeoutRef.current = window.setTimeout(() => {
      void performStatusChange("AFK", preferredStatusRef.current, true);
    }, afkTimeoutMs);

    return () => {
      clearAfkTimeout();
    };
  }, [accessToken, clearAfkTimeout, currentStatus, isAfkEnabled, performStatusChange]);

  return {
    changeManualPresenceStatus,
    currentStatus,
    errorMessage,
    isUpdatingStatus,
    preferredStatus,
  };
}
