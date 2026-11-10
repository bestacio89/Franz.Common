#!/usr/bin/env bash
set -Eeuo pipefail

suite="${1:-unknown}"
mkdir -p container-diagnostics
{
  echo "## Suite: ${suite}"
  echo "## Docker version"
  docker version
  echo "## Docker daemon information"
  docker info
  echo "## Docker disk usage"
  docker system df
  echo "## All containers"
  docker ps -a --no-trunc
  echo "## Container inspection"
  container_ids="$(docker ps -aq)"
  if [[ -n "$container_ids" ]]; then
    docker inspect $container_ids
    while IFS= read -r container_id; do
      [[ -z "$container_id" ]] && continue
      echo "## Container logs: $container_id"
      docker logs --timestamps --details "$container_id" 2>&1 || true
    done <<< "$container_ids"
  fi
  echo "## Local image inventory"
  docker image ls --digests --no-trunc
} > container-diagnostics/docker-diagnostics.log 2>&1 || true
