#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
file=$(option compose_file "$INFRA_DIR/Kafka/docker-compose.kafka-ui.yml")
kafka_file=$(option kafka_compose_file "$INFRA_DIR/Kafka/docker-compose.kafka.yml")
project=$(option project_name flowchat-kafka)
docker compose -p "$project" -f "$kafka_file" -f "$file" up -d "$(option service_name kafka-ui)"
wait_http "http://localhost:$(option port 8080)/" "$(option timeout_seconds 120)" 'Kafka UI'
