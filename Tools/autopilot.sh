#!/usr/bin/env bash
# Builds the Linux test player and runs the automated play-through. Screenshots + report in Logs/AutoPilot/.
set -u
cd "$(dirname "$0")/.."
if [ "${SKIP_BUILD:-0}" != 1 ]; then
  Tools/unity_batch.sh LittleBuildersWorld.EditorTools.BuildScript.LinuxBatch linux_build -buildTarget Linux64
  echo "LINUX BUILD exit=$?"; grep "\[LBW Build\]" Logs/linux_build.log
fi
rm -rf Logs/AutoPilot; mkdir -p Logs/AutoPilot
timeout 2700 xvfb-run -a -s "-screen 0 1280x720x24" Builds/Linux/LittleBuildersWorld.x86_64 -force-glcore -screen-fullscreen 0 \
  -screen-width 1280 -screen-height 720 -lbw-autopilot -lbw-shots "$PWD/Logs/AutoPilot" -logFile "$PWD/Logs/autopilot.log"
code=$?
echo "AUTOPILOT exit=$code"
cat Logs/AutoPilot/autopilot_report.txt 2>/dev/null || grep -E "AutoPilot|Exception|Error" Logs/autopilot.log | head -40
exit $code
