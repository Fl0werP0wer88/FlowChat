#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/../../lib/observability.sh"
bootstrap_observability_component Prometheus prometheus prometheus.yml http://localhost:9090/-/ready "$@"
