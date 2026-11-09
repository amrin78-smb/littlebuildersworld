#!/usr/bin/env bash
# Offline generation of the helper's voice clips (no recording, no network once the models are cached).
# English: Kokoro TTS (Apache-2.0, voice af_heart). Thai: VachanaTTS (MIT, voice th_f_2).
# Output: Assets/_LittleBuildersWorld/Audio/Voice/<lang>/<key>.wav  — then run Tools/setup.sh.
# Usage: Tools/voice/generate_voice.sh [key ...]   (no keys = every line)
set -euo pipefail
cd "$(dirname "$0")/../.."
T="${LBW_TTS_HOME:-$HOME/.cache/lbw-tts}"
if [ ! -x "$T/venv/bin/python" ]; then
  python3 -m venv "$T/venv"
  "$T/venv/bin/pip" install -q kokoro-onnx==0.6.1 vachanatts==0.0.7 requests soundfile
fi
mkdir -p "$T/models"
for f in kokoro-v1.0.onnx voices-v1.0.bin; do
  [ -s "$T/models/$f" ] || curl -sSL -o "$T/models/$f" "https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.0/$f"
done
"$T/venv/bin/python" -I Tools/voice/generate_voice.py Tools/voice/voice_lines.json "${OUT:-Assets/_LittleBuildersWorld/Audio/Voice}" "$T/models" "$@"
