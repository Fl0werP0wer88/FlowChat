#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/lib/common.sh"
parse_options "$@"
project=flowchat
solution="$REPO_ROOT/FlowChat.slnx"
client="$REPO_ROOT/ReactClient"
postgres="$INFRA_DIR/PostgreSQL/docker-compose.yml"
kafka="$INFRA_DIR/Kafka/docker-compose.kafka.yml"
kafka_ui="$INFRA_DIR/Kafka/docker-compose.kafka-ui.yml"
redis="$INFRA_DIR/Redis/docker-compose.yml"
redis_insight="$INFRA_DIR/Redis/docker-compose.redisinsight.yml"
mailhog="$INFRA_DIR/MailHog/docker-compose.yml"
infisical="$INFRA_DIR/Infisical/docker-compose.yml"
observability=(
  "$INFRA_DIR/Observability/Loki/docker-compose.yml"
  "$INFRA_DIR/Observability/Tempo/docker-compose.yml"
  "$INFRA_DIR/Observability/Prometheus/docker-compose.yml"
  "$INFRA_DIR/Observability/Alloy/docker-compose.yml"
  "$INFRA_DIR/Observability/Grafana/docker-compose.yml"
)
observability_args=(--project-directory "$INFRA_DIR/Observability")
for file in "${observability[@]}"; do observability_args+=(-f "$file"); done
export COMPOSE_IGNORE_ORPHANS=true

step 'Checking prerequisites'
for command in docker dotnet node npm curl jq; do require_command "$command"; done
if ! has_option skip_infisical; then require_command openssl; fi
for file in "$solution" "$REPO_ROOT/FlowChat.code-workspace" "$client/package-lock.json" "$postgres" "$kafka" "$kafka_ui" "$redis" "$redis_insight" "$mailhog"; do require_file "$file"; done
if ! has_option skip_infisical; then require_file "$infisical"; fi
if ! has_option skip_observability; then for file in "${observability[@]}"; do require_file "$file"; done; fi
docker version --format '{{.Server.Version}}' >/dev/null
docker compose version >/dev/null
dotnet --list-sdks | grep -q '^10\.'
dotnet ef --version | grep -q '^10\.'
node_major=$(node --version | sed -E 's/^v([0-9]+).*/\1/')
((node_major >= 20)) || { printf 'Node.js 20 or later is required\n' >&2; exit 1; }
npm --version >/dev/null

remove_legacy() {
  local name=$1; shift
  if [[ -n $(docker ps -aq --filter "label=com.docker.compose.project=$name") ]]; then
    step "Migrating legacy Compose project $name"
    docker compose -p "$name" "$@" down --remove-orphans
  fi
}
remove_legacy postgresql -f "$postgres"
remove_legacy flowchat-kafka -f "$kafka" -f "$kafka_ui"
remove_legacy flowchat-redis -f "$redis"
remove_legacy flowchat-redisinsight -f "$redis_insight"
remove_legacy mailhog -f "$mailhog"
if ! has_option skip_infisical; then remove_legacy flowchat-infisical -f "$infisical"; fi
if ! has_option skip_observability; then remove_legacy flowchat-observability "${observability_args[@]}"; fi

step 'Preparing persistent volumes'
for volume in postgresql_flowchat_pgdata flowchat-redis_redis_data flowchat-redisinsight_redisinsight_data; do ensure_volume "$volume"; done
if ! has_option skip_infisical; then for volume in flowchat-infisical_pg_data flowchat-infisical_redis_data; do ensure_volume "$volume"; done; fi
if ! has_option skip_observability; then for name in loki tempo prometheus alloy grafana; do ensure_volume "flowchat-observability_flowchat_${name}_data"; done; fi

if ! has_option skip_pull; then
  step 'Pulling Docker images'
  docker compose -p "$project" -f "$postgres" pull
  docker compose -p "$project" -f "$kafka" -f "$kafka_ui" pull
  docker compose -p "$project" -f "$redis" pull
  docker compose -p "$project" -f "$redis_insight" pull
  docker compose -p "$project" -f "$mailhog" pull
  if ! has_option skip_infisical; then for image in infisical/infisical:latest postgres:14-alpine redis:7-alpine; do docker pull "$image"; done; fi
  if ! has_option skip_observability; then docker compose -p "$project" "${observability_args[@]}" pull; fi
fi

step 'Bootstrapping infrastructure'
"$SCRIPTS_DIR/Bash/PostgreSQL/bootstrap-postgres.sh" --project-name "$project" --compose-file "$postgres"
"$SCRIPTS_DIR/Bash/Kafka/bootstrap-kafka-stack.sh" --project-name "$project" --kafka-compose-file "$kafka" --ui-compose-file "$kafka_ui"
"$SCRIPTS_DIR/Bash/Redis/bootstrap-redis-stack.sh" --project-name "$project" --redis-compose-file "$redis" --redis-insight-compose-file "$redis_insight"
"$SCRIPTS_DIR/Bash/MailHog/bootstrap-mailhog.sh" --project-name "$project" --compose-file "$mailhog"
if ! has_option skip_infisical; then "$SCRIPTS_DIR/Bash/Infisical/bootstrap-infisical.sh" --project-name "$project" --compose-file "$infisical" --env-file "$INFRA_DIR/Infisical/.env"; fi
if ! has_option skip_observability; then "$SCRIPTS_DIR/Bash/Observability/bootstrap-observability-stack.sh" --project-name "$project"; fi

step 'Restoring and building application'
dotnet restore "$solution"
npm ci --prefix "$client"
if ! has_option skip_build; then
  dotnet build "$solution" --no-restore
  npm run build --prefix "$client"
fi
if ! has_option skip_migrations; then
  migration_args=()
  if ! has_option skip_build; then migration_args+=(--skip-build); fi
  "$SCRIPTS_DIR/Bash/PostgreSQL/migrate-all.sh" "${migration_args[@]}"
fi
if ! dotnet dev-certs https --check --trust >/dev/null 2>&1; then
  printf 'Development HTTPS certificate is missing or not trusted; run dotnet dev-certs https --trust\n' >&2
fi
step 'FlowChat local environment is ready'
printf 'Gateway: https://localhost:7270\nReact client: http://localhost:5173\nKafka UI: http://localhost:8080\nRedisInsight: http://localhost:5540\nMailHog: http://localhost:8025\n'
if ! has_option skip_infisical; then printf 'Infisical: http://localhost:18180\n'; fi
if ! has_option skip_observability; then printf 'Grafana: http://localhost:3000\nPrometheus: http://localhost:9090\n'; fi
