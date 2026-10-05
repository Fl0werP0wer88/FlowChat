#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
"$(dirname -- "${BASH_SOURCE[0]}")/bootstrap-postgres.sh" --drop-db
while IFS= read -r directory; do
  step "Clearing migrations in $directory"
  find "$directory" -mindepth 1 -maxdepth 1 -exec rm -r -- {} +
done < <(find "$REPO_ROOT" -type d -name Migrations -not -path '*/bin/*' -not -path '*/obj/*')
args=(--auto-generate-migration --migration-name "$(option migration_name InitialCreate)")
if has_option skip_build; then args+=(--skip-build); fi
"$(dirname -- "${BASH_SOURCE[0]}")/migrate-all.sh" "${args[@]}"
