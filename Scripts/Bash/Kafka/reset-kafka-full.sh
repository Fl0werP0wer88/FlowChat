#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
project=$(option project_name flowchat-kafka)
kafka_file=$(option kafka_compose_file "$INFRA_DIR/Kafka/docker-compose.kafka.yml")
ui_file=$(option ui_compose_file "$INFRA_DIR/Kafka/docker-compose.kafka-ui.yml")
container=$(option container_name broker)
existing=$(docker ps -aq --filter "name=^/${container}$")
if [[ -n $existing ]]; then docker rm -f "$existing"; fi
docker compose -p "$project" -f "$kafka_file" -f "$ui_file" up -d --remove-orphans
"$(dirname -- "${BASH_SOURCE[0]}")/ensure-kafka-topics.sh" --project-name "$project" --kafka-compose-file "$kafka_file" --ui-compose-file "$ui_file" --service-name "$(option service_name broker)" --kafka-bin "$(option kafka_bin /opt/kafka/bin)" --bootstrap-server "$(option bootstrap_server localhost:9092)" --timeout-seconds "$(option timeout_seconds 180)"
