#!/usr/bin/env bash
source "$(dirname -- "${BASH_SOURCE[0]}")/lib/common.sh"
parse_options "$@"
require_command jq
require_command curl
id=$(cat /proc/sys/kernel/random/uuid)
token=${id//-/}; token=${token:0:12}
first_names=(Anna Piotr Marta Jakub Zofia Tomasz Julia Mikolaj Natalia Kacper)
last_names=(Nowak Wojcik Mazur Krawczyk Szymczak Dudek Sikora Baran Lis Pawlak)
organizations=('Northwind Labs' 'Blue Orbit Studio' 'Riverstone Systems' 'Amberleaf Media' 'Polar Grid Works')
first=${first_names[RANDOM % ${#first_names[@]}]}
last=${last_names[RANDOM % ${#last_names[@]}]}
organization=${organizations[RANDOM % ${#organizations[@]}]}
friendly="${first,,}.${last,,}.$token"
email="${first,,}.${last,,}.$token@$(option email_domain seed.flowchat.local)"
password=$(option password 'dRabina#098')
url="$(option base_url https://localhost:7236)/$(option route api/users)"
payload=$(jq -n --arg id "$id" --arg friendly "$friendly" --arg email "$email" --arg password "$password" --arg first "$first" --arg last "$last" --arg organization "$organization" '{Id:$id,FriendlyUserId:$friendly,Email:$email,Password:$password,FirstName:$first,LastName:$last,Organization:$organization}')
printf 'RegisterUser endpoint: %s\nGenerated user:\nId: %s\nFriendlyUserId: %s\nEmail: %s\nPassword: %s\nFirstName: %s\nLastName: %s\nOrganization: %s\n' "$url" "$id" "$friendly" "$email" "$password" "$first" "$last" "$organization"
if has_option dry_run; then printf 'Dry run complete. User was not registered.\n'; exit 0; fi
response=$(curl -kfsS -X PUT -H 'Content-Type: application/json' --data "$payload" "$url") || { printf 'FAILED %s <%s>\n' "$friendly" "$email" >&2; exit 1; }
printf 'CREATED %s <%s>\n' "$friendly" "$email"
response_id=$(jq -r '.id // empty' <<<"$response")
[[ -z $response_id ]] || printf 'ResponseId: %s\n' "$response_id"
printf 'Email activation was not performed; account should remain unconfirmed.\n'
