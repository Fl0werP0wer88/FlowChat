#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/common.sh"

bootstrap_observability_component() {
  local component=$1 service=$2 config=$3 url=$4
  shift 4
  parse_options "$@"
  require_command docker
  local file network container
  file=$(option compose_file "$INFRA_DIR/Observability/$component/docker-compose.yml")
  if [[ $component == Grafana ]]; then
    config=$(option provisioning_path "$INFRA_DIR/Observability/$component/$config")
  else
    config=$(option config_file "$INFRA_DIR/Observability/$component/$config")
  fi
  network=$(option network_name flowchat-observability)
  url=$(option ready_url "$url")
  require_file "$file"
  [[ -e $config ]] || { printf "Missing config: %s\n" "$config" >&2; return 1; }
  ensure_network "$network"
  docker compose --project-directory "$INFRA_DIR/Observability" -f "$file" up -d
  container=$(docker compose --project-directory "$INFRA_DIR/Observability" -f "$file" ps -q "$(option service_name "$service")")
  [[ -n $container ]] || { printf '%s container not found\n' "$service" >&2; return 1; }
  wait_http "$url" "$(option timeout_seconds 120)" "$component"
}
