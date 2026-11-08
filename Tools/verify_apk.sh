#!/usr/bin/env bash
# Checks an APK for the budget-phone requirements: 32-bit + 64-bit native code, PowerVR Vulkan deny filter,
# landscape, no INTERNET permission in release builds. Usage: Tools/verify_apk.sh <apk>
set -u
APK="$1"
[ -f "$APK" ] || { echo "No APK at $APK"; exit 1; }
SDK="$HOME/Unity/Hub/Editor/6000.3.27f2/Editor/Data/PlaybackEngines/AndroidPlayer/SDK"
AAPT=$(ls -d "$SDK"/build-tools/*/aapt 2>/dev/null | sort -V | tail -1)
fail=0
echo "APK: $APK ($(du -h "$APK" | cut -f1))"
BADGING=$("$AAPT" dump badging "$APK")
echo "$BADGING" | grep -E "^package:|native-code|sdkVersion|targetSdkVersion|uses-permission|application-label:"
echo "$BADGING" | grep native-code | grep -q "'arm64-v8a'" && echo "$BADGING" | grep native-code | grep -q "'armeabi-v7a'" \
  && echo "OK   native code: arm64-v8a + armeabi-v7a (installs on 32-bit Android such as the Redmi A3)" \
  || { echo "FAIL native code must list both arm64-v8a and armeabi-v7a"; fail=1; }
TMP=$(mktemp -d); unzip -q -o "$APK" 'assets/bin/Data/*' -d "$TMP" 2>/dev/null
if grep -rlaF ".*PowerVR.*" "$TMP/assets/bin/Data/" >/dev/null 2>&1; then
  echo "OK   PowerVR Vulkan deny filter present in $(grep -rlaF '.*PowerVR.*' "$TMP/assets/bin/Data/" | xargs -n1 basename | tr '\n' ' ')"
else echo "FAIL PowerVR filter not found under assets/bin/Data"; fail=1; fi
rm -rf "$TMP"
"$AAPT" dump xmltree "$APK" AndroidManifest.xml | grep -q 'screenOrientation.*=(type 0x10)0x6\|screenOrientation.*0xb' && echo "OK   landscape orientation" || echo "INFO orientation: $("$AAPT" dump xmltree "$APK" AndroidManifest.xml | grep -m1 screenOrientation)"
if echo "$BADGING" | grep -q "android.permission.INTERNET"; then
  case "$APK" in *-dev.apk) echo "INFO INTERNET permission present (development build: used by the Unity profiler)";;
                 *) echo "FAIL release APK must not request INTERNET (offline children's game)"; fail=1;; esac
else echo "OK   no INTERNET permission"; fi
exit $fail
