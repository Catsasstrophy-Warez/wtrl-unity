#!/usr/bin/env bash
# Pure-bash structural validator — no Unity Editor, no Python required.
# Adapted from Rev16.1's validate_rev16_structure.sh (see PIVOT-PLAN.md),
# generalized since this project has no content yet to assert against.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

echo "[WTRL-Unity] Structural validation (no Editor/Python required)"

asmdefs=$(find Assets -name "*.asmdef")
asmdef_count=$(echo "$asmdefs" | grep -c . || true)
echo "Assemblies found: $asmdef_count"

cs_files=$(find Assets -name "*.cs" 2>/dev/null || true)
cs_count=$(echo "$cs_files" | grep -c . || true)
echo "C# files found: $cs_count"

# Brace-balance check per .cs file (crude but catches truncated pastes).
if [ -n "$cs_files" ]; then
  while IFS= read -r f; do
    [ -z "$f" ] && continue
    open=$(grep -o '{' "$f" | wc -l)
    close=$(grep -o '}' "$f" | wc -l)
    if [ "$open" -ne "$close" ]; then
      echo "FAIL: brace mismatch in $f ($open open vs $close close)"
      exit 1
    fi
  done <<< "$cs_files"
fi

# Duplicate assembly names.
names=$(for f in $asmdefs; do grep -oE '"name"\s*:\s*"[^"]+"' "$f" | head -1 | sed -E 's/.*"([^"]+)"$/\1/'; done)
dup=$(echo "$names" | sort | uniq -d)
if [ -n "$dup" ]; then
  echo "FAIL: duplicate assembly name(s): $dup"
  exit 1
fi

# Reference-cycle check: build an edge map, then DFS with a real visited/
# in-progress stack (bash associative arrays, not a language dependency).
declare -A refs_of
for f in $asmdefs; do
  name=$(grep -oE '"name"\s*:\s*"[^"]+"' "$f" | head -1 | sed -E 's/.*"([^"]+)"$/\1/')
  refs=$(grep -oE '"references"\s*:\s*\[[^]]*\]' "$f" | grep -oE '"[A-Za-z0-9_.]+"' | sed -E 's/"//g' || true)
  refs_of["$name"]=$(echo "$refs" | tr '\n' ' ')
done

declare -A visited in_progress
dfs() {
  local node="$1" path="$2"
  if [ "${in_progress[$node]:-0}" = "1" ]; then
    echo "FAIL: assembly reference cycle: $path -> $node"
    exit 1
  fi
  if [ "${visited[$node]:-0}" = "1" ]; then
    return
  fi
  in_progress[$node]=1
  for r in ${refs_of[$node]:-}; do
    if [ -n "${refs_of[$r]+set}" ]; then
      dfs "$r" "$path -> $r"
    fi
  done
  in_progress[$node]=0
  visited[$node]=1
}
for n in "${!refs_of[@]}"; do
  dfs "$n" "$n"
done
echo "Reference-cycle check: PASS (no cycles among $asmdef_count assemblies)"

# TODO/FIXME/NotImplementedException sweep, excluding this file itself.
if [ -n "$cs_files" ]; then
  hits=$(grep -lE "TODO|FIXME|NotImplementedException" $cs_files 2>/dev/null || true)
  if [ -n "$hits" ]; then
    echo "FAIL: unresolved TODO/FIXME/NotImplementedException marker(s) in:"
    echo "$hits"
    exit 1
  fi
fi

echo "PASS: $asmdef_count assemblies, $cs_count C# files, no brace mismatches,"
echo "no duplicate assembly names, no unresolved markers."
