#!/usr/bin/env bash
# Android APK (development by default; pass "release" for a release build), then verifies it.
set -u
cd "$(dirname "$0")/.."
METHOD=AndroidDevelopmentBatch; APK=Builds/Android/LittleBuildersWorld-dev.apk
if [ "${1:-}" = "release" ]; then METHOD=AndroidReleaseBatch; APK=Builds/Android/LittleBuildersWorld.apk; fi
Tools/unity_batch.sh LittleBuildersWorld.EditorTools.BuildScript.$METHOD android_build -buildTarget Android
echo "ANDROID BUILD exit=$?"; grep "\[LBW Build\]" Logs/android_build.log
Tools/verify_apk.sh "$APK"
