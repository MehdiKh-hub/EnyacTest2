#!/usr/bin/env bash
# Get current user profile
# Usage:
#   BASE_URL=http://localhost:5080 ./get_me.sh
# Requires token in ./ .token or TOKEN env var

set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5080}"
TOKEN="${TOKEN:-}"
if [ -z "$TOKEN" ] && [ -f .token ]; then
  TOKEN=$(cat .token)
fi

if [ -z "$TOKEN" ]; then
  echo "No token found. Run ./login.sh or set TOKEN env var."
  exit 1
fi

curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/users/me" | (command -v jq >/dev/null 2>&1 && jq . || cat)
