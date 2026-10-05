#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../../lib/observability.sh"
bootstrap_observability_component Loki loki loki-config.yml http://localhost:3100/ready "$@"
