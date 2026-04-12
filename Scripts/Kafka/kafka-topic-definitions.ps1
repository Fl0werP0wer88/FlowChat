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
      name = "dev.flowchat.identity.user.v1.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.identity.user.v1.dlq"
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
      name = "dev.flowchat.user-profile.user-profile.v1.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.user-profile.user-profile.v1.dlq"
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
      name = "dev.flowchat.notification.email.v1.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.notification.email.v1.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "dev.flowchat.chat.message.v1"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.chat.message.v1.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.chat.message.v1.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    },
    @{
      name = "dev.flowchat.social-graph.contact"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "2419200000"
      }
    },
    @{
      name = "dev.flowchat.social-graph.contact.retry"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "3600000"
      }
    },
    @{
      name = "dev.flowchat.social-graph.contact.dlq"
      partitions = 1
      rf = 1
      config = @{
        "cleanup.policy" = "delete"
        "retention.ms" = "1209600000"
      }
    }
  )
}

function Get-LegacyTopicNames {
  return @(
    "dev.flowchat.social-graph.contact-added.v1",
    "dev.flowchat.social-graph.contact-added.v1.retry",
    "dev.flowchat.social-graph.contact-added.v1.dlq",
    "dev.flowchat.social-graph.contact-deleted.v1",
    "dev.flowchat.social-graph.contact-deleted.v1.retry",
    "dev.flowchat.social-graph.contact-deleted.v1.dlq"
  )
}
