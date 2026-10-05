#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/lib/common.sh"
parse_options "$@"
require_command dotnet
require_command npm
require_command curl
require_command setsid
startup_timeout=$(option startup_timeout_seconds 180)
worker_delay=$(option worker_stabilization_seconds 10)
activation_delay=$(option activation_delay_seconds 10)
log_dir="$REPO_ROOT/.codex/temp/local-environment-$(date +%Y%m%d-%H%M%S)"
api_specs=(
  'AuthService|FlowChat.AuthService.API|7236|5234'
  'ChatService|FlowChat.ChatService.API|7254|5254'
  'UserProfileService|FlowChat.UserProfileService.API|7148|5054'
  'PresenceService|FlowChat.PresenceService.API|7198|5098'
  'NotificationService|FlowChat.NotificationService.API|7206|5206'
  'RealtimeService|FlowChat.RealtimeService.API|7215|5215'
  'GatewayService|FlowChat.GatewayService.API|7270|5270'
  'HarnessService|FlowChat.HarnessService.API|7300|5300'
)
worker_specs=(
  'AuthService|FlowChat.AuthService.Consumers'
  'AuthService|FlowChat.AuthService.OutboxPublisher'
  'ChatService|FlowChat.ChatService.Consumers'
  'ChatService|FlowChat.ChatService.OutboxPublisher'
  'UserProfileService|FlowChat.UserProfileService.Consumers'
  'UserProfileService|FlowChat.UserProfileService.OutboxPublisher'
  'NotificationService|FlowChat.NotificationService.Consumers'
  'RealtimeService|FlowChat.RealtimeService.Consumers'
  'PresenceService|FlowChat.PresenceService.Consumers'
  'PresenceService|FlowChat.PresenceService.OutboxPublisher'
  'HarnessService|FlowChat.HarnessService.Consumers'
)
port_open() { (echo >/dev/tcp/127.0.0.1/"$1") >/dev/null 2>&1; }
for spec in "${api_specs[@]}"; do
  IFS='|' read -r service project port http_port <<<"$spec"
  if port_open "$port"; then printf 'Port %s is already occupied; stop existing services first\n' "$port" >&2; exit 1; fi
done
step 'Resetting all FlowChat databases'
reset_args=()
if has_option skip_build; then reset_args+=(--skip-build); fi
bash "$SCRIPTS_DIR/Bash/PostgreSQL/reset-db.sh" "${reset_args[@]}"
if ! has_option skip_build; then
  step 'Building FlowChat backend'
  dotnet build "$REPO_ROOT/FlowChat.slnx"
  step 'Building ReactClient'
  (cd "$REPO_ROOT/ReactClient" && npm run build)
fi
mkdir -p "$log_dir"
pids=()
cleanup() {
  step 'Stopping services started by this script'
  local pid
  for pid in "${pids[@]}"; do
    kill -- -"$pid" 2>/dev/null || kill "$pid" 2>/dev/null || true
  done
  for pid in "${pids[@]}"; do wait "$pid" 2>/dev/null || true; done
  printf 'Service logs: %s\n' "$log_dir"
}
trap cleanup EXIT INT TERM
start_dotnet() {
  local service=$1 project=$2 kind=$3 port=${4:-} http_port=${5:-}
  local dir dll safe="${project,,}"
  safe=${safe//./-}
  if [[ $kind == api ]]; then dir="$REPO_ROOT/$service/src/$project"; else dir="$REPO_ROOT/$service/src/Workers/$project"; fi
  dll="$dir/bin/Debug/net10.0/$project.dll"
  require_file "$dll"
  if [[ $kind == api ]]; then
    (cd "$dir" && exec env ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="https://localhost:$port;http://localhost:$http_port" setsid dotnet "$dll") >"$log_dir/$safe.out.log" 2>"$log_dir/$safe.err.log" &
  else
    (cd "$dir" && exec env DOTNET_ENVIRONMENT=Development setsid dotnet "$dll") >"$log_dir/$safe.out.log" 2>"$log_dir/$safe.err.log" &
  fi
  pids+=("$!")
  printf 'Started %s (PID %s)\n' "$project" "${pids[-1]}"
}
step 'Starting backend APIs'
for spec in "${api_specs[@]}"; do IFS='|' read -r service project port http_port <<<"$spec"; start_dotnet "$service" "$project" api "$port" "$http_port"; done
step 'Starting consumers and outbox publishers'
for spec in "${worker_specs[@]}"; do IFS='|' read -r service project <<<"$spec"; start_dotnet "$service" "$project" worker; done
step 'Starting ReactClient'
(cd "$REPO_ROOT/ReactClient" && exec setsid npm run dev) >"$log_dir/react-client.out.log" 2>"$log_dir/react-client.err.log" &
pids+=("$!")
deadline=$((SECONDS + startup_timeout))
step 'Waiting for all HTTP applications'
while :; do
  for pid in "${pids[@]}"; do kill -0 "$pid" 2>/dev/null || { printf 'Process %s exited; see logs: %s\n' "$pid" "$log_dir" >&2; exit 1; }; done
  pending=()
  for spec in "${api_specs[@]}"; do
    IFS='|' read -r service project port http_port <<<"$spec"
    port_open "$port" || pending+=("$project:$port")
  done
  port_open 5173 || pending+=(ReactClient:5173)
  ((${#pending[@]} == 0)) && break
  ((SECONDS < deadline)) || { printf 'Services did not become ready: %s. Logs: %s\n' "${pending[*]}" "$log_dir" >&2; exit 1; }
  printf 'Still waiting for: %s\n' "${pending[*]}"
  sleep 2
done
step "Checking workers for ${worker_delay}s"
sleep "$worker_delay"
for pid in "${pids[@]}"; do kill -0 "$pid" 2>/dev/null || { printf 'Process %s exited; see logs: %s\n' "$pid" "$log_dir" >&2; exit 1; }; done
step 'Registering development users'
bash "$SCRIPTS_DIR/Bash/register-auth-users.sh" --activation-delay-seconds "$activation_delay"
step 'Local environment reset and user registration completed'
