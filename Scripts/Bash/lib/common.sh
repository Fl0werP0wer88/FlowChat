#!/usr/bin/env bash
set -euo pipefail

SCRIPTS_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)
INFRA_DIR="$SCRIPTS_DIR/Infrastructure"
REPO_ROOT=$(cd -- "$SCRIPTS_DIR/.." && pwd)
declare -A OPTIONS=()

parse_options() {
  while (($#)); do
    [[ $1 == -* ]] || { printf 'Unexpected argument: %s\n' "$1" >&2; return 2; }
    local key=${1#--}
    key=${key#-}
    key=${key//-/_}
    key=${key,,}
    shift
    if (($#)) && [[ $1 != -* ]]; then
      OPTIONS[$key]=$1
      shift
    else
      OPTIONS[$key]=true
    fi
  done
}

option() { printf '%s' "${OPTIONS[${1,,}]:-${2:-}}"; }
has_option() { [[ -v OPTIONS[${1,,}] ]]; }
step() { printf '\n==> %s\n' "$*"; }
require_command() { command -v "$1" >/dev/null || { printf 'Missing required command: %s\n' "$1" >&2; return 1; }; }
require_file() { [[ -f $1 ]] || { printf 'Missing file: %s\n' "$1" >&2; return 1; }; }
ensure_volume() { docker volume inspect "$1" >/dev/null 2>&1 || docker volume create "$1" >/dev/null; }
ensure_network() { docker network inspect "$1" >/dev/null 2>&1 || docker network create "$1" >/dev/null; }

wait_http() {
  local url=$1 timeout=$2 name=${3:-$1} deadline=$((SECONDS + timeout))
  step "Waiting for $name at $url"
  until curl --silent --show-error --fail --max-time 5 --output /dev/null "$url" 2>/dev/null; do
    ((SECONDS < deadline)) || { printf '%s did not become ready within %ss\n' "$name" "$timeout" >&2; return 1; }
    sleep 2
  done
}

compose_project_container() {
  local project=$1 service=$2
  shift 2
  docker compose -p "$project" "$@" ps -q "$service"
}

wait_kafka() {
  local container=$1 binary=$2 server=$3 timeout=$4 deadline=$((SECONDS + timeout))
  until docker exec "$container" "$binary" --bootstrap-server "$server" --list >/dev/null 2>&1; do
    ((SECONDS < deadline)) || { printf 'Kafka did not become ready within %ss\n' "$timeout" >&2; return 1; }
    sleep 2
  done
}

wait_postgres() {
  local container=$1 user=$2 database=$3 password=$4 timeout=$5 deadline=$((SECONDS + timeout))
  until docker exec -e "PGPASSWORD=$password" "$container" pg_isready --username="$user" --dbname="$database" >/dev/null 2>&1; do
    ((SECONDS < deadline)) || { printf 'PostgreSQL did not become ready within %ss\n' "$timeout" >&2; return 1; }
    sleep 2
  done
}

json_file() { printf '%s/Kafka/kafka-definitions.json' "$INFRA_DIR"; }

resolve_kafka_container() {
  local project=${1:-} kafka_file=$2 ui_file=$3 service=${4:-broker} direct=${5:-}
  [[ -z $direct ]] || { printf '%s\n' "$direct"; return; }
  local found=''
  if [[ -n $project ]]; then
    found=$(docker compose -p "$project" -f "$kafka_file" -f "$ui_file" ps -q "$service" 2>/dev/null || true)
  fi
  if [[ -z $found ]]; then
    found=$(docker compose -f "$kafka_file" ps -q "$service" 2>/dev/null || true)
  fi
  if [[ -z $found ]]; then
    found=$(docker ps -q --filter "name=^/${service}$")
  fi
  [[ -n $found ]] || { printf 'Could not resolve Kafka container for %s\n' "$service" >&2; return 1; }
  printf '%s\n' "$found"
}
