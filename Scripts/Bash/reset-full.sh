#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/lib/common.sh"
parse_options "$@"
poll=$(option poll_interval_seconds 2)
((poll >= 1)) || { printf 'Poll interval must be positive\n' >&2; exit 2; }
log_dir="$REPO_ROOT/.codex/temp/full-reset-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$log_dir"
step 'Starting full database and Kafka resets in parallel'
bash "$SCRIPTS_DIR/Bash/reset-db-auto.sh" >"$log_dir/database-services-users.out.log" 2>"$log_dir/database-services-users.err.log" &
db_pid=$!
bash "$SCRIPTS_DIR/Bash/Kafka/reset-kafka-full.sh" >"$log_dir/kafka.out.log" 2>"$log_dir/kafka.err.log" &
kafka_pid=$!
printf 'Started database/services/users reset (PID %s) and Kafka reset (PID %s)\n' "$db_pid" "$kafka_pid"
step 'Waiting for both resets to finish'
while [[ -n $(jobs -pr) ]]; do sleep "$poll"; done
set +e
wait "$db_pid"; db_status=$?
wait "$kafka_pid"; kafka_status=$?
set -e
printf 'Database/services/users reset: %s; Kafka reset: %s\nLogs: %s\n' "$db_status" "$kafka_status" "$log_dir"
((db_status == 0 && kafka_status == 0))
