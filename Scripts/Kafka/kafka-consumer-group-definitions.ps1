#requires -Version 5.1

function Get-ConsumerGroupDefinitions {
  return @(
    "auth-service",
    "auth-service-retry",
    "notification-service",
    "notification-service-retry",
    "realtime-service",
    "realtime-service-retry",
    "presence-service-realtime-connection",
    "presence-service-realtime-connection-retry",
    "socialgraph-service",
    "socialgraph-service-retry",
    "userprofile-service",
    "userprofile-service-retry"
  )
}
