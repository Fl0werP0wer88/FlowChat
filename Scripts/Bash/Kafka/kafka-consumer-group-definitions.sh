#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
require_command jq
get_consumer_group_definitions() { jq -r '.consumerGroups[]' "$(json_file)"; }
if [[ ${BASH_SOURCE[0]} == "$0" ]]; then get_consumer_group_definitions; fi
