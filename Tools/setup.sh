#!/usr/bin/env bash
# Regenerates all art, audio, data, prefabs and scenes, then validates. Log: Logs/setup.log
set -u
cd "$(dirname "$0")/.."
if [ ! -f "Assets/TextMesh Pro/Resources/TMP Settings.asset" ]; then
  PKG=$(ls Library/PackageCache/com.unity.ugui@*/"Package Resources/TMP Essential Resources.unitypackage" | head -1)
  python3 -I Tools/extract_unitypackage.py "$PKG" .
fi
Tools/unity_batch.sh LittleBuildersWorld.EditorTools.ProjectSetup.RunBatch setup
code=$?
grep -E "\[LBW|error CS|Exception" Logs/setup.log | grep -v "^UnityEngine" | head -60
echo "SETUP exit=$code"
exit $code
