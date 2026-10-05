#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
require_command docker
project=$(option project_name flowchat)
file=$(option compose_file "$INFRA_DIR/MailHog/docker-compose.yml")
url=$(option ui_url http://localhost:8025)
docker compose -p "$project" -f "$file" up -d
container=$(compose_project_container "$project" "$(option service_name mailhog)" -f "$file")
[[ -n $container ]] || { printf 'MailHog container not found\n' >&2; exit 1; }
wait_http "$url" "$(option timeout_seconds 60)" MailHog
printf 'MailHog SMTP: localhost:1025, UI: %s\n' "$url"
