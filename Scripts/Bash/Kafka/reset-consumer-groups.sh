#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
source "$(dirname -- "${BASH_SOURCE[0]}")/kafka-consumer-group-definitions.sh"
parse_options "$@"
require_command docker
kafka_file=$(option kafka_compose_file "$INFRA_DIR/Kafka/docker-compose.kafka.yml")
ui_file=$(option ui_compose_file "$INFRA_DIR/Kafka/docker-compose.kafka-ui.yml")
service=$(option service_name broker)
base=$(option kafka_bin /opt/kafka/bin)
server=$(option bootstrap_server localhost:9092)
container=$(resolve_kafka_container "$(option project_name)" "$kafka_file" "$ui_file" "$service" "$(option container_id)")
wait_kafka "$container" "$base/kafka-topics.sh" "$server" "$(option timeout_seconds 120)"
while IFS= read -r group; do
  if docker exec "$container" "$base/kafka-consumer-groups.sh" --bootstrap-server "$server" --list | grep -Fxq -- "$group"; then
    printf 'Deleting consumer group: %s\n' "$group"
    docker exec "$container" "$base/kafka-consumer-groups.sh" --bootstrap-server "$server" --delete --group "$group"
  else
    printf 'Consumer group missing: %s\n' "$group"
  fi
done < <(get_consumer_group_definitions)
docker exec "$container" "$base/kafka-consumer-groups.sh" --bootstrap-server "$server" --list
