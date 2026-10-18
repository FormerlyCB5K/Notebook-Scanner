package org.notebookscanner;

import android.Manifest;
import android.app.Activity;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.bluetooth.*;
import android.content.*;
import android.content.pm.PackageManager;
import android.graphics.*;
import android.net.Uri;
import android.os.*;
import android.provider.MediaStore;
import android.view.WindowManager;
import android.widget.*;
import androidx.core.content.FileProvider;
import java.io.*;
import java.security.MessageDigest;
import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicBoolean;

/** Foreground experiment: one paired laptop, one in-flight photo, no Internet permission. */
public class MainActivity extends Activity {
    static final UUID SERVICE = UUID.fromString("e286e3de-3194-4e95-ae08-7ad9f621aba0");
    static final byte[] MAGIC = {78, 83, 48, 49};
    final ArrayList<BluetoothDevice> devices = new ArrayList<>();
    static final class PhotoJob {
        final BluetoothSocket peer;
        final byte[] jpeg;
        PhotoJob(BluetoothSocket peer, byte[] jpeg) { this.peer = peer; this.jpeg = jpeg; }
    }
    final BlockingQueue<PhotoJob> photos = new ArrayBlockingQueue<>(1);
    final ExecutorService imaging = Executors.newSingleThreadExecutor();
    final ScheduledExecutorService timers = Executors.newSingleThreadScheduledExecutor();
    BluetoothAdapter adapter;
    Spinner laptop;
    Button start, stop, capture, refresh, cameraTab, settingsTab;
    LinearLayout cameraPage;
    ScrollView settingsPage;
    TextView status;
    Switch notificationSwitch;
    volatile BluetoothServerSocket server;
    volatile BluetoothSocket connection;
    volatile boolean running;
    volatile boolean connected;
    boolean busy, cameraOpen, preparing, permissionPending;
    volatile boolean autoWanted;
    String rememberedLaptop;
    String selectedForSession;
    volatile String listeningLaptop;
    BluetoothSocket capturePeer;
    String captureTarget;
    byte[] waitingPhoto;
    String waitingPhotoTarget;
    boolean resumed, autoCameraPending, autoCameraEligible;
    final Handler main = new Handler(Looper.getMainLooper());
    final Runnable reconnect = new Runnable() {
        @Override public void run() {
            if (autoWanted && !running && transport == null && !cameraOpen && !preparing) loadDevices(false);
            main.postDelayed(this, 2500);
        }
    };
    Thread transport;
    File raw;
    Uri captureUri;

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        adapter = getSystemService(BluetoothManager.class).getAdapter();
        rememberedLaptop = getPreferences(MODE_PRIVATE).getString("last-connected-laptop",
            getPreferences(MODE_PRIVATE).getString("laptop", null));
        autoWanted = rememberedLaptop != null;
        autoCameraEligible = rememberedLaptop != null;
        autoCameraPending = state == null && autoCameraEligible;
        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setPadding(24, 48, 24, 24);
        TextView appTitle=new TextView(this);
        appTitle.setText("Notebook Scanner"); root.addView(appTitle);
        LinearLayout tabs = new LinearLayout(this);
        cameraTab = button(tabs, "Camera", () -> {
            showCameraPage();
            if (running && listeningLaptop != null && !busy) takePhoto();
            else if (listeningLaptop == null) { showSettingsPage(); status.setText("Choose your laptop and connect once to finish setup."); }
        });
        settingsTab = button(tabs, "Settings", () -> showSettingsPage());
        root.addView(tabs);
        FrameLayout pages = new FrameLayout(this);
        root.addView(pages, new LinearLayout.LayoutParams(-1, 0, 1));
        cameraPage = new LinearLayout(this);
        cameraPage.setOrientation(LinearLayout.VERTICAL);
        TextView cameraIntro = new TextView(this);
        cameraIntro.setText("Your phone's usual camera opens automatically. Press Back there to return to Settings. A photo taken before your laptop connects waits for that laptop.");
        cameraPage.addView(cameraIntro);
        capture = button(cameraPage, "Open camera", () -> takePhoto());
        pages.addView(cameraPage);
        settingsPage = new ScrollView(this);
        LinearLayout layout = new LinearLayout(this);
        layout.setOrientation(LinearLayout.VERTICAL);
        settingsPage.addView(layout);
        pages.addView(settingsPage);
        TextView intro = new TextView(this);
        intro.setText("Choose your paired laptop once. Next time, just open both apps and take a photo. Keep this app open while using it.");
        layout.addView(intro);
        laptop = new Spinner(this); layout.addView(laptop);
        refresh = button(layout, "Refresh paired laptops", () -> loadDevices(true));
        start = button(layout, "Connect to this laptop", () -> startListening());
        stop = button(layout, "Pause / change laptop", () -> stopTransport());
        Switch automaticCamera = new Switch(this);
        automaticCamera.setText("Open camera when I open the app");
        automaticCamera.setChecked(getPreferences(MODE_PRIVATE).getBoolean("open-camera-on-launch", true));
        automaticCamera.setOnCheckedChangeListener((button, checked) -> {
            getPreferences(MODE_PRIVATE).edit().putBoolean("open-camera-on-launch", checked).apply();
            if (!checked) autoCameraPending = false;
        });
        layout.addView(automaticCamera);
        Switch notifyCopied = new Switch(this);
        notificationSwitch=notifyCopied;
        notifyCopied.setText("Notify when copied to laptop");
        notifyCopied.setChecked(getPreferences(MODE_PRIVATE).getBoolean("notify-copied",true) &&
            (Build.VERSION.SDK_INT<33 || checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS)==PackageManager.PERMISSION_GRANTED));
        notifyCopied.setOnCheckedChangeListener((button,checked)-> {
            getPreferences(MODE_PRIVATE).edit().putBoolean("notify-copied",checked).apply();
            if(checked) requestNotificationPermission();
        });
        layout.addView(notifyCopied);
        button(layout,"Allow completion notifications",()->requestNotificationPermission());
        status = new TextView(this); root.addView(status);
        root.setBackground(new NightTheme());
        NightTheme.style(root);
        appTitle.setTextSize(24); appTitle.setTypeface(Typeface.DEFAULT,Typeface.BOLD);
        status.setBackgroundColor(NightTheme.PANEL);
        getWindow().setStatusBarColor(Color.rgb(21,24,36)); getWindow().setNavigationBarColor(Color.rgb(21,24,36));
        getWindow().getDecorView().setSystemUiVisibility(0);
        NotificationManager notifications=getSystemService(NotificationManager.class);
        notifications.createNotificationChannel(new NotificationChannel("copied","Photo completion",NotificationManager.IMPORTANCE_HIGH));
        setContentView(root);
        if (autoCameraEligible && automaticCamera.isChecked()) showCameraPage(); else showSettingsPage();
        updateControls();
        loadDevices(true);
        // A killed Activity cannot retain its socket. Do not silently resend an old page.
        File directory = new File(getCacheDir(), "capture");
        File[] old = directory.listFiles();
        if (old != null) for (File file : old) file.delete();
    }

    Button button(LinearLayout layout, String label, Runnable action) {
        Button button = new Button(this); button.setText(label);
        button.setOnClickListener(v -> action.run()); layout.addView(button); return button;
    }

    boolean permitted() {
        return Build.VERSION.SDK_INT < 31 || checkSelfPermission(Manifest.permission.BLUETOOTH_CONNECT) == PackageManager.PERMISSION_GRANTED;
    }

    void loadDevices(boolean allowPrompt) {
        if (running || transport != null) return;
        if (!permitted()) {
            if (allowPrompt && !permissionPending) {
                permissionPending = true;
                requestPermissions(new String[] { Manifest.permission.BLUETOOTH_CONNECT }, 7);
            }
            status.setText("Allow Nearby devices permission to connect to your laptop.");
            return;
        }
        try {
            if (adapter == null || !adapter.isEnabled()) { status.setText("Turn on Bluetooth in Android settings. Your saved connection will resume automatically."); return; }
            devices.clear(); devices.addAll(adapter.getBondedDevices());
            devices.sort(Comparator.comparingLong((BluetoothDevice d) ->
                getPreferences(MODE_PRIVATE).getLong("used:" + d.getAddress(), 0)).reversed()
                .thenComparing(BluetoothDevice::getAddress));
            ArrayList<String> names = new ArrayList<>();
            names.add("Choose your laptop…");
            int savedIndex = -1;
            ArrayList<String> paired = new ArrayList<>();
            for (BluetoothDevice d : devices) paired.add(d.getAddress());
            String target = DeviceHistory.startupTarget(selectedForSession != null ? selectedForSession :
                getPreferences(MODE_PRIVATE).getString("last-connected-laptop", null),
                getPreferences(MODE_PRIVATE).getString("laptop", null), paired);
            for (BluetoothDevice d : devices) names.add(d.getName() + " — " + d.getAddress());
            for (int i = 0; i < devices.size(); i++) if (devices.get(i).getAddress().equals(target)) savedIndex = i + 1;
            laptop.setAdapter(NightTheme.adapter(this,names));
            if (savedIndex > 0) laptop.setSelection(savedIndex);
            status.setText(devices.isEmpty() ? "No paired devices. Pair your laptop in Bluetooth settings first." : "Choose your laptop and tap Connect to this laptop once.");
            // Do not read the Spinner here: setSelection may not have laid out yet.
            if (autoWanted && target != null) startListening(target);
            else if (autoWanted && rememberedLaptop != null) {
                autoWanted = false;
                status.setText("Your saved laptop is no longer paired. Choose a paired laptop and connect again.");
            }
            maybeOpenCamera();
        } catch (SecurityException ex) { status.setText("Nearby devices permission is required. Allow it in app settings."); }
    }

    @Override public void onRequestPermissionsResult(int request, String[] permissions, int[] grants) {
        super.onRequestPermissionsResult(request, permissions, grants);
        if(request==9) {
            permissionPending=false; autoCameraPending=false;
            boolean allowed=grants.length>0&&grants[0]==PackageManager.PERMISSION_GRANTED;
            getPreferences(MODE_PRIVATE).edit().putBoolean("notify-copied",allowed).apply();
            notificationSwitch.setChecked(allowed);
            status.setText(allowed ? "Completion notifications enabled." : "Notifications are off. Photo transfer still works."); return;
        }
        permissionPending = false;
        if (request == 7 && grants.length > 0 && grants[0] == PackageManager.PERMISSION_GRANTED) loadDevices(false);
        else status.setText("Nearby devices permission denied. Enable it in Android app settings to connect.");
    }

    void startListening() {
        int selected = laptop.getSelectedItemPosition() - 1;
        if (transport != null || running || !permitted()) return;
        if (selected < 0 || selected >= devices.size()) { status.setText("Choose your laptop from the list first."); return; }
        startListening(devices.get(selected).getAddress());
    }

    void startListening(String allowed) {
        if (transport != null || running || !permitted() || allowed == null) return;
        selectedForSession = allowed;
        listeningLaptop = allowed;
        getPreferences(MODE_PRIVATE).edit().putString("laptop", allowed).apply();
        autoWanted = true;
        running = true; photos.clear(); updateControls();
        transport = new Thread(() -> {
            try {
                server = adapter.listenUsingRfcommWithServiceRecord("Notebook Scanner", SERVICE);
                if (!running) return;
                say("Waiting for your laptop. Open Notebook Scanner on Windows; it will reconnect automatically after first setup.");
                while (running) {
                    BluetoothSocket peer = server.accept();
                    try {
                        if (!peer.getRemoteDevice().getAddress().equals(allowed) || peer.getRemoteDevice().getBondState() != BluetoothDevice.BOND_BONDED) continue;
                        connection = peer;
                        servePeer(peer);
                    } catch (SecurityException ex) { throw ex; }
                    catch (InterruptedException ex) { throw ex; }
                    catch (Exception ex) {
                        if (running) say("Laptop disconnected. Reconnecting automatically; please retake any photo that was not confirmed copied.");
                    } finally {
                        close(peer); connected = false; connection = null; photos.clear();
                        ui(() -> { if (!cameraOpen && !preparing && waitingPhoto == null) busy = false; updateControls(); });
                    }
                }
            } catch (SecurityException ex) {
                autoWanted = false;
                ui(() -> { waitingPhoto = null; waitingPhotoTarget = null; });
                say("Nearby devices permission was revoked. Allow it in app settings, then reconnect.");
            } catch (Exception ex) {
                if (running) say("Connection interrupted. Waiting to reconnect: " + ex.getMessage());
            } finally {
                close(connection); close(server); connection = null; server = null;
                photos.clear(); running = false; connected = false;
                ui(() -> { transport = null; if (!cameraOpen && !preparing && waitingPhoto == null) busy = false; updateControls(); });
            }
        }, "scanner-rfcomm");
        updateControls();
        transport.start();
    }

    void servePeer(BluetoothSocket peer) throws Exception {
        DataInputStream in = new DataInputStream(peer.getInputStream());
        DataOutputStream out = new DataOutputStream(peer.getOutputStream());
        ScheduledFuture<?> helloTimer = timers.schedule(() -> close(peer), 20, TimeUnit.SECONDS);
        try {
            byte[] hello = new byte[4]; in.readFully(hello);
            if (!Arrays.equals(hello, MAGIC)) throw new IOException("Wrong protocol version");
        } finally { helloTimer.cancel(false); }
        AtomicBoolean peerOpen = new AtomicBoolean(true);
        AtomicBoolean expectingAck = new AtomicBoolean(false);
        BlockingQueue<Integer> acknowledgements = new ArrayBlockingQueue<>(1);
        Thread reader = new Thread(() -> {
            try {
                while (running && peerOpen.get()) {
                    int value = in.read();
                    if (value != 6 || !expectingAck.getAndSet(false) || !acknowledgements.offer(value)) break;
                }
            } catch (IOException ignored) {
                // Closing the socket below wakes both an active send and an idle session.
            } finally {
                peerOpen.set(false); close(peer); acknowledgements.offer(-1);
            }
        }, "scanner-ack");
        reader.setDaemon(true);
        reader.start();
        connected = true;
        ui(() -> { if (running && connection == peer) {
            rememberedLaptop = peer.getRemoteDevice().getAddress();
            getPreferences(MODE_PRIVATE).edit().putString("last-connected-laptop", rememberedLaptop)
                .putString("laptop", rememberedLaptop).putLong("used:" + rememberedLaptop, System.currentTimeMillis()).apply();
            status.setText("Connected. Open the Camera tab to take a photo.");
            sendWaitingPhoto(); updateControls(); maybeOpenCamera();
        } });
        try {
            while (running && peerOpen.get()) {
                PhotoJob job = photos.poll(250, TimeUnit.MILLISECONDS);
                if (job == null) continue;
                if (job.peer != peer) {
                    ui(() -> { busy = false; updateControls(); status.setText("Connection changed. Please retake your photo."); });
                    continue;
                }
                byte[] jpeg = job.jpeg;
                long began = System.nanoTime();
                ScheduledFuture<?> deadline = timers.schedule(() -> close(peer), 120, TimeUnit.SECONDS);
                try {
                    expectingAck.set(true);
                    out.writeInt(jpeg.length);
                    out.write(MessageDigest.getInstance("SHA-256").digest(jpeg));
                    out.write(jpeg); out.flush();
                    Integer ack = acknowledgements.poll(120, TimeUnit.SECONDS);
                    if (ack == null || ack != 6) throw new IOException("No clipboard acknowledgement");
                } finally { expectingAck.set(false); deadline.cancel(false); }
                double seconds = (System.nanoTime() - began) / 1e9;
                ui(() -> { busy = false; updateControls(); status.setText(String.format(Locale.US,
                    "Copied on laptop: %d KiB in %.1f s. Press Ctrl+V in your draft.", jpeg.length / 1024, seconds)); notifyCopied(); maybeOpenCamera(); });
            }
            if (running) throw new IOException("Laptop disconnected");
        } finally {
            peerOpen.set(false); close(peer);
            reader.join(1000);
        }
    }

    void takePhoto() {
        if (!running || listeningLaptop == null || busy || cameraOpen || preparing) return;
        try {
            File directory = new File(getCacheDir(), "capture");
            if (!directory.exists() && !directory.mkdirs()) throw new IOException("Cannot create capture directory");
            raw = new File(directory, "page.jpg");
            captureUri = FileProvider.getUriForFile(this, getPackageName() + ".photos", raw);
            Intent intent = new Intent(MediaStore.ACTION_IMAGE_CAPTURE);
            intent.putExtra(MediaStore.EXTRA_OUTPUT, captureUri);
            intent.setClipData(ClipData.newRawUri("Notebook page", captureUri));
            intent.addFlags(Intent.FLAG_GRANT_WRITE_URI_PERMISSION | Intent.FLAG_GRANT_READ_URI_PERMISSION);
            capturePeer = connected ? connection : null;
            captureTarget = listeningLaptop;
            autoCameraPending = false;
            autoCameraEligible = true;
            cameraOpen = true; busy = true; updateControls();
            startActivityForResult(intent, 8);
        } catch (Exception ex) { cameraOpen = false; busy = false; updateControls(); status.setText("Cannot open camera: " + ex.getMessage()); }
    }

    @Override protected void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request, result, data);
        if (request != 8) return;
        cameraOpen = false;
        if (captureUri != null) revokeUriPermission(captureUri, Intent.FLAG_GRANT_WRITE_URI_PERMISSION | Intent.FLAG_GRANT_READ_URI_PERMISSION);
        final File image = raw;
        if (result != RESULT_OK || image == null || !image.exists()) {
            if (image != null) image.delete();
            busy = false; showSettingsPage(); updateControls(); status.setText("Camera closed. Change settings here, or tap Camera to take a photo."); return;
        }
        BluetoothSocket intended = capturePeer;
        String intendedTarget = captureTarget;
        showCameraPage();
        preparing = true;
        status.setText("Preparing photo…");
        imaging.execute(() -> {
            try {
                Bitmap bitmap = ImageDecoder.decodeBitmap(ImageDecoder.createSource(image), (decoder, info, source) -> {
                    int w = info.getSize().getWidth(), h = info.getSize().getHeight();
                    double scale = Math.min(1.0, 2400.0 / Math.max(w, h));
                    decoder.setTargetSize(Math.max(1, (int)(w * scale)), Math.max(1, (int)(h * scale)));
                    decoder.setAllocator(ImageDecoder.ALLOCATOR_SOFTWARE);
                });
                ByteArrayOutputStream bytes = new ByteArrayOutputStream();
                try { if (!bitmap.compress(Bitmap.CompressFormat.JPEG, 85, bytes)) throw new IOException("JPEG encoding failed"); }
                finally { bitmap.recycle(); }
                byte[] jpeg = bytes.toByteArray();
                if (jpeg.length == 0 || jpeg.length > 8 * 1024 * 1024) throw new IOException("Photo exceeds transfer limit");
                ui(() -> {
                    if (!running || !autoWanted || !Objects.equals(intendedTarget, listeningLaptop)) {
                        busy = false; status.setText("Connection was paused or changed. Please retake your photo.");
                    } else if (intended != null) {
                        // Once a transfer had a connection, never replay it on another one.
                        if (!connected || connection != intended || !photos.offer(new PhotoJob(intended, jpeg))) {
                            busy = false; status.setText("Laptop disconnected. Please retake after reconnecting.");
                        } else status.setText("Sending photo; waiting for laptop clipboard acknowledgement…");
                    } else {
                        // The camera can open before Bluetooth connects. Hold one unsent
                        // image in memory for this explicitly selected laptop only.
                        waitingPhoto = jpeg; waitingPhotoTarget = intendedTarget;
                        sendWaitingPhoto();
                    }
                    updateControls();
                });
            } catch (Exception ex) {
                ui(() -> { busy = false; updateControls(); status.setText("Photo failed: " + ex.getMessage()); });
            } finally { image.delete(); ui(() -> { preparing = false; updateControls(); maybeOpenCamera(); }); }
        });
    }

    void sendWaitingPhoto() {
        if (waitingPhoto == null) return;
        if (!running || !autoWanted || !Objects.equals(waitingPhotoTarget, listeningLaptop)) {
            waitingPhoto = null; waitingPhotoTarget = null; busy = false;
            status.setText("Laptop changed. Please retake your photo.");
            return;
        }
        if (!connected || connection == null) {
            status.setText("Photo ready. Waiting for your laptop; it will send when connected.");
            return;
        }
        if (photos.offer(new PhotoJob(connection, waitingPhoto))) {
            waitingPhoto = null; waitingPhotoTarget = null;
            status.setText("Sending photo; waiting for laptop clipboard acknowledgement…");
        }
    }

    void requestNotificationPermission() {
        if(permissionPending || cameraOpen) { status.setText("Return to Settings after camera/setup to enable notifications."); return; }
        getPreferences(MODE_PRIVATE).edit().putBoolean("notify-copied",true).apply();
        if(Build.VERSION.SDK_INT>=33 && checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS)!=PackageManager.PERMISSION_GRANTED) {
            permissionPending=true; autoCameraPending=false;
            requestPermissions(new String[]{Manifest.permission.POST_NOTIFICATIONS},9);
        }
        else { notificationSwitch.setChecked(true); status.setText("Completion notifications enabled. Check Android notification settings if they are hidden."); }
    }

    void notifyCopied() {
        if(!getPreferences(MODE_PRIVATE).getBoolean("notify-copied",true)) return;
        if(Build.VERSION.SDK_INT>=33 && checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS)!=PackageManager.PERMISSION_GRANTED) return;
        try {
            Intent open=new Intent(this,MainActivity.class).addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP|Intent.FLAG_ACTIVITY_SINGLE_TOP);
            PendingIntent action=PendingIntent.getActivity(this,0,open,PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);
            Notification note=new Notification.Builder(this,"copied").setSmallIcon(org.notebookscanner.R.drawable.ic_notification)
                .setContentTitle("Notebook Scanner").setContentText("Photo copied to your laptop.")
                .setContentIntent(action).setAutoCancel(true).setCategory(Notification.CATEGORY_STATUS).build();
            getSystemService(NotificationManager.class).notify(1,note);
        } catch(SecurityException ignored) { /* Notifications never block a successful transfer. */ }
    }

    void showCameraPage() {
        cameraPage.setVisibility(android.view.View.VISIBLE);
        settingsPage.setVisibility(android.view.View.GONE);
        cameraTab.setSelected(true); settingsTab.setSelected(false);
    }

    void showSettingsPage() {
        cameraPage.setVisibility(android.view.View.GONE);
        settingsPage.setVisibility(android.view.View.VISIBLE);
        cameraTab.setSelected(false); settingsTab.setSelected(true);
    }

    void maybeOpenCamera() {
        if (!resumed || isFinishing() || isDestroyed() || !autoCameraPending ||
            !getPreferences(MODE_PRIVATE).getBoolean("open-camera-on-launch", true) ||
            permissionPending || !running || listeningLaptop == null || busy || cameraOpen || preparing) return;
        autoCameraPending = false;
        showCameraPage();
        takePhoto();
    }

    void stopTransport() {
        autoWanted = false;
        waitingPhoto = null; waitingPhotoTarget = null;
        if (!cameraOpen && !preparing) busy = false;
        running = false; connected = false; close(connection); close(server);
        if (transport != null) transport.interrupt();
        status.setText("Connection paused. Choose your laptop and tap Connect to this laptop to resume.");
        updateControls();
    }
    void updateControls() {
        boolean stopped = !running && transport == null;
        start.setEnabled(stopped); refresh.setEnabled(stopped); laptop.setEnabled(stopped);
        start.setVisibility(running ? android.view.View.GONE : android.view.View.VISIBLE);
        refresh.setVisibility(running ? android.view.View.GONE : android.view.View.VISIBLE);
        stop.setEnabled(transport != null); capture.setEnabled(running && listeningLaptop != null && !busy && !cameraOpen && !preparing);
    }
    @Override protected void onResume() {
        super.onResume(); resumed = true;
        main.removeCallbacks(reconnect); main.post(reconnect); main.post(() -> maybeOpenCamera());
    }
    @Override protected void onPause() {
        if (!cameraOpen && autoCameraEligible && !permissionPending) autoCameraPending = true;
        resumed = false; main.removeCallbacks(reconnect); super.onPause();
    }
    void say(String message) { ui(() -> status.setText(message)); }
    void ui(Runnable action) { runOnUiThread(() -> { if (!isDestroyed()) action.run(); }); }
    static void close(Closeable object) { if (object != null) try { object.close(); } catch (IOException ignored) {} }
    @Override public void onDestroy() {
        main.removeCallbacks(reconnect);
        stopTransport(); imaging.shutdownNow(); timers.shutdownNow(); super.onDestroy();
    }
}
