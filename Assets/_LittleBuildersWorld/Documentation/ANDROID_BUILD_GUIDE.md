# Android Build Guide — Little Builders World

## Requirements

* Unity **6000.3.27f2** with Android Build Support (SDK, NDK, OpenJDK modules from Unity Hub).
* No other installs. No accounts, services, ads, analytics or network permission.

## One-time / after changing a generator

```bash
Tools/setup.sh          # regenerate art, audio, data, prefabs, scenes; validates (exit 0 = OK)
```
In the Editor: **Little Builders World ▸ Setup Project (generate everything)**.

## Build

```bash
Tools/build_android.sh            # development APK → Builds/Android/LittleBuildersWorld-dev.apk
Tools/build_android.sh release    # release APK     → Builds/Android/LittleBuildersWorld.apk
Tools/verify_apk.sh <apk>         # checks below (run automatically by build_android.sh)
```
Editor menu: **Little Builders World ▸ Build Android APK (development | release)**.

Every build re-applies these settings from code (`ProjectConfigurator.ConfigurePlayerSettings`), so they cannot
silently drift:

| Setting | Value | Why |
|---|---|---|
| Application id | `com.studio.littlebuildersworld` | neutral placeholder until the publisher id is chosen |
| Scripting backend | IL2CPP | |
| **Architectures** | **ARMv7 + ARM64** | 32-bit-only Android phones (e.g. **Redmi A3**) refuse 64-bit-only APKs. Costs ~15–20 MB. |
| **Graphics APIs** | **Vulkan, OpenGL ES 3** + Vulkan device deny filter **`.*PowerVR.*`** | On the Redmi A3 (PowerVR GE8320) a Vulkan Unity build ran once, then crashed at gameplay start on every later launch; with PowerVR on GLES3 it worked (Roar Rally finding). Filter asset: `Settings/VulkanDeviceFilters.asset`. |
| Orientation | Landscape left/right only | |
| Min SDK | 25 (Android 7.1) | Unity 6 minimum is 23; 25 matches the owner's other games |
| Target SDK | highest installed | |
| Internet permission | not forced | no network use; development builds add it for the profiler |
| Format | APK (not AAB) | sideloading for testing |

### What `verify_apk.sh` checks

1. `aapt dump badging` → `native-code: 'arm64-v8a' 'armeabi-v7a'` (both required).
2. `.*PowerVR.*` is present in the files under `assets/bin/Data/` (`globalgamemanagers` or a `.splitN` part).
3. Landscape orientation in the manifest.

## Install and test on a phone

```bash
adb devices
adb install -r Builds/Android/LittleBuildersWorld-dev.apk
adb shell monkey -p com.studio.littlebuildersworld 1      # launch
adb logcat -s Unity                                       # Unity log
```

**Second-launch test (mandatory on PowerVR phones such as the Redmi A3):** install, play, close the game fully
(swipe it away from Recent apps), open it again and drive. The PowerVR/Vulkan crash only showed on the second
launch.

### Device notes (from this machine)

* The device currently paired over Wi-Fi (`192.168.31.111:5555`) is a **Xiaomi Mi Box 4** (Android TV, API 28,
  32-bit, **Mali-450 = OpenGL ES 2.0 only**). Unity 6 requires OpenGL ES 3.0 or Vulkan, so **no Unity 6 game can
  run on it**; it is not a usable test device (and has no touch screen).
* Target test phone: **Redmi A3** (32-bit Android, PowerVR GE8320). Roar Rally measured 30–44 FPS there.

## Performance budget (budget phones)

* Requested 60 FPS (`GameConfig.targetFrameRate`); expect 30–45 on a Redmi A3-class phone. Measure on the Redmi,
  not a flagship.
* One directional light with hard shadows (40 m, 1 cascade, 1024 map), no HDR, no post-processing, 2× MSAA.
* `PerformanceGovernor` drops render scale to 0.8 and then shadows if the average stays under 40 FPS
  (logged as `[LBW Perf]` in logcat).
* Static props are static-batched; materials are a small shared palette.

## Release checklist

* Replace the application id and set a release keystore (Player Settings ▸ Publishing Settings).
* Bump `bundleVersion` / `bundleVersionCode`.
* Build `release`, run `verify_apk.sh`, do the second-launch test on the Redmi A3 and one 64-bit phone.
