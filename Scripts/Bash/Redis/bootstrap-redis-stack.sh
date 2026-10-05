#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
project=$(option project_name flowchat)
user=$(option redis_username default)
password=$(option redis_password flowchat_redis_pw)
script_dir=$(dirname -- "${BASH_SOURCE[0]}")
"$script_dir/bootstrap-redis.sh" --project-name "$project" --compose-file "$(option redis_compose_file "$INFRA_DIR/Redis/docker-compose.yml")" --service-name "$(option redis_service_name redis)" --redis-username "$user" --redis-password "$password" --timeout-seconds "$(option redis_timeout_seconds 60)"
"$script_dir/bootstrap-redisinsight.sh" --project-name "$project" --compose-file "$(option redis_insight_compose_file "$INFRA_DIR/Redis/docker-compose.redisinsight.yml")" --service-name "$(option redis_insight_service_name redisinsight)" --ui-url "$(option redis_insight_ui_url http://localhost:5540)" --redis-username "$user" --redis-password "$password" --timeout-seconds "$(option redis_insight_timeout_seconds 120)" --skip-redis-bootstrap
