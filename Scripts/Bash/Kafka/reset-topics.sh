#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
parse_options "$@"
script_dir=$(dirname -- "${BASH_SOURCE[0]}")
"$script_dir/remove-topics.sh" "$@"
"$script_dir/ensure-kafka-topics.sh" "$@"
