#requires -Version 5.1

function Get-KafkaDefinitions {
  $path = Join-Path $PSScriptRoot '../../Infrastructure/Kafka/kafka-definitions.json'
  return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}

function Get-TopicDefinitions {
  foreach ($topic in (Get-KafkaDefinitions).topics) {
    $config = @{}
    foreach ($property in $topic.config.PSObject.Properties) {
      $config[$property.Name] = [string]$property.Value
    }

    @{
      name = [string]$topic.name
      partitions = [int]$topic.partitions
      rf = [int]$topic.rf
      config = $config
    }
  }
}

function Get-LegacyTopicNames {
  return @((Get-KafkaDefinitions).legacyTopics)
}
