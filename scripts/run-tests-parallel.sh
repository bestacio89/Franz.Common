#!/usr/bin/env bash
set -Eeuo pipefail

configuration="${1:-Release}"
parallelism="${TEST_PROJECT_PARALLELISM:-2}"
test_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/tests"

if ! [[ "$parallelism" =~ ^[1-9][0-9]*$ ]]; then
  echo "::error::TEST_PROJECT_PARALLELISM must be a positive integer."
  exit 2
fi

mapfile -d '' test_projects < <(
  find "$test_root" -type f -name '*.csproj' \
    ! -path '*/Franz.Common.AotSmokeTest/*' \
    ! -path '*/Franz.Common.Architecture.Tests/*' \
    ! -path '*/Franz.Common.EntityFramework.AzureCosmosDB/*' \
    -print0 | sort -z
)

if (( ${#test_projects[@]} == 0 )); then
  echo "::error::No independent test projects were discovered."
  exit 1
fi

printf 'Discovered %d independent test projects; running at most %d concurrently.\n' \
  "${#test_projects[@]}" "$parallelism"

printf '%s\0' "${test_projects[@]}" |
  xargs -0 -r -n 1 -P "$parallelism" bash -c '
    set -Eeuo pipefail
    configuration="$1"
    project="$2"
    project_name="$(basename "$project" .csproj)"

    echo "::group::Testing ${project_name}"
    start_time=$SECONDS
    dotnet test "$project" \
      --configuration "$configuration" \
      --no-build \
      --no-restore \
      --blame-hang-timeout 10m \
      --logger "trx;LogFileName=${project_name}.trx"
    elapsed=$((SECONDS - start_time))
    echo "Completed ${project_name} in ${elapsed}s."
    echo "::endgroup::"
  ' _ "$configuration"
