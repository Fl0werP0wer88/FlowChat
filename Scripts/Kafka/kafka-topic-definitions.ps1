#requires -Version 5.1

function Get-TopicDefinitions {
  return @(
    @{
      name = "dev.flowchat.identity.user.v1"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.identity.user.v1.userprofile-service.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.identity.user.v1.userprofile-service.retry.20s"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.identity.user.v1.userprofile-service.retry.60s"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.identity.user.v1.userprofile-service.retry.300s"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.identity.user.v1.userprofile-service.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "dev.flowchat.user-profile.user-profile.v1"
      partitions = 1
      rf = 1
      config = @{
        "min.insync.replicas" = "1"
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.user-profile.user-profile.v1.auth-service.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.user-profile.user-profile.v1.auth-service.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "dev.flowchat.user-profile.user-profile-projection.v1"
      partitions = 1
      rf = 1
      config = @{
        "min.insync.replicas" = "1"
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.user-profile.user-profile-projection.v1.chat-service.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.user-profile.user-profile-projection.v1.chat-service.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "dev.flowchat.user-profile.user-profile-projection.v1.socialgraph-service.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.user-profile.user-profile-projection.v1.socialgraph-service.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "dev.flowchat.notification.email.v1"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.notification.email.v1.notification-service.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.notification.email.v1.notification-service.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "dev.flowchat.chat.message.v2"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.chat.message.v2.realtime-service.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.chat.message.v2.realtime-service.retry.20s"
      partitions = 1
      rf = 1
      config = @{ "cleanup.policy" = "delete"; "retention.ms" = "3600000" }
    },
    @{
      name = "dev.flowchat.chat.message.v2.realtime-service.retry.60s"
      partitions = 1
      rf = 1
      config = @{ "cleanup.policy" = "delete"; "retention.ms" = "3600000" }
    },
    @{
      name = "dev.flowchat.chat.message.v2.realtime-service.retry.300s"
      partitions = 1
      rf = 1
      config = @{ "cleanup.policy" = "delete"; "retention.ms" = "3600000" }
    },
    @{
      name = "dev.flowchat.chat.message.v2.realtime-service.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "dev.flowchat.chat.conversation-projection.v2"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.chat.conversation-projection.v2.realtime-service.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.chat.conversation-projection.v2.realtime-service.retry.20s"
      partitions = 1
      rf = 1
      config = @{ "cleanup.policy" = "delete"; "retention.ms" = "3600000" }
    },
    @{
      name = "dev.flowchat.chat.conversation-projection.v2.realtime-service.retry.60s"
      partitions = 1
      rf = 1
      config = @{ "cleanup.policy" = "delete"; "retention.ms" = "3600000" }
    },
    @{
      name = "dev.flowchat.chat.conversation-projection.v2.realtime-service.retry.300s"
      partitions = 1
      rf = 1
      config = @{ "cleanup.policy" = "delete"; "retention.ms" = "3600000" }
    },
    @{
      name = "dev.flowchat.chat.conversation-projection.v2.realtime-service.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "dev.flowchat.chat.conversation-membership-projection.v2"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.chat.conversation-membership-projection.v2.realtime-service.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.chat.conversation-membership-projection.v2.realtime-service.retry.20s"
      partitions = 1
      rf = 1
      config = @{ "cleanup.policy" = "delete"; "retention.ms" = "3600000" }
    },
    @{
      name = "dev.flowchat.chat.conversation-membership-projection.v2.realtime-service.retry.60s"
      partitions = 1
      rf = 1
      config = @{ "cleanup.policy" = "delete"; "retention.ms" = "3600000" }
    },
    @{
      name = "dev.flowchat.chat.conversation-membership-projection.v2.realtime-service.retry.300s"
      partitions = 1
      rf = 1
      config = @{ "cleanup.policy" = "delete"; "retention.ms" = "3600000" }
    },
    @{
      name = "dev.flowchat.chat.conversation-membership-projection.v2.realtime-service.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "dev.flowchat.chat.conversation-participant-projection.v2"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "dev.flowchat.presence.presence"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.presence.presence.realtime-service.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.presence.presence.realtime-service.retry.20s"
      partitions = 1
      rf = 1
      config = @{ "cleanup.policy" = "delete"; "retention.ms" = "3600000" }
    },
    @{
      name = "dev.flowchat.presence.presence.realtime-service.retry.60s"
      partitions = 1
      rf = 1
      config = @{ "cleanup.policy" = "delete"; "retention.ms" = "3600000" }
    },
    @{
      name = "dev.flowchat.presence.presence.realtime-service.retry.300s"
      partitions = 1
      rf = 1
      config = @{ "cleanup.policy" = "delete"; "retention.ms" = "3600000" }
    },
    @{
      name = "dev.flowchat.presence.presence.realtime-service.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "dev.flowchat.realtime.connection.v1"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.realtime.connection.v1.presence-service.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.realtime.connection.v1.presence-service.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "test.flowchat.harness.projection.events"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "test.flowchat.harness.projection.events.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "test.flowchat.harness.projection.events.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "86400000"
      }
    }
  )
}

function Get-LegacyTopicNames {
  return @(
    "dev.flowchat.presence.presence-status-changed.v1",
    "dev.flowchat.presence.presence-status-changed.v1.retry",
    "dev.flowchat.presence.presence-status-changed.v1.dlq",
    "dev.flowchat.identity.user.v1.retry",
    "dev.flowchat.identity.user.v1.dlq",
    "dev.flowchat.user-profile.user-profile.v1.retry",
    "dev.flowchat.user-profile.user-profile.v1.dlq",
    "dev.flowchat.notification.email.v1.retry",
    "dev.flowchat.notification.email.v1.dlq",
    "dev.flowchat.chat.message.v1",
    "dev.flowchat.chat.message.v1.retry",
    "dev.flowchat.chat.message.v1.dlq",
    "dev.flowchat.chat.message.v1.realtime-service.retry",
    "dev.flowchat.chat.message.v1.realtime-service.dlq",
    "dev.flowchat.chat.group-conversation.v1",
    "dev.flowchat.chat.group-conversation.v1.realtime-service.retry",
    "dev.flowchat.chat.group-conversation.v1.realtime-service.dlq",
    "dev.flowchat.chat.duet-conversation-projection.v1",
    "dev.flowchat.chat.duet-conversation-projection.v1.realtime-service.retry",
    "dev.flowchat.chat.duet-conversation-projection.v1.realtime-service.dlq",
    "dev.flowchat.chat.duet-conversation-projection.v1.presence-service.retry",
    "dev.flowchat.chat.duet-conversation-projection.v1.presence-service.dlq",
    "dev.flowchat.presence.presence.retry",
    "dev.flowchat.presence.presence.dlq",
    "dev.flowchat.realtime.connection.v1.retry",
    "dev.flowchat.realtime.connection.v1.dlq",
    "dev.flowchat.social-graph.contact-projection.v1",
    "dev.flowchat.social-graph.contact-projection.v1.presence-service.retry",
    "dev.flowchat.social-graph.contact-projection.v1.presence-service.dlq",
    "dev.flowchat.user-profile.user-profile.v1.chat-service.retry",
    "dev.flowchat.user-profile.user-profile.v1.chat-service.dlq",
    "dev.flowchat.user-profile.user-profile.v1.socialgraph-service.retry",
    "dev.flowchat.user-profile.user-profile.v1.socialgraph-service.dlq"
  )
}
