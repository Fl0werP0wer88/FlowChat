import { HubConnectionState, type HubConnection } from '@microsoft/signalr';

import { useAuthStore } from '@/stores/auth-store';

import { createRealtimeClient, resolveRealtimeAccessToken } from '../realtime-client';

function createConnectionMock() {
  let state = HubConnectionState.Disconnected;
  let reconnectedHandler: (() => void) | undefined;
  let closeHandler: (() => void) | undefined;
  const start = vi.fn(async () => {
    state = HubConnectionState.Connected;
  });
  const stop = vi.fn(async () => {
    state = HubConnectionState.Disconnected;
  });
  const connection = {
    get state() {
      return state;
    },
    start,
    stop,
    on: vi.fn(),
    off: vi.fn(),
    onreconnected: vi.fn((handler: () => void) => {
      reconnectedHandler = handler;
    }),
    onclose: vi.fn((handler: () => void) => {
      closeHandler = handler;
    }),
  } as unknown as HubConnection;

  return {
    connection,
    start,
    stop,
    setState: (nextState: HubConnectionState) => {
      state = nextState;
    },
    reconnect: () => reconnectedHandler?.(),
    close: () => closeHandler?.(),
  };
}

describe('realtime client', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('starts only one connection across a Strict Mode cleanup cycle', async () => {
    const mock = createConnectionMock();
    const client = createRealtimeClient(mock.connection);

    const firstStart = client.start();
    client.stop();
    const secondStart = client.start();

    await Promise.all([firstStart, secondStart]);
    await vi.runAllTimersAsync();

    expect(mock.start).toHaveBeenCalledTimes(1);
    expect(mock.stop).not.toHaveBeenCalled();
  });

  it('retries a failed initial connection while it is still requested', async () => {
    const mock = createConnectionMock();
    mock.start
      .mockRejectedValueOnce(new Error('Unavailable'))
      .mockImplementationOnce(async () => mock.setState(HubConnectionState.Connected));
    const client = createRealtimeClient(mock.connection, 1_000);

    await client.start();
    await vi.advanceTimersByTimeAsync(1_000);

    expect(mock.start).toHaveBeenCalledTimes(2);
  });

  it('cancels retries and stops after logout', async () => {
    const mock = createConnectionMock();
    const client = createRealtimeClient(mock.connection, 1_000);

    await client.start();
    client.stop();
    await vi.runAllTimersAsync();

    expect(mock.stop).toHaveBeenCalledTimes(1);
    await vi.advanceTimersByTimeAsync(1_000);
    expect(mock.start).toHaveBeenCalledTimes(1);
  });

  it('uses the latest access token from the in-memory session', () => {
    useAuthStore.setState({
      session: {
        accessToken: 'first-token',
        expiresAtUtc: '2026-08-18T12:00:00+00:00',
        user: { id: 'user-id', email: null, friendlyUserId: 'alex', roles: [] },
      },
    });
    expect(resolveRealtimeAccessToken()).toBe('first-token');

    useAuthStore.getState().setSession({
      accessToken: 'refreshed-token',
      expiresAtUtc: '2026-08-18T13:00:00+00:00',
      user: { id: 'user-id', email: null, friendlyUserId: 'alex', roles: [] },
    });

    expect(resolveRealtimeAccessToken()).toBe('refreshed-token');
  });

  it('removes event and reconnect handlers when unsubscribed', () => {
    const mock = createConnectionMock();
    const client = createRealtimeClient(mock.connection);
    const eventHandler = vi.fn();
    const reconnectedHandler = vi.fn();

    const unsubscribeEvent = client.subscribe('PresenceChanged', eventHandler);
    const unsubscribeReconnected = client.subscribeToReconnected(reconnectedHandler);
    unsubscribeEvent();
    unsubscribeReconnected();
    mock.reconnect();

    expect(mock.connection.on).toHaveBeenCalledWith('PresenceChanged', eventHandler);
    expect(mock.connection.off).toHaveBeenCalledWith('PresenceChanged', eventHandler);
    expect(reconnectedHandler).not.toHaveBeenCalled();
  });
});
