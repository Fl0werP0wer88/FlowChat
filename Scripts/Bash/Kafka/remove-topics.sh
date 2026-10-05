#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
source "$(dirname -- "${BASH_SOURCE[0]}")/kafka-topic-definitions.sh"
parse_options "$@"
require_command docker
kafka_file=$(option kafka_compose_file "$INFRA_DIR/Kafka/docker-compose.kafka.yml")
ui_file=$(option ui_compose_file "$INFRA_DIR/Kafka/docker-compose.kafka-ui.yml")
service=$(option service_name broker)
binary="$(option kafka_bin /opt/kafka/bin)/kafka-topics.sh"
server=$(option bootstrap_server localhost:9092)
container=$(resolve_kafka_container "$(option project_name)" "$kafka_file" "$ui_file" "$service" "$(option container_id)")
wait_kafka "$container" "$binary" "$server" "$(option timeout_seconds 120)"
while IFS= read -r name; do
  if docker exec "$container" "$binary" --bootstrap-server "$server" --describe --topic "$name" >/dev/null 2>&1; then
    printf 'Deleting topic: %s\n' "$name"
    docker exec "$container" "$binary" --delete --topic "$name" --bootstrap-server "$server"
  fi
done < <({ get_topic_definitions | jq -r '.name'; get_legacy_topic_names; } | sort -u)
docker exec "$container" "$binary" --bootstrap-server "$server" --list
