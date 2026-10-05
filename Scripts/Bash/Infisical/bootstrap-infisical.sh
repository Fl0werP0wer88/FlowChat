#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
require_command docker
require_command openssl
project=$(option project_name flowchat)
file=$(option compose_file "$INFRA_DIR/Infisical/docker-compose.yml")
env_file=$(option env_file "$INFRA_DIR/Infisical/.env")
port=$(option port 18180)
require_file "$file"
ensure_volume flowchat-infisical_pg_data
ensure_volume flowchat-infisical_redis_data
if [[ ! -f $env_file ]]; then
  password=$(openssl rand -hex 16)
  umask 077
  cat > "$env_file" <<ENV
INFISICAL_PORT=$port
ENCRYPTION_KEY=$(openssl rand -hex 16)
AUTH_SECRET=$(openssl rand -base64 32)
POSTGRES_USER=infisical
POSTGRES_PASSWORD=$password
POSTGRES_DB=infisical
DB_CONNECTION_URI=postgres://infisical:${password}@db:5432/infisical
REDIS_URL=redis://infisical-redis:6379
SITE_URL=http://localhost:$port
HTTPS_ENABLED=false
ENV
else
  sed -i 's#^REDIS_URL=redis://redis:6379$#REDIS_URL=redis://infisical-redis:6379#' "$env_file"
fi
configured_port=$(sed -n 's/^INFISICAL_PORT=//p' "$env_file" | head -1)
port=${configured_port:-$port}
url="http://localhost:$port"
docker compose -p "$project" -f "$file" up -d
container=$(compose_project_container "$project" "$(option service_name backend)" -f "$file")
[[ -n $container ]] || { printf 'Infisical backend container not found\n' >&2; exit 1; }
if ! wait_http "$url/api/status" "$(option timeout_seconds 180)" Infisical; then
  docker compose -p "$project" -f "$file" logs --tail 120 backend
  exit 1
fi
printf 'Infisical UI: %s\n' "$url"
