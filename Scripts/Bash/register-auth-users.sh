#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/lib/common.sh"
parse_options "$@"
require_command curl
require_command jq
register_url="$(option base_url https://localhost:7236)/$(option route api/users)"
notifications_url="$(option notification_base_url https://localhost:7206)/$(option notification_route internal/notifications/recent)"
confirm_url="$(option user_profile_base_url https://localhost:7148)/$(option user_profile_confirm_route api/userprofiles/email-verification/confirm)"
api_key=$(option notification_api_key dev-notification-internal-key)
delay=$(option activation_delay_seconds 10)
password='dRabina#098'
organizations=('Northwind Labs' 'Blue Orbit Studio' 'Riverstone Systems' 'Amberleaf Media' 'Polar Grid Works')
first_names=(Anna Piotr Marta Jakub Zofia Tomasz Julia Mikolaj Natalia Kacper)
last_names=(Nowak Wojcik Mazur Krawczyk Szymczak Dudek Sikora Baran Lis Pawlak)
created=0 failed=0 activated=0 not_activated=0

describe_user() {
  local index=$1 sequence=$((index + 1)) token
  printf -v token '%02d' "$sequence"
  if ((index < 50)); then
    first=${first_names[index % 10]}
    last=${last_names[index / 5]}
    organization=${organizations[index % 5]}
    friendly="${first,,}.${last,,}.${token}"
    email="${first,,}.${last,,}${token}@seed.flowchat.local"
  else
    first=Piotr
    printf -v last '%02d' "$((index - 50))"
    organization=${organizations[index % 5]}
    friendly="piotr.$last"
    email="piotr.$last@seed.flowchat.local"
  fi
  printf -v id '00000000-0000-0000-0000-%012x' "$sequence"
}

for ((index=0; index<150; index++)); do
  describe_user "$index"
  printf -v position '%03d/150' "$((index + 1))"
  if has_option dry_run; then
    printf '[%s] DRY-RUN %s <%s> [%s]\n' "$position" "$friendly" "$email" "$organization"
    continue
  fi
  payload=$(jq -n --arg id "$id" --arg friendly "$friendly" --arg email "$email" --arg password "$password" --arg first "$first" --arg last "$last" --arg organization "$organization" '{Id:$id,FriendlyUserId:$friendly,Email:$email,Password:$password,FirstName:$first,LastName:$last,Organization:$organization}')
  if curl -kfsS -X PUT -H 'Content-Type: application/json' --data "$payload" "$register_url" >/dev/null; then
    ((created+=1))
    printf '[%s] CREATED %s <%s>\n' "$position" "$friendly" "$email"
  else
    ((failed+=1))
    printf '[%s] FAILED %s <%s>\n' "$position" "$friendly" "$email" >&2
  fi
done
if has_option dry_run; then printf 'Dry run complete.\n'; exit 0; fi
printf 'Registration summary: created=%s failed=%s\n' "$created" "$failed"
if has_option register_only; then exit 0; fi
sleep "$delay"
notifications=$(curl -kfsS -H "X-Internal-Api-Key: $api_key" "$notifications_url")
for ((index=0; index<150; index++)); do
  describe_user "$index"
  body=$(jq -r --arg email "$email" '[.Notifications[]? | select(.Email == $email and (.Type == "EmailVerification" or .Type == 1))] | sort_by(.CreatedDate) | last | .Body // empty' <<<"$notifications")
  if [[ -z $body ]]; then ((not_activated+=1)); continue; fi
  link=$(grep -Eo 'https?://[^[:space:]"'\''<>]+' <<<"$body" | head -1 || true)
  link=$(printf '%s' "$link" | sed 's/[.,;)>]*$//')
  if [[ -z $link ]]; then ((not_activated+=1)); continue; fi
  curl -kfsS "$link" >/dev/null || true
  query=${link#*\?}
  token=''
  IFS='&' read -ra pairs <<<"$query"
  for pair in "${pairs[@]}"; do
    if [[ $pair == token=* ]]; then token=${pair#token=}; break; fi
  done
  token=${token//+/ }
  token=$(printf '%b' "${token//%/\\x}")
  if [[ -n $token ]] && curl -kfsS -X POST -H 'Content-Type: application/json' --data "$(jq -n --arg token "$token" '{Token:$token}')" "$confirm_url" >/dev/null; then
    ((activated+=1))
    printf 'ACTIVATED %s <%s>\n' "$friendly" "$email"
  else
    ((not_activated+=1))
    printf 'ACTIVATION FAILED %s <%s>\n' "$friendly" "$email" >&2
  fi
done
printf 'Activation summary: activated=%s not activated=%s\n' "$activated" "$not_activated"
