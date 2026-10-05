#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
script_dir=$(dirname -- "${BASH_SOURCE[0]}")
"$script_dir/reset-consumer-groups.sh" "$@"
"$script_dir/reset-topics.sh" "$@"
