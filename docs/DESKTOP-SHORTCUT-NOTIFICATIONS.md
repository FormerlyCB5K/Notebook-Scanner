# Version 0.4 — style, keyboard shortcut and completion notifications

The approved direction uses the portfolio's before-dawn navy, broad blue/violet glows, sparse fixed stars, pale lavender, opaque text panels and neutral sans-serif type. Both apps draw their own static backgrounds; no animated or downloaded background asset is needed. Text remains functional: Notebook Scanner, Settings, Connected and Photo copied. Concept mockups may contain older illustrative headings; those slogans are not app copy.

## Desktop shortcut

Open Windows **Settings → Set keyboard shortcut**. The suggested chord is **Ctrl+Alt+N**; press another Ctrl+Alt+letter combination to change it, then Save. Remove clears the hotkey. Saving creates or updates a per-user `Notebook Scanner.lnk` in Windows Start Menu Programs, pointing at this executable. Windows Explorer manages the shortcut, so it can launch the app after its window is fully closed. The app itself does not need to run at login.

While already running, a named single-instance mutex/event routes another launch to the existing window rather than starting another Bluetooth receiver. The shortcut shows the laptop app; it does not remotely wake the phone or trigger its camera. Keep the executable in its chosen folder. If you move/update it, open the new executable once so its saved shortcut target is refreshed. Windows policies, shortcut conflicts and foreground-window rules still need a user desktop trial; choose another chord if necessary. No shortcut was registered globally by the agent: the COM smoke test saved a `.lnk` under `.tools` only.

## Completion notifications

Windows uses a local tray notification after the image is committed to the clipboard. The desktop Settings checkbox enables/disables it and persists the choice. It uses the built-in WinForms `NotifyIcon` API, avoiding a server or packaged-app identity requirement. OS notification/Do Not Disturb settings may suppress the popup. [Microsoft NotifyIcon documentation](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon.showballoontip).

Android posts a local notification only after receiving the existing clipboard ACK from Windows. In the phone's Settings tab, enable **Notify when copied to laptop**, or tap **Allow completion notifications**. Android 13+ requests its standard notification permission at that moment. Denial leaves photo transfer working. Notifications have a dedicated completion channel, use a monochrome notebook icon and open the scanner when tapped. They contain no page thumbnail. [Android notification permission](https://developer.android.com/develop/ui/views/notifications/notification-permission).

These are local completion notifications, not a cloud push service. The image still travels over the existing secure Bluetooth link. No transfer size, quality, framing or speed tuning was changed; the user's roughly five-second report remains the accepted baseline.

## Verification

Both apps compile and Android lint passes. Existing protocol, retry/preferences and saved-device policy tests pass. A hidden Windows rendering path verifies the desktop layout without starting Bluetooth or creating a Start Menu shortcut. A workspace-only WScript.Shell shortcut test confirmed that the target and Ctrl+Alt+N chord can be saved and read back; this does not test Explorer's actual global key registration. Android APK signature continuity is checked before delivery.

Still needs user testing: saved shortcut from closed/minimized/running states; restoring the existing receiver without a second instance; actual Windows notification appearance; Android permission allow/deny and copy completion; the Android appearance on the S23. The Windows rendering is a native control adaptation, not a claim of pixel identity with the generated concept boards.

Design references in `docs/design` were generated with the built-in image generation tool. Prompt summary: two flat Windows/Android UI mockups, compact and spacious, using the supplied #151824 / #edeef8 / #b6b8cc / #bcb7e7 / #1b2030 palette, broad blue/violet glows and two sparse static star clusters; scanner Settings rather than an embedded viewfinder. The user approved the visual style and then requested removal of promotional headings. The implementation uses native vector/painted backgrounds rather than the generated images as runtime assets.

Desktop copy follow-up: the main screen includes the Bluetooth-on / no-WiFi reminder. A small bottom-right portfolio link opens the user's site in the default browser only when clicked. Android is unchanged.
