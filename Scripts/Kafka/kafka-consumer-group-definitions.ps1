#requires -Version 5.1

function Get-ConsumerGroupDefinitions {
  return @(
    "auth-service",
    "auth-service-retry",
    "notification-service",
    "notification-service-retry",
    "realtime-service",
    "realtime-service-retry",
    "presence-service",
    "presence-service-duet-conversation-contact-retry",
    "presence-service-realtime-connection",
    "presence-service-realtime-connection-retry",
    "userprofile-service",
    "userprofile-service-retry"
  )
}
