# Validation record — 18 October 2026

## Version 0.4 follow-up

The user confirmed version 0.3 is sufficiently convenient and reported about five seconds per transfer. Version 0.4 applies the approved palette/static stars, removes slogan-style headings, adds configurable desktop launch/show shortcuts and local clipboard-completion notifications on Windows/Android. Protocol and compression remain unchanged.

Both updated builds compile. Android lint passes with zero errors; warnings concern English UI literals, pinned dependencies and compatibility recommendations. The existing 63 protocol checks, 8 Windows retry/preferences scenarios and 5 Android saved-device policy cases pass. Windows preview rendering is visually inspected; Android appearance is not inspected on physical hardware. WScript.Shell shortcut-file creation/hotkey round-trip passes under `.tools`, with no global shortcut installed by the agent. Actual keyboard activation, single-instance foreground behavior and notifications require the user trial described in [version 0.4 notes](DESKTOP-SHORTCUT-NOTIFICATIONS.md).

The one new Android permission is POST_NOTIFICATIONS, requested only from Settings. No internet, Wi-Fi, location or direct camera permission was added. APK signatures are verified before packaging. These remain unsigned Windows/debug-signed Android test builds.

## Completed in this workspace

| Check | Result |
| --- | --- |
| Windows source compilation | Succeeded using WinForms + the real WinRT SDK projection |
| Self-contained Windows x64 publish | Succeeded with SDK 8.0.426; runtime configuration confirms bundled Microsoft.NETCore.App and Microsoft.WindowsDesktop.App **8.0.32** |
| Protocol tests | **63 passed**: byte-by-byte fragmentation, consecutive frames, truncation at every boundary, zero/oversized lengths, wrong digest, non-JPEG, oversized dimensions, unsupported JPEG coding, bad segment length, cancellation |
| Version 0.2 connection/preferences tests | **8 scenarios passed**: serial retry and cleanup, cancelling an in-flight attempt, cancelling backoff without another attempt, access denial without repeated prompts, pre-cancelled startup, and missing/valid/replaced/corrupt preference handling |
| Android debug build | `assembleDebug` succeeded with JDK 17, Gradle 8.9, AGP 8.7.3, platform 35/build tools 34.0.0 |
| Android lint | Passed, **0 errors**. Remaining warnings concern English UI literals and a deliberately pinned AndroidX Core version; no permission errors are suppressed |
| Android lint freshness | Reran `lintDebug --rerun-tasks --offline` after final manifest changes; succeeded |
| APK signature | Android `apksigner verify --verbose` succeeded (v2 signature, development key) |
| Packaged Android permissions | Bluetooth through API 30; Bluetooth Connect; AndroidX-generated app-private signature permission. **No Internet, Wi-Fi, location, broad media, or direct camera permission** |
| Gradle wrapper integrity | JAR SHA-256 matches Gradle's official wrapper checksum; Gradle distribution SHA-256 pinned in wrapper properties |
| SDK integrity | Downloaded portable .NET SDK archive hash verified against Microsoft's release metadata |

The small JPEG fixture in protocol tests validates frame parsing/header limits, **not native JPEG rendering or camera image quality**. The test suite does not establish Bluetooth interoperability or clipboard behavior.

Test outputs:

* `dist/win-x64/NotebookScanner.exe`: self-contained compressed x64 Windows test executable, about 74 MiB; unsigned. Vendor notices are beside it.
* `dist/android/NotebookScanner-debug.apk`: about 4.9 MiB, debug-signed Android test APK; not a production release.
* `dist/NotebookScanner-source.zip`: original source, wrapper, instructions, workflow and vendor notices; excludes developer SDKs/caches, generated builds and synced project references.
* `dist/SHA256SUMS.txt`: hashes for these outputs. Hashes detect accidental changes; they do not establish publisher trust.

The tools needed to create these outputs were kept in `.tools` inside the project. Nothing under `sources/` was changed. No GitHub repository, public release, automation, store submission or online service was created.

## User-confirmed hardware trial

After the initial build, the user installed the Android APK on their Samsung S23, connected it to the Windows receiver and successfully pasted both an ordinary photo and a notebook-page photo into this chat. The user explicitly confirmed that the workflow works. The notebook's main text, diagrams and quantifiers were readable; some small subscripts required more effort.

This is user-reported end-to-end evidence on one device pair, not a controlled reliability benchmark. I did not operate the phone myself. The phone's Android version, Windows version, radio/driver details, Wi-Fi/mobile-data state and timings were not reported.

The first Windows connection attempt occurred before the phone app was installed. Service discovery returned an empty list with API result `Success`, which the UI misleadingly appended to its failure message. The receiver source now separates API failure from an absent scanner service, and the absent-service message gives phone installation/listening instructions.

The follow-up Windows wording-fix build compiled and published successfully. Because the original executable was still in use, that corrected build was delivered alongside it as `dist/win-x64/NotebookScanner-updated.exe`; the running original was not stopped.

The user subsequently reported that the transfer works **without Wi-Fi** and that transfer speed is perfectly acceptable. The main bottleneck was manually connecting/listening. Precise timings and the mobile-data state were not separately reported. No further transfer optimization was requested.

