#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/lib/common.sh"
parse_options "$@"
root=$(realpath -- "$(option root_path "$REPO_ROOT")")
[[ -d $root ]] || { printf 'Directory not found: %s\n' "$root" >&2; exit 1; }
exclude_arg=$(option exclude_path_pattern '')
patterns=(.git .vscode coverage .next .nuxt .svelte-kit .angular .turbo .pnpm-store .yarn .cache .parcel-cache .vite .vite-temp publish ApplicationEvents)
if [[ -n $exclude_arg ]]; then
  IFS=',' read -ra patterns <<<"$exclude_arg"
fi
printf 'Scanning for empty directories under: %s\n' "$root"
removed=0
while IFS= read -r -d '' dir; do
  rel=${dir#"$root"/}
  skip=false
  IFS='/' read -ra parts <<<"$rel"
  for part in "${parts[@]}"; do
    for pattern in "${patterns[@]}"; do
      if [[ $part == $pattern ]]; then skip=true; break 2; fi
    done
  done
  [[ $skip == false ]] || continue
  if ! has_option no_gitignore && [[ -f $(option gitignore_path "$root/.gitignore") ]]; then
    if git -C "$root" check-ignore -q -- "$rel" 2>/dev/null; then continue; fi
  fi
  [[ -z $(find "$dir" -mindepth 1 -maxdepth 1 -print -quit) ]] || continue
  if has_option dry_run; then
    printf 'Would remove: %s\n' "$rel"
  else
    rmdir -- "$dir"
    printf 'Removed: %s\n' "$rel"
  fi
  ((removed+=1))
done < <(find "$root" -mindepth 1 -depth -type d -print0)
printf '%s empty directories: %s\n' "$(has_option dry_run && printf 'Eligible' || printf 'Removed')" "$removed"
