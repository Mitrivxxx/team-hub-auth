#!/usr/bin/env bash
set -euo pipefail

# Recreates the team-hub monorepo directory layout required by ProjectReference
# paths and the service Dockerfile (services/team-hub-auth/...).
#
# Usage:
#   prepare-monorepo-layout.sh <auth-repo-checkout> [output-root] [monorepo-ref]

AUTH_CHECKOUT="${1:?auth checkout path is required}"
OUTPUT_ROOT="${2:-${RUNNER_TEMP:-/tmp}/team-hub-ci}"
MONOREPO_REF="${3:-main}"
MONOREPO_URL="${TEAM_HUB_MONOREPO:-https://github.com/Mitrivxxx/team-hub.git}"

DEPS_DIR="$(mktemp -d)"
trap 'rm -rf "$DEPS_DIR"' EXIT

git clone --depth 1 --branch "$MONOREPO_REF" --filter=blob:none --no-checkout \
  "$MONOREPO_URL" "$DEPS_DIR/monorepo"

(
  cd "$DEPS_DIR/monorepo"
  git sparse-checkout init --cone
  git sparse-checkout set building-blocks aspire/TeamHub.ServiceDefaults
  git checkout "$MONOREPO_REF"
)

for required_path in building-blocks aspire/TeamHub.ServiceDefaults; do
  if [[ ! -d "$DEPS_DIR/monorepo/$required_path" ]]; then
    echo "::error::Missing '$required_path' on ${MONOREPO_URL}@${MONOREPO_REF}." >&2
    echo "::error::Push shared libs from team-hub monorepo before running CI." >&2
    exit 1
  fi
done

rm -rf "$OUTPUT_ROOT"
mkdir -p "$OUTPUT_ROOT/services"

cp -a "$DEPS_DIR/monorepo/building-blocks" "$OUTPUT_ROOT/building-blocks"
cp -a "$DEPS_DIR/monorepo/aspire" "$OUTPUT_ROOT/aspire"
cp -a "$AUTH_CHECKOUT" "$OUTPUT_ROOT/services/team-hub-auth"

echo "MONOREPO_ROOT=$OUTPUT_ROOT"
