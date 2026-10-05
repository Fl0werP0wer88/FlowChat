#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
require_command docker
project=$(option project_name flowchat)
file=$(option compose_file "$INFRA_DIR/Redis/docker-compose.redisinsight.yml")
user=$(option redis_username default)
password=$(option redis_password flowchat_redis_pw)
url=$(option ui_url http://localhost:5540)
ensure_volume flowchat-redisinsight_redisinsight_data
export FLOWCHAT_REDIS_USERNAME=$user FLOWCHAT_REDIS_PASSWORD=$password
if ! has_option skip_redis_bootstrap; then
  "$(dirname -- "${BASH_SOURCE[0]}")/bootstrap-redis.sh" --project-name "$project" --redis-username "$user" --redis-password "$password"
fi
docker compose -p "$project" -f "$file" up -d
container=$(compose_project_container "$project" "$(option service_name redisinsight)" -f "$file")
[[ -n $container ]] || { printf 'RedisInsight container not found\n' >&2; exit 1; }
wait_http "${url%/}/api/health/" "$(option timeout_seconds 120)" RedisInsight
printf 'RedisInsight ready: %s\n' "$url"
