#!/usr/bin/env bash
# Art review: renders vehicles, props and town views to Logs/Showcase/*.png (run Tools/setup.sh first).
set -u
cd "$(dirname "$0")/.."
Tools/unity_batch.sh LittleBuildersWorld.EditorTools.ShowcaseRenderer.RunBatch showcase
code=$?
grep -E "\[LBW|error CS|Exception" Logs/showcase.log | head -30
echo "SHOWCASE exit=$code"
exit $code
