import { describe, expect, it, vi } from "vitest";
import {
  fetchWithRetry,
  runMessageSyncSingleFlight,
} from "./useConversationMessageSync";

describe("message synchronization orchestration", () => {
  it("retries three times with backoff", async () => {
    vi.useFakeTimers();
    const operation = vi.fn()
      .mockRejectedValueOnce(new Error("first"))
      .mockRejectedValueOnce(new Error("second"))
      .mockRejectedValueOnce(new Error("third"))
      .mockResolvedValue({ throughSequenceNum: 4 });

    const resultPromise = fetchWithRetry(operation);
    await vi.runAllTimersAsync();

    await expect(resultPromise).resolves.toEqual({ throughSequenceNum: 4 });
    expect(operation).toHaveBeenCalledTimes(4);
    vi.useRealTimers();
  });

  it("uses one in-flight request per conversation", async () => {
    let resolve!: (value: number) => void;
    const operation = vi.fn(() => new Promise<number>((done) => {
      resolve = done;
    }));

    const first = runMessageSyncSingleFlight("conversation", operation);
    const second = runMessageSyncSingleFlight("conversation", operation);
    resolve(12);

    await expect(first).resolves.toBe(12);
    await expect(second).resolves.toBe(12);
    expect(operation).toHaveBeenCalledOnce();
  });
});
