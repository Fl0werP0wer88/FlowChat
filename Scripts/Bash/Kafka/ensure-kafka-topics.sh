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
while IFS= read -r definition; do
  name=$(jq -r '.name' <<<"$definition")
  if docker exec "$container" "$binary" --bootstrap-server "$server" --describe --topic "$name" >/dev/null 2>&1; then
    printf 'Topic exists: %s\n' "$name"
    continue
  fi
  args=(--create --if-not-exists --topic "$name" --bootstrap-server "$server" --partitions "$(jq -r '.partitions' <<<"$definition")" --replication-factor "$(jq -r '.rf' <<<"$definition")")
  while IFS= read -r config; do args+=(--config "$config"); done < <(jq -r '.config | to_entries[] | "\(.key)=\(.value)"' <<<"$definition")
  docker exec "$container" "$binary" "${args[@]}"
done < <(get_topic_definitions)
docker exec "$container" "$binary" --bootstrap-server "$server" --list
