#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
require_command dotnet
require_command jq
migration_name=$(option migration_name)
while IFS= read -r service; do
  name=$(jq -r '.Name' <<<"$service")
  project="$REPO_ROOT/$(jq -r '.Project' <<<"$service")"
  startup="$REPO_ROOT/$(jq -r '.Startup' <<<"$service")"
  migrations_dir="$(dirname -- "$project")/Migrations"
  step "$name: update database"
  if ! has_option skip_build; then dotnet build "$startup" -c Debug; fi
  if has_option auto_generate_migration && [[ -z $(find "$migrations_dir" -maxdepth 1 -name '*.cs' -print -quit 2>/dev/null || true) ]]; then
    name_to_add=${migration_name:-InitialCreate}
    dotnet ef migrations add "$name_to_add" --project "$project" --startup-project "$startup" --no-build
    dotnet build "$startup" -c Debug
  fi
  if output=$(dotnet ef database update --project "$project" --startup-project "$startup" --no-build 2>&1); then
    printf '%s: database is up to date\n' "$name"
  elif has_option auto_generate_migration && [[ $output == *PendingModelChangesWarning* || $output == *'No migrations were found'* ]]; then
    name_to_add=${migration_name:-AutoMigration_$(date -u +%Y%m%d_%H%M%S)}
    [[ $output != *'No migrations were found'* ]] || name_to_add=${migration_name:-InitialCreate}
    dotnet ef migrations add "$name_to_add" --project "$project" --startup-project "$startup" --no-build
    dotnet build "$startup" -c Debug
    dotnet ef database update --project "$project" --startup-project "$startup" --no-build
  else
    printf '%s\n' "$output" >&2
    exit 1
  fi
done < <(jq -c '.[]' "$INFRA_DIR/PostgreSQL/migration-services.json")
