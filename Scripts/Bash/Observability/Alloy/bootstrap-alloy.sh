#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../../lib/observability.sh"
bootstrap_observability_component Alloy alloy config.alloy http://localhost:12345/-/ready "$@"
