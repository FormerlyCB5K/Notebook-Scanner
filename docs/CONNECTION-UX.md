# Version 0.2: fewer setup steps

This records the version 0.2 change. For the current version 0.3 automatic standard-camera shortcut and improved last-successful-device selection, see [CAMERA-SHORTCUT.md](CAMERA-SHORTCUT.md).

The user confirmed that the original phone → Bluetooth → clipboard → paste path works with Wi-Fi off and that transfer speed is fine. Repeated connection/listening steps were the actual bottleneck. Version 0.2 keeps the existing transport and image quality, and changes only connection setup/recovery.

## Behavior

* First use: choose a paired laptop on Android and press **Connect to this laptop**; choose the paired phone on Windows and press **Connect**. A default dropdown item is not itself permission to connect automatically.
* Later uses: open both apps in either order. Android restores its explicitly authorized laptop and starts its RFCOMM server; Windows restores its phone and retries service discovery/connection until ready.
* A saved peer that is absent from the paired-device list is not replaced automatically. Choose again explicitly.
* Windows retries serially with 2/4/8-second delays; Pause/closing cancels both current work and backoff. Access denied is not repeatedly retried.
* Android can accept a new connection from the same selected laptop after a disconnect. A socket reader detects EOF while idle and receives clipboard acknowledgements. Failed/pending photos are bound to their old connection and never replayed onto a new one.
* Pause disables reconnection for the current running app instance. Press Connect to resume, or fully close and relaunch; merely returning to an existing Android Activity may leave it paused. Android's foreground reconnect checks stop when the Activity is paused; its existing session may survive a camera handoff, but there is still no always-on service.
* The phone still requires a user tap to open its camera and accept the system camera result. It does not wake a locked/closed phone or start its camera remotely.

## Why this architecture

The change reuses Android's secure RFCOMM server and Windows' existing WinRT client. Explicit first selection plus the OS bond remains the authorization boundary. Already-paired communication uses `BLUETOOTH_CONNECT`; app-level reconnection adds no scan, location, Wi-Fi or camera permissions. [Android Bluetooth permissions](https://developer.android.com/develop/connectivity/bluetooth/bt-permissions), [secure RFCOMM connections](https://developer.android.com/develop/connectivity/bluetooth/connect-bluetooth-devices).

Android lifecycle callbacks limit the periodic reconnect check to the visible Activity and remove scheduled callbacks on pause/destroy. This does not promise that Android will retain the process while a camera app is on top or while the app is backgrounded. An app-open reconnect avoids adding a foreground service, companion-device association, notification or background camera behavior solely to remove a button. [Activity lifecycle](https://developer.android.com/guide/components/activities/activity-lifecycle).

Only the selected Bluetooth address (Android SharedPreferences) and Windows device ID (a local `NotebookScanner/phone.json` file under the user's LocalAppData) persist. No images are saved as a retry queue. Android backup exclusions cover the preferences. Desktop preferences can be reset by deleting that app-owned JSON file while the app is closed; deleting preferences does not unpair OS devices.

Windows rechecks that the saved phone is still paired on every attempt. Consent-required, user-disabled and policy-disabled results stop retrying and ask for attention rather than repeatedly requesting access. Ordinary unavailable/not-ready results remain retryable. [Microsoft Bluetooth error definitions](https://learn.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.bluetootherror).

## Acceptance trial still needed for the update

1. Install the new APK over the old version; open only the new Windows executable. Make the explicit device choices once.
2. Close both apps. Open Windows first, then Android: require an automatic Connected state without pressing either connection button.
3. Close both. Open Android first, then Windows: same result.
4. Take two photos; confirm each arrives once and is readable.
5. Pause Windows: confirm attempts stop. Resume with Connect. Pause Android: require no automatic restart until pressing Connect or reopening it.
6. Close/reopen one app while the other stays open, first idle and then during a photo. Require automatic connection recovery, and no replay of the interrupted photo. A copied photo may still be present after lost ACK; check before retaking.
7. Repeat with Wi-Fi off. Neither speed nor image quality should be intentionally changed.

The original hardware trial establishes the underlying transfer path. Compile/lint and simulated retry checks do not establish this new lifecycle/reconnection behavior on the S23; the trial above is still required.
