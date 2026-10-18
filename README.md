# Notebook Scanner

**Source preview — downloadable releases have not been published yet.** The paths below describe local test builds and are not files included in this repository. Packaging and public distribution are still being decided.

**Latest local beta: version 0.5.** Share `dist/v0.5/NotebookScanner-0.5-Friends-and-Family.zip`, containing a Windows installer, a release-signed Android APK, and START-HERE.txt. See [the beginner instructions](docs/FRIENDS-AND-FAMILY.txt) and [maintainer build/signing notes](docs/BUILDING-RELEASE.md). Windows remains unsigned. If upgrading a phone from the earlier 0.4 development APK, uninstall that old phone app first: its certificate differs, so this clears saved preferences. Fresh installations do not need this step. No public download release has been published.

* Desktop: **Settings → Set keyboard shortcut**, choose a Ctrl+Alt+letter chord and Save. The Windows Start Menu shortcut can launch the app when closed and show the existing app when running. Close older copies before using this version.
* Phone: press Back from the standard camera, open **Settings**, and tap **Allow completion notifications** once. Android may ask permission. Desktop notifications have their own checkbox in desktop Settings.
* Notifications mean **copied to the laptop clipboard**, not submitted to ChatGPT. Speed tuning is deferred; image quality and Bluetooth transfer behavior remain the same.

See [shortcut, notification and validation details](docs/DESKTOP-SHORTCUT-NOTIFICATIONS.md).

An early Android → Windows notebook-photo-to-clipboard experiment. Open both apps, connect over Bluetooth, take and confirm a photo, then press **Ctrl+V** in your unsent draft. No automatic message sending, account, ads, network server or internet API.

**Status:** prototype, not a public release. The user confirmed capture-to-paste on a Samsung S23, operation without Wi-Fi and acceptable transfer speed. Version **0.3** restores the last successful device directly and automatically opens the phone's usual camera after setup. The user confirmed that camera/Settings flow; the new version 0.4 shortcut and notifications still need a device trial. Read [the architecture decision](docs/ARCHITECTURE.md), [camera shortcut notes](docs/CAMERA-SHORTCUT.md) and [validation record](docs/VALIDATION.md).

## Start here — using the test builds

You do **not** need developer tools to use prebuilt versions. The local `dist` folder contains build outputs when produced. Windows x64 is the first desktop target; ARM64 can be built separately. The phone requires Android 9 or later and Bluetooth Classic. The Windows source targets Windows 10 build 19041 or later; use a currently maintained Windows installation.

1. Close older Windows copies of Notebook Scanner, then run **`dist/v0.5/windows/NotebookScanner-0.5-Windows-Setup.exe`** to install, and open Notebook Scanner from the Start Menu. It bundles .NET and remains unsigned. Public signing/distribution is unfinished.
2. Transfer **`dist/v0.5/android/NotebookScanner-0.5.apk`** to your phone and open it to install/update Notebook Scanner. You can use Windows right-click → Show more options → Send to → Bluetooth device, select your S23, accept the transfer, and open the received APK from My Files/Downloads. No cable or developer mode is needed. This is a release-signed test build. For an existing 0.4 development installation, uninstall that old phone app first and select the laptop again; fresh testers can install directly. Preserve the release signing identity for future updates.
3. Keep Bluetooth enabled and the devices paired in normal OS settings. Existing pairing is enough; you do not need to pair again for each session.
4. **First-ever setup:** allow Nearby devices if asked, choose your laptop, and tap **Connect to this laptop** in Settings. Later updates signed with the same release key should retain your choice. Moving from the old development APK requires choosing again after uninstalling it.
5. **First-ever Windows setup:** choose your phone and click **Connect**. Windows remembers successful connections and waits/retries if the phone app is not ready yet. Approve Bluetooth access if Windows asks.
6. For your first photo after brand-new setup, tap the **Camera** tab. On later app opens the standard camera launches automatically. Photograph your page and accept the phone camera's confirmation. Keep the phone unlocked.
7. Wait for **Copied on laptop**, click your draft, and press **Ctrl+V**. The app never presses Send. Another successful photo replaces the clipboard image.

**From then on:** open both apps in either order. The phone opens its usual camera while Bluetooth connects to the remembered laptop; Windows retries its saved phone. A photo taken before connection is ready waits in memory for that selected laptop and sends once connected. You no longer choose the device or tap Open camera each time you open the app. After taking a photo, the Camera tab has an **Open camera** button for another page.

**Settings:** press Back in the phone camera to return directly to the scanner's Settings tab. You can change laptops there or turn off **Open camera when I open the app**. The scanner does not alter the standard camera's own lens/mode settings. Bluetooth must remain on; this does not remotely wake a locked phone.

**Pause/change devices:** use **Pause connection** on Windows, or **Pause / change laptop** on Android. Choose another paired device and connect explicitly. Pause lasts for the current running app instance; press Connect to resume, or fully close and relaunch the app. Simply returning to a paused Android app from Home may leave it paused. If a saved device is no longer paired, the app asks for a choice and never silently substitutes another device.

If a connection fails, the apps reconnect while open. An already attempted/interrupted photo is **not resent automatically**: retake it after reconnection. A photo captured before any connection is the one exception: it waits, unsent, for its selected laptop. Pause/change laptop or closing the app discards that pending photo. If Windows copied but the acknowledgement was lost, the phone may report failure even though the image is already available; check before retaking. **Test clipboard with local JPEG** remains available on Windows when paused to isolate paste problems.

