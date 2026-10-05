#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
file=$(option compose_file "$INFRA_DIR/Kafka/docker-compose.kafka.yml")
project=$(option project_name flowchat-kafka)
docker compose -p "$project" -f "$file" up -d
"$(dirname -- "${BASH_SOURCE[0]}")/ensure-kafka-topics.sh" --project-name "$project" --kafka-compose-file "$file" --service-name "$(option service_name broker)" --timeout-seconds "$(option timeout_seconds 120)"
