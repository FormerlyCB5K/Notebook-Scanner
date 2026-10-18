# Building the local friends-and-family beta

These instructions are for the maintainer. Users need only the installer and APK; they do not need SDKs or compiler tools. There is no automatic public upload.

## Windows

Use the .NET SDK specified by `global.json` and Inno Setup 6.7.3 or newer. In PowerShell:

```powershell
./tools/build-windows-installer.ps1 -Compiler 'C:/path/to/Inno Setup/ISCC.exe'
```

This publishes the self-contained Windows x64 app and compiles `tools/NotebookScanner.iss` into `dist/v0.5/windows/NotebookScanner-0.5-Windows-Setup.exe`. The app and installer are unsigned. The installer uses per-user LocalAppData Programs, a Start Menu shortcut, an optional desktop shortcut, an uninstaller, and no startup-at-login entry. It requires Windows build 19041 or later; support still depends on a functioning Bluetooth Classic adapter/driver.

`tools/test-windows-installer.ps1` can check install/reinstall/uninstall in a temporary workspace folder only if no installed copy is registered. It suppresses shortcut creation with `/SKIPSHORTCUTS=1`, does not launch the app, and checks the existing Start Menu shortcut stays unchanged. This is not a substitute for testing the default interactive install on a fresh user's computer.

## Android signing — private files

Use JDK 17 and the Android SDK versions in README. PowerShell 7 is required for the signing helper. Set JAVA_HOME, ANDROID_HOME, and, if needed, GRADLE_USER_HOME for your build machine.

The owner's release key has already been generated. **Reuse it. Do not generate another key for an update.** It lives in the ignored `.private-release` folder, which is restricted to the current Windows user. `release.jks` is password protected; its random password is in `release-password.txt` in that same protected folder. No password is printed, passed on the command line, or committed. This is local access protection, not an independent backup.

Before sharing beyond the first local test, the owner must save the key and password in a password manager with file attachments or another independent encrypted backup. Do not send the signing folder with the application or put it in an ordinary shared folder. Preserve this identity for subsequent APKs and consider the same key when setting up Play App Signing later.

For a genuinely new project that has never signed a release, `tools/new-android-signing-key.ps1` creates a key and refuses to overwrite an existing folder. It is not part of the normal build.

```powershell
./tools/build-android-release.ps1
# Or, with your restored private backup:
./tools/build-android-release.ps1 -SigningFolder 'C:/private/signing-backup'
```

The helper temporarily supplies signing credentials through the process environment, runs release build/lint, and restores prior environment values. Gradle refuses release tasks without credentials. It produces `dist/v0.5/android/NotebookScanner-0.5.apk`. Release builds are not debuggable and contain license/notice assets. Do not enable verbose environment/debug logging while credentials are present.

Public certificate SHA-256 fingerprint (not a secret):

```text
9088294c2a460ad65d2f2b36182dbb7ac5eaf92f3ac6f990ebab1d2449fc17ca
```

Verify every distributed APK with Android build-tools `apksigner verify --verbose --print-certs`, checking this fingerprint, versionCode/versionName, permissions, and absence of `application-debuggable` in `aapt dump badging`. The old 0.4 development certificate is different: installing 0.5 on such a device requires uninstalling the development app, clearing preferences. Fresh testers are unaffected. Future builds with this release key should install as updates, subject to actual device testing.

## Share package

```powershell
./tools/package-beta.ps1
```

This creates `dist/v0.5/NotebookScanner-0.5-Friends-and-Family.zip` from explicit installer/APK/instructions/license/checksum paths. It never recursively collects the workspace or the private signing folder. Inspect the ZIP and verify its checksums before sharing. `docs/FRIENDS-AND-FAMILY.txt` is the included beginner guide. No GitHub Release or store listing has been published.
