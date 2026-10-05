#requires -Version 5.1

function Get-ConsumerGroupDefinitions {
  $path = Join-Path $PSScriptRoot '../../Infrastructure/Kafka/kafka-definitions.json'
  $definitions = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
  return @($definitions.consumerGroups)
}
