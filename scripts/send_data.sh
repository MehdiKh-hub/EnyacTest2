#!/usr/bin/env bash
# Send data to /api/data/send
# Usage:
#   BASE_URL=http://localhost:5080 ./send_data.sh "عنوان" "محتوا"
# Requires token in ./ .token or TOKEN env var

set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5080}"
TITLE="${1:-تست}"
PAYLOAD="${2:-این یک محموله نمونه است}"
TOKEN="${TOKEN:-}"
if [ -z "$TOKEN" ] && [ -f .token ]; then
  TOKEN=$(cat .token)
fi

if [ -z "$TOKEN" ]; then
  echo "No token found. Run ./login.sh or set TOKEN env var."
  exit 1
fi

resp=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/data/send" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"$TITLE\",\"payload\":\"$PAYLOAD\"}")

http_code=$(echo "$resp" | tail -n1)
body=$(echo "$resp" | sed '$d')

if [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
  (command -v jq >/dev/null 2>&1 && echo "$body" | jq .) || echo "$body"
else
  echo "Request failed (HTTP $http_code):"
  echo "$body"
  exit 1
fi
