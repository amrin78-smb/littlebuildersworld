#!/usr/bin/env bash
# Builds the smooth textured environment props with Blender (headless, offline). Then run Tools/setup.sh.
#   Tools/blender/build_env.sh [Nature Street ...] [--preview]
set -eu
cd "$(dirname "$0")/../.."
mkdir -p Logs/Blender
BLENDER=${BLENDER:-blender}
"$BLENDER" -b --factory-startup --python-exit-code 1 --python Tools/blender/build_env.py -- "$@" 2>&1 | tee Logs/Blender/build_env.log | grep -E "\[LBW\]|Error|Traceback|File \"" 
exit "${PIPESTATUS[0]}"
