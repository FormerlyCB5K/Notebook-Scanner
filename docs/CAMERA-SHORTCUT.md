# Version 0.3: open the standard camera immediately

The user requested automatic last-device selection, direct camera opening after first-time setup, and a Settings tab. They clarified that camera selection is already governed by their phone camera's current settings and does not need changing. They explicitly chose **keep the standard camera and press Back for scanner Settings**, rather than an embedded camera requiring direct camera permission.

## Implemented behavior

* Brand-new installs retain the explicit paired-laptop setup. Existing version 0.2 preferences migrate automatically.
* Later opens/resumes of the scanner launch its normal `ACTION_IMAGE_CAPTURE` flow automatically, as soon as the saved paired laptop's listening session starts. This does not wait for the Windows app to connect.
* Camera and Settings tabs remain in the scanner itself. Back/cancel in the separate camera returns directly to Settings. Returning from a completed/cancelled camera result does not automatically relaunch it in a loop.
* Settings includes **Open camera when I open the app**, enabled by default. Turning it off keeps startup in Settings. The Camera tab/Open camera action remains available manually.
* The native camera's confirmation still authorizes sending the photo. No camera-facing or mode extras are supplied. No CameraX, new camera SDK, direct CAMERA permission, or new end-user setup was added.
* If the user captures before a laptop connection exists, the prepared JPEG waits in memory for that selected laptop only. There is at most one photo. It is offered once on connection and then removed from the pending slot. Pause/change target/closing discards it. It is not a disk retry queue.
* A capture begun on an existing connection still belongs to that socket; if it changes before delivery, the app asks for a retake instead of replaying the photo on a replacement connection.

## Saved-device fix

Version 0.2 restored the Spinner selection and immediately read it back to decide where to listen. Spinner selection/layout callbacks are asynchronous, so this creates avoidable timing dependence. Version 0.3 resolves the saved address directly, separately updates the dropdown for display, and passes the trusted address to the server. This removes the UI timing dependence; the exact cause on the user's S23 was not independently observed.

Android records the actual last successful application handshake and per-device last-use times, showing recently connected paired laptops first. A temporary failed selection does not replace an existing successful last connection at the next launch. Windows likewise saves its phone only after the secure socket connects and the protocol hello is written. Missing/unpaired saved peers never cause silent substitution of another paired device. Legacy saved choices remain a fallback only when no successful connection has yet been recorded by this version.

## Why retain the existing camera

Android documents `ACTION_IMAGE_CAPTURE` with an output URI as a supported way to obtain a full-size photo from a camera Activity. The scanner already used this successfully on the user's S23, so the change reuses its narrow FileProvider URI grant and result handling. [Android common camera intents](https://developer.android.com/guide/components/intents-common#Camera).

An in-app preview using CameraX was considered because it would place scanner settings beside the viewfinder, but that is a different user flow and requires direct camera access. The user chose the external camera approach. The scanner's Settings UI therefore appears **after returning from the camera**, not as an overlay inside Samsung's camera. [CameraX preview](https://developer.android.com/media/camera/camerax/preview).

The automatic launch occurs only while the scanner Activity is resumed. The launch flag is consumed before opening the camera; its own camera handoff does not schedule another launch. Ordinary leaving/reopening of the scanner schedules a later launch when enabled and idle. Process death and OEM camera behavior still require hardware validation. [Android Activity lifecycle](https://developer.android.com/guide/components/activities/activity-lifecycle).

## Checks and hardware trial

The Android build and lint pass. Five pure-Java policy cases cover first-run trust, legacy migration, prioritizing the actual last connection over a failed choice, and refusing unpaired/absent saved peers. Existing Windows protocol/retry checks still pass. These checks do not exercise Android lifecycle callbacks or the Samsung camera.

On the S23: install the update in place; close older Windows copies and use the new receiver. Fully close/reopen the scanner and check that the normal camera appears without selecting a laptop or pressing an Open camera button. Capture a page and verify one paste. Reopen, press Back in the camera, and verify Settings remains visible rather than being replaced by another automatic launch. Test turning automatic launch off/on. Finally, open the scanner and take a photo before opening Windows: it should wait for and send to only the remembered laptop.
