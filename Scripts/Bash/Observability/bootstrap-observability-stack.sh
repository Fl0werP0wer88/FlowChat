#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
require_command docker
project=$(option project_name flowchat)
network=$(option network_name flowchat-observability)
timeout=$(option timeout_seconds 180)
ensure_network "$network"
for name in loki tempo prometheus alloy grafana; do ensure_volume "flowchat-observability_flowchat_${name}_data"; done
files=()
for name in Loki Tempo Prometheus Alloy Grafana; do
  file="$INFRA_DIR/Observability/$name/docker-compose.yml"
  require_file "$file"
  files+=(-f "$file")
done
docker compose -p "$project" --project-directory "$INFRA_DIR/Observability" "${files[@]}" up -d
wait_http http://localhost:3100/ready "$timeout" Loki
wait_http http://localhost:3200/ready "$timeout" Tempo
wait_http http://localhost:9090/-/ready "$timeout" Prometheus
wait_http http://localhost:12345/-/ready "$timeout" Alloy
wait_http http://localhost:3000/api/health "$timeout" Grafana