## Version 0.2 build checks

Both updated applications compile successfully. The Windows self-contained build is at `dist/v0.2/windows/NotebookScanner.exe`; the Android update is at `dist/v0.2/android/NotebookScanner-0.2.apk` (versionCode 2, versionName 0.2.0). Android lint passes with zero errors and warnings limited to English UI text and the deliberately pinned AndroidX version. The new APK verifies and its signing-certificate SHA-256 matches the original local APK, allowing an in-place update without uninstalling.

The new Windows retry supervisor and device-memory code pass the simulated scenarios above. The NS01 protocol and image encoding are unchanged. Old builds remain available as a fallback. Neither app was automatically installed or restarted on the user's devices.

Automatic reconnecting, remembering choices across app restarts, Android idle-disconnect detection and avoiding replay during a connection change **have not yet been exercised on the S23**. See [the update acceptance trial](CONNECTION-UX.md). Compilation and simulated tests do not establish those hardware behaviors.

## Version 0.3 build checks

The user reported version 0.2 was an improvement but still wanted automatic saved-device choice and direct camera launch. Version 0.3 now passes Android compilation/lint and Windows self-contained publishing. Android lint has **0 errors, 26 warnings** (English UI text and the deliberately pinned AndroidX Core version). The original **63 protocol checks** and **8 Windows retry/preferences scenarios** pass. **5 Android saved-device policy cases** also pass, covering first setup, legacy migration, successful-versus-failed device choice and absent/unpaired peers. That Java policy check is included in CI.

The Android APK is versionCode 3/versionName 0.3.0, verifies successfully, and has the same signing certificate as version 0.2. It can update in place. Its packaged permissions remain Bluetooth/Connect plus the AndroidX app-private signature permission: no direct camera, internet, Wi-Fi or location permission was added. The local Camera/Settings screens invoke the existing standard camera Activity, with no lens-selection extras.

Outputs are `dist/v0.3/android/NotebookScanner-0.3.apk`, `dist/v0.3/windows/NotebookScanner.exe` and the source archive under `dist/v0.3`. Prior outputs remain available. No app was remotely installed, closed or restarted.

The tests do **not** exercise automatic camera launch/resume, Back-to-Settings behavior, avoidance of camera relaunch loops, pending-photo delivery before the first connection, recent-device order on the S23, or Samsung camera preferences. Those require the short trial in [CAMERA-SHORTCUT.md](CAMERA-SHORTCUT.md). First-time setup is preserved in source; its updated screen layout has not been visually inspected on a phone.

## Still not tested

* Explicitly disabling mobile data in addition to Wi-Fi; Wi-Fi-off operation is user confirmed.
* Repeated connections, exact Windows consent behavior, and pairing-code UX.
* Fine pencil and smaller writing, orientation coverage, compressed sizes and measured transfer speed.
* Reliability of keeping the transport alive while different OEM camera apps are on top.
* Clipboard persistence after closing the receiver and busy-clipboard retry behavior.
* Recovery from radio loss, range, locked phone, process death or lost acknowledgement.
* Installation on a clean Windows machine without any developer software/runtime; x64 self-contained publishing is configured and built, but clean-machine installation was not exercised.
* Android 9/11/12+ device coverage, OEM battery management, Android installation policies and ARM64 Windows.
* GitHub Actions execution. The workflow is included, but no remote build was dispatched.

The basic capture-to-paste path and Wi-Fi-off operation have now been demonstrated on the user's device pair. Next check version 0.2's simpler connection setup and repeated-use reliability. The user has accepted transfer speed; detailed timing remains optional rather than a blocker for this personal trial.

## First hardware session record

Fill this in while testing; blank cells mean untested, not pass.

| Item | Result |
| --- | --- |
| Android phone/model/version | Samsung S23; Android version not reported |
| Windows version, Bluetooth adapter/driver | |
| Local JPEG paste into Paint | |
| Photo paste into ChatGPT | User confirmed; photo and notebook page received in this chat |
| Manual OS Bluetooth photo transfer with Wi-Fi/data off | |
| App photo transfer with Wi-Fi off | User confirmed successful; mobile-data state not separately reported |
| Custom-service connection and consent | Connection worked after installing phone app; consent details not reported |
| First capture → clipboard, compressed size and seconds | |
| Transfer-speed feedback | User reports speeds are perfectly fine; connection/listening steps are the bottleneck |
| Notebook-page readability | Main handwriting, diagrams and quantifiers readable in the submitted photo; fine-pencil and orientation coverage pending |
| 20-photo median and 95th-percentile total latency | |
| Disconnect/permission/camera-cancel failure checks | |

The original proposed latency gates were median below 10 seconds and 95th percentile below 20 seconds. The user has accepted subjective speed for their workflow, so prioritize removing connection steps. Broader distribution can revisit formal timing and readability coverage later.

Desktop reminder/portfolio follow-up: Windows publish succeeded, and the rendered main screen was visually checked for readable reminder text and a bottom-right portfolio link. Android was unchanged. The new executable is in dist/v0.4/windows-update because the prior build was running.
