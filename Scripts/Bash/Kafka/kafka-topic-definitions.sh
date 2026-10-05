#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../lib/common.sh"
require_command jq
get_topic_definitions() { jq -c '.topics[]' "$(json_file)"; }
get_legacy_topic_names() { jq -r '.legacyTopics[]' "$(json_file)"; }
if [[ ${BASH_SOURCE[0]} == "$0" ]]; then get_topic_definitions; fi
