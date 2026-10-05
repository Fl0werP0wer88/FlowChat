#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../../lib/observability.sh"
bootstrap_observability_component Grafana grafana provisioning http://localhost:3000/api/health "$@"
