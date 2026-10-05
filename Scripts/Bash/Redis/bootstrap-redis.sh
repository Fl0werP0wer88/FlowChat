#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
require_command docker
project=$(option project_name flowchat)
file=$(option compose_file "$INFRA_DIR/Redis/docker-compose.yml")
service=$(option service_name redis)
user=$(option redis_username default)
password=$(option redis_password flowchat_redis_pw)
ensure_volume flowchat-redis_redis_data
export FLOWCHAT_REDIS_USERNAME=$user FLOWCHAT_REDIS_PASSWORD=$password
docker compose -p "$project" -f "$file" up -d
container=$(compose_project_container "$project" "$service" -f "$file")
[[ -n $container ]] || { printf 'Redis container not found\n' >&2; exit 1; }
deadline=$((SECONDS + $(option timeout_seconds 60)))
until [[ $(docker exec "$container" redis-cli --user "$user" --pass "$password" ping 2>/dev/null || true) == PONG ]]; do
  ((SECONDS < deadline)) || { docker logs --tail 80 "$container"; exit 1; }
  sleep 2
done
printf 'Redis ready: localhost:6379\n'
