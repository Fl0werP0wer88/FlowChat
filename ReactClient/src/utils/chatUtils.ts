export function calculateUnreadCount(currentMsgSeqNum: number, lastReadMsgSeqNum: number): number {
  return Math.max(0, currentMsgSeqNum - lastReadMsgSeqNum);
}
