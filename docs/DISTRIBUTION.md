# Distribution plan — not a published release

The project owner selected MIT for original source and delegated installation/signing recommendations. Public downloads remain on hold pending review and a fresh-user trial.

## Recommended first beta

Use one GitHub Release page with two clearly labeled downloads:

* Windows: a standard per-user installer, built with Inno Setup. Install the bundled .NET application under the user's local Programs folder, add a Start Menu shortcut, offer an optional desktop shortcut, and provide an uninstaller. Users should not need administrator access, .NET, Java, Android Studio, or other developer tools. Keep the existing configurable hotkey. Do not enable startup-at-login or add an automatic updater for the first beta.
* Android: a release-signed APK downloaded directly on the phone. Document the one-time Android permission to install from that download source. Do not ask people to enable developer mode. Use manual download/install updates initially; app stores can be considered after the beta proves useful.

Keep setup instructions short: install both apps, turn on Bluetooth, pair once, select the other device in each app, take a photo, and paste. Downloading/installing requires obtaining the files, but photo transfer remains offline. Include checksums, license notices, supported platforms, and known limitations with the release.

## Signing decisions

Signing attaches a cryptographic publisher identity to a build. It does not certify that an app is bug-free.

Android: generate a dedicated private release signing key, never commit it or its password, and arrange a separate private backup controlled by the owner before distributing the first signed beta. Reuse the same signing identity for future APK updates. Do not publish our current debug APK as the public release. Transitioning from the existing development certificate to a different release certificate will normally require uninstalling the development app first (losing saved app preferences); the source and release instructions must make that clear before testing. Do not silently change package identity or signing keys.

Windows: prepare the installer first. Trusted Windows code signing is a separate decision involving publisher identity verification and potentially cost. Do not buy a certificate, create an account, or claim a trusted publisher without the owner's involvement. A limited unsigned beta is an option if its download page clearly explains that it is unsigned; a self-signed certificate is not a substitute for public trust. Signing does not guarantee that every Windows reputation warning disappears.

No private release key, signing-service account, paid certificate, or public binary release has been created as part of this plan.

## Fresh-user trial

The owner's roommate can test a locally shared beta before public distribution. Test without developer tools installed. Verify install/uninstall, first pairing and permissions, Wi-Fi-off capture-to-paste, reconnect after closing either app, hotkey launch/show, completion notifications only after copying, and installing a second build as an update. Record actual phone/Windows versions and any confusing steps. A local build or automated test cannot replace this trial.

## References

* [Inno Setup features](https://jrsoftware.org/isinfo.php): single EXE installers, non-administrative installation, shortcuts, and uninstall support.
* [Android app signing](https://developer.android.com/studio/publish/app-signing): release signing, key protection, and update identity.
* [GitHub Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases): downloadable versioned release assets.
* [MIT License](https://opensource.org/license/mit): original source terms; upstream component licenses remain separate.