The original transfer speed was acceptable in the user's trial, so version 0.3 keeps the same JPEG size/quality and Bluetooth transport. A system camera or app process can still be killed or behave differently across phones; this prototype does not promise an always-on background service or remote camera launching.

## Build from source — only for developers

These instructions are for creating the apps, not ordinary use. First builds need internet to obtain compiler dependencies. The downloaded runtimes/libraries are bundled as appropriate; they are not an end-user SDK requirement.

### Windows

Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), version **8.0.426** or a later patch in the same feature band, on the build machine. The checked-in `global.json` prevents accidentally building with an old SDK/runtime. Open PowerShell in the project folder (the folder containing this README). Run:

```powershell
dotnet run --project tests/ProtocolTests.csproj
dotnet build windows/NotebookScanner/NotebookScanner.csproj
dotnet run --project windows/NotebookScanner/NotebookScanner.csproj
```

To make a runtime-bundled executable for another Windows PC:

```powershell
powershell -NoProfile -File tools/publish-windows.ps1
```

The executable is `dist/win-x64/NotebookScanner.exe`. For an ARM64 PC:

```powershell
powershell -NoProfile -File tools/publish-windows.ps1 -Runtime win-arm64
```

If a local script policy disallows scripts, run the equivalent command directly rather than changing that policy:

```powershell
dotnet publish windows/NotebookScanner/NotebookScanner.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o dist/win-x64
```

Do not substitute `dotnet build` output for this publish output on a PC without .NET. .NET 8 is an experiment compatibility pin; update to a supported LTS before public release.

### Android — graphical route

Install [Android Studio](https://developer.android.com/studio) on the **build machine only**. Open the existing `android` folder, not the whole repository as a new empty project. Allow the Gradle sync. In Settings → Build, Execution, Deployment → Build Tools → Gradle, choose a **JDK 17** Gradle JDK. If the bundled JDK is a newer major version, install/select JDK 17 for this pinned build.

In Tools → SDK Manager, install Android SDK Platform 35 and SDK Build-Tools 34.0.0. Accept the tool licenses yourself as prompted. The checked-in wrapper downloads Gradle 8.9 with distribution checksum verification; no separate Gradle installation is needed.

Use Build → Build App Bundle(s)/APK(s) → Build APK(s), or the equivalent “Generate APKs” command in your Studio version. The debug APK appears at `android/app/build/outputs/apk/debug/app-debug.apk`. Copy it to your phone and open it for installation. Emulator camera/UI checks can help development but do not establish Bluetooth interoperability.

### Android — command-line route

With JDK 17 and an Android SDK already set up, set `JAVA_HOME` to JDK 17 and `ANDROID_HOME` to the SDK directory. Android Studio may instead create a machine-local `android/local.properties` containing `sdk.dir`; do not commit it. From the project folder:

```powershell
./android/gradlew.bat -p android assembleDebug lintDebug --no-daemon
```

On macOS/Linux, use `bash android/gradlew -p android assembleDebug lintDebug --no-daemon`. The required SDK is Android platform 35, build tools 34.0.0. No NDK is needed. The package has no internet permission. The build system itself downloads Gradle/AndroidX/compiler dependencies.

### Optional builds through GitHub

The included `.github/workflows/build.yml` builds both platforms and runs the protocol checks and Android lint. If you choose to create a GitHub repository, upload this source and use its Actions page to run **Build proof of concept**. Download the two build artifacts after a successful run. **No repository has been created or uploaded by this task, and the workflow has not been run on GitHub.** Using GitHub for builds requires a developer GitHub account; installing/using the utility does not. CI debug signing keys can change between clean builds; uninstalling a previous debug build may be needed. Use a stable private signing key for actual releases.

## Where things are

| Folder | Purpose |
| --- | --- |
| `android` | Complete Android app, manifest, camera file provider and Gradle wrapper |
| `windows/NotebookScanner` | WinForms receiver, WinRT Bluetooth client, clipboard handling |
| `tests` | Dependency-free executable tests of frame parsing and rejection |
| `tools` | Self-contained Windows publishing helper |
| `docs` | Sources, architecture, wire protocol, validation and hardware plan |
| `dist` | Locally built test artifacts; excluded from version control |
| `.tools` | Local build dependencies/cache, if downloaded; excluded from version control |
| `sources` | Synced project references; read-only and untouched |

## Scope and future release

This is native Android + native Windows, deliberately platform-specific. The framing could be reused elsewhere, but macOS/iOS/Linux support would require new platform work. The system camera is the camera UI for now; it may add a confirmation tap. Remembered peers and reconnecting while apps are open are included. No document cropping, OCR, background service, tray icon, launch at login, remote shutter, clipboard monitoring or ChatGPT integration is included.

Before broad sharing: pass the hardware tests, fix relevant lifecycle/driver issues, update compiler/runtime targets, keep stable release signing keys, sign the Windows build, and produce a signed Android release APK or store bundle. Original source is MIT-licensed; see LICENSE. The Gradle wrapper retains its own upstream Apache-2.0 notices. See [the distribution plan](docs/DISTRIBUTION.md) for installation, signing, and the fresh-user beta trial. Public downloads remain unpublished.
