#!/usr/bin/env bash
# Login script for MinimalApiJwtDemo
# Usage:
#   BASE_URL=http://localhost:5080 USERNAME=admin PASSWORD='P@ssw0rd!' ./login.sh
# Saves the token to ./ .token file

set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5080}"
USERNAME="${USERNAME:-admin}"
PASSWORD="${PASSWORD:-P@ssw0rd!}"

resp=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")

# Separate body and status
http_code=$(echo "$resp" | tail -n1)
body=$(echo "$resp" | sed '$d')

# Try to extract token with jq, fallback to sed
if command -v jq >/dev/null 2>&1; then
  token=$(echo "$body" | jq -r '.token')
else
  token=$(echo "$body" | sed -n 's/.*"token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')
fi

if [ "$http_code" -ne 200 ] || [ -z "$token" ] || [ "$token" = "null" ]; then
  echo "Login failed (HTTP $http_code):"
  echo "$body"
  exit 1
fi

echo "$token" > .token
echo "Token saved to .token"

if command -v jq >/dev/null 2>&1; then
  echo "expiresAtUtc: " $(echo "$body" | jq -r '.expiresAtUtc')
fi
