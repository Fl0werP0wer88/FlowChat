#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
require_command docker
project=$(option project_name flowchat)
file=$(option compose_file "$INFRA_DIR/PostgreSQL/docker-compose.yml")
service=$(option service_name postgres)
admin=$(option admin_user flowchat)
admin_password=$(option admin_password flowchat_pw)
migrator=$(option migrator_user flowchat_migrator)
migrator_password=$(option migrator_password flowchat_migrator_pw)
app=$(option app_user flowchat_app)
app_password=$(option app_password flowchat_app_pw)
databases=(
  "$(option auth_db flowchat_auth_db)"
  "$(option chat_db flowchat_chat_db)"
  "$(option user_profile_db flowchat_userprofile_db)"
  "$(option presence_db flowchat_presence_db)"
  "$(option notification_db flowchat_notification_db)"
  "$(option harness_db flowchat_harness_db)"
  "$(option realtime_db flowchat_realtime_db)"
)
for identifier in "$admin" "$migrator" "$app" "${databases[@]}"; do
  [[ $identifier =~ ^[a-zA-Z_][a-zA-Z0-9_]*$ ]] || { printf 'Invalid PostgreSQL identifier: %s\n' "$identifier" >&2; exit 2; }
done
sql_password() { printf '%s' "${1//\'/\'\'}"; }
psql_sql() {
  local database=$1 statement=$2
  printf '%s\n' "$statement" | docker exec -i -e "PGPASSWORD=$admin_password" "$container" psql -h 127.0.0.1 -p 5432 -U "$admin" -d "$database" -v ON_ERROR_STOP=1 -f - >/dev/null
}
psql_query() {
  docker exec -e "PGPASSWORD=$admin_password" "$container" psql -h 127.0.0.1 -p 5432 -U "$admin" -d postgres -tA -v ON_ERROR_STOP=1 -c "$1"
}
ensure_role() {
  local role=$1 password=$2 createdb=$3 action=CREATE
  [[ -z $(psql_query "SELECT 1 FROM pg_roles WHERE rolname='$role' LIMIT 1;") ]] || action=ALTER
  psql_sql postgres "$action ROLE $role WITH LOGIN PASSWORD '$(sql_password "$password")' $createdb;"
}
ensure_database() {
  local db=$1
  if [[ -z $(psql_query "SELECT 1 FROM pg_database WHERE datname='$db' LIMIT 1;") ]]; then
    psql_sql postgres "CREATE DATABASE $db OWNER $migrator;"
  fi
  psql_sql postgres "ALTER DATABASE $db OWNER TO $migrator; GRANT CONNECT ON DATABASE $db TO $app;"
  psql_sql "$db" "ALTER SCHEMA public OWNER TO $migrator; REVOKE CREATE ON SCHEMA public FROM PUBLIC;"
  psql_sql "$db" "$(cat <<SQL
DO \$\$
DECLARE item record;
BEGIN
  FOR item IN SELECT tablename FROM pg_tables WHERE schemaname = 'public' LOOP
    EXECUTE format('ALTER TABLE public.%I OWNER TO $migrator;', item.tablename);
  END LOOP;
  FOR item IN SELECT sequence_name FROM information_schema.sequences WHERE sequence_schema = 'public' LOOP
    EXECUTE format('ALTER SEQUENCE public.%I OWNER TO $migrator;', item.sequence_name);
  END LOOP;
  FOR item IN SELECT table_name FROM information_schema.views WHERE table_schema = 'public' LOOP
    EXECUTE format('ALTER VIEW public.%I OWNER TO $migrator;', item.table_name);
  END LOOP;
END
\$\$;
SQL
)"
  psql_sql "$db" "GRANT USAGE ON SCHEMA public TO $app; REVOKE CREATE ON SCHEMA public FROM $app; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO $app; GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO $app; ALTER DEFAULT PRIVILEGES FOR ROLE $migrator IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO $app; ALTER DEFAULT PRIVILEGES FOR ROLE $migrator IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO $app;"
  printf 'Database ready: %s\n' "$db"
}
ensure_volume postgresql_flowchat_pgdata
docker compose -p "$project" -f "$file" up -d
container=$(compose_project_container "$project" "$service" -f "$file")
[[ -n $container ]] || { printf 'PostgreSQL container not found\n' >&2; exit 1; }
wait_postgres "$container" "$admin" postgres "$admin_password" "$(option timeout_seconds 180)"
if has_option drop_db; then
  for db in "${databases[@]}"; do psql_sql postgres "DROP DATABASE IF EXISTS $db WITH (FORCE);"; done
fi
ensure_role "$migrator" "$migrator_password" CREATEDB
psql_sql postgres "GRANT pg_signal_backend TO $migrator;"
ensure_role "$app" "$app_password" NOCREATEDB
for db in "${databases[@]}"; do ensure_database "$db"; done
