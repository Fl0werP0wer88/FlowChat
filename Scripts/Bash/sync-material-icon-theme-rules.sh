#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/lib/common.sh"
parse_options "$@"
require_command jq
require_command perl
root=$(realpath -- "$(option repo_root "$REPO_ROOT")")
workspace="$root/FlowChat.code-workspace"
settings="$root/.vscode/settings.json"
require_file "$workspace"
require_file "$settings"
tmp_dir="$REPO_ROOT/.codex/temp"
mkdir -p "$tmp_dir"
work=$(mktemp -d "$tmp_dir/icon-rules.XXXXXXXX")
trap 'rm -rf -- "$work"' EXIT
find "$root" -type d \( -name .git -o -name bin -o -name obj -o -name node_modules -o -name dist -o -name out -o -name TestResults \) -prune -o -type d -printf '%f\n' | sort -u >"$work/folders"
clones='[]'
while IFS='|' read -r name match base color; do
  [[ -n $name ]] || continue
  if [[ $match == Workers ]]; then
    names=$(awk 'tolower($0)=="workers"' "$work/folders" | jq -R . | jq -s .)
  else
    names=$(awk -v suffix="$match" 'tolower(substr($0,length($0)-length(suffix)+1))==tolower(suffix)' "$work/folders" | jq -R . | jq -s .)
  fi
  if [[ $names == '[]' ]]; then printf 'Skipping rule %s: no matching folders\n' "$name"; continue; fi
  clone=$(jq -n --arg name "$name" --arg base "$base" --arg color "$color" --argjson folderNames "$names" '{name:$name,base:$base,color:$color,lightColor:$color,folderNames:$folderNames}')
  clones=$(jq -c --argjson clone "$clone" '. + [$clone]' <<<"$clones")
  printf 'Prepared rule %s\n' "$name"
done <<'RULES'
flowchat-application-folders|.Application|app|#d95c6a
flowchat-api-folders|.API|api|#d99a2b
flowchat-infrastructure-folders|.Infrastructure|server|#4f93b3
flowchat-persistence-folders|.Persistence|database|#6f9d4a
domain|.Domain|core|#7c6bcf
workers|Workers|tasks|#5f73d8
RULES
names=$(jq -c '[.[].name]' <<<"$clones")
for source_file in "$workspace" "$settings"; do
  target="$work/$(basename "$source_file")"
  perl -0777 -pe 's/,(?=\s*[\]}])//g' "$source_file" |
    jq --argjson clones "$clones" --argjson names "$names" '
      def update_settings:
        .["material-icon-theme.folders.theme"] = "specific" |
        .["material-icon-theme.folders.customClones"] = ((.["material-icon-theme.folders.customClones"] // [] | map(select(.name as $n | $names | index($n) | not))) + $clones);
      if has("settings") then .settings |= update_settings else update_settings end
    ' >"$target"
done
if has_option dry_run; then printf 'Dry run complete. No files were written.\n'; exit 0; fi
cp -- "$work/$(basename "$workspace")" "$workspace"
cp -- "$work/$(basename "$settings")" "$settings"
printf 'Material Icon Theme rules synchronized successfully.\n'
