#!/usr/bin/env bash
# Builds the smooth textured vehicle models with Blender (headless, offline). Then run Tools/setup.sh.
#   Tools/blender/build_models.sh [DumpTruck ...] [--preview]
set -eu
cd "$(dirname "$0")/../.."
mkdir -p Logs/Blender
BLENDER=${BLENDER:-blender}
"$BLENDER" -b --factory-startup --python-exit-code 1 --python Tools/blender/build_models.py -- "$@" 2>&1 | tee Logs/Blender/build.log | grep -E "\[LBW\]|Error|Traceback|File \"" 
exit "${PIPESTATUS[0]}"
