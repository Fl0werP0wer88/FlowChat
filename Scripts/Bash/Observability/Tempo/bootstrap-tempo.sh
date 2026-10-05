#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../../lib/observability.sh"
bootstrap_observability_component Tempo tempo tempo.yml http://localhost:3200/ready "$@"
