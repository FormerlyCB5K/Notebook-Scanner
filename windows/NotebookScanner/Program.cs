using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;

namespace NotebookScanner;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if(args.Length==2 && args[0]=="--render-preview") {
            using var preview=new ReceiverForm(true); preview.ShowInTaskbar=false; preview.Opacity=0; preview.Show(); Application.DoEvents();
            using var bitmap=new Bitmap(preview.Width,preview.Height);
            preview.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size)); bitmap.Save(args[1]); return;
        }
        string name="Local\\NotebookScanner."+Environment.UserName;
        using var show=new EventWaitHandle(false,EventResetMode.AutoReset,name+".Show");
        using var singleton=new Mutex(true,name+".Instance",out bool first);
        if(!first) { show.Set(); return; }
        using var form=new ReceiverForm();
        var registration=ThreadPool.RegisterWaitForSingleObject(show,(_,_)=> {
            if(form.IsHandleCreated&&!form.IsDisposed) try { form.BeginInvoke(()=> { form.Show(); form.WindowState=FormWindowState.Normal; form.Activate(); }); } catch(InvalidOperationException) {}
        },null,Timeout.Infinite,false);
        try { Application.Run(form); } finally { registration.Unregister(null); singleton.ReleaseMutex(); }
    }
}

sealed class ReceiverForm : Form
{
    readonly ComboBox phones = new() { Width = 440, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly Button refresh = new() { Text = "Refresh paired phones", AutoSize = true };
    readonly Button connect = new() { Text = "Connect", AutoSize = true };
    readonly Button disconnect = new() { Text = "Disconnect", AutoSize = true, Enabled = false };
    readonly Button local = new() { Text = "Test clipboard with local JPEG…", AutoSize = true };
    readonly Label status = new() { AutoSize = true, MaximumSize = new Size(570, 0), Text = "Pair your phone in Windows Bluetooth settings first." };
    CancellationTokenSource? session;
    StreamSocket? socket;
    BluetoothDevice? device;
    RfcommDeviceService? service;
    readonly string memoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NotebookScanner", "phone.json");
    string? rememberedPhone;
    bool refreshing;
    bool closing;
    readonly DesktopOptions options=DesktopOptions.Load();
    readonly NotifyIcon notification=new() { Icon=SystemIcons.Information, Text="Notebook Scanner", Visible=false };

    public ReceiverForm(bool preview=false)
    {
        Text = "Notebook Scanner — 0.5 beta";
        ClientSize = new Size(680, 570);
        rememberedPhone = DeviceMemory.Read(memoryPath);
        var layout = new DawnPanel { Dock = DockStyle.Fill, Padding = new Padding(30), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll=true };
        var title=new Label { Text="BLUETOOTH PHOTO TRANSFER", AutoSize=true, Margin=new Padding(0,8,0,20) };
        var heading=new Label { Text="Notebook Scanner", AutoSize=true, Margin=new Padding(0,0,0,24) };
        status.Padding=new Padding(18); status.BackColor=Color.FromArgb(27,32,48); status.MinimumSize=new Size(590,100);
        var settings=new FlowLayoutPanel { AutoSize=true, FlowDirection=FlowDirection.TopDown, WrapContents=false, Visible=rememberedPhone==null };
        ClientSize=new Size(680,settings.Visible?780:450);
        HandleCreated+=(_,_)=>DawnPanel.Chrome(this);
        settings.Controls.AddRange([phones, refresh, connect, disconnect, local]);
        var settingsButton=new Button { Text="Settings",AutoSize=true };
        settingsButton.Click+=(_,_)=> { settings.Visible=!settings.Visible; ClientSize=new Size(680,settings.Visible?780:450); };
        var notices=new CheckBox { Text="Notify when a photo is copied",Checked=options.Notifications,AutoSize=true };
        notices.CheckedChanged+=(_,_)=> { options.Notifications=notices.Checked; try { options.Save(); } catch(Exception ex) { status.Text=ex.Message; } };
        var shortcut=new Button { Text=options.Shortcut=="" ? "Set keyboard shortcut…" : "Shortcut: "+options.Shortcut, AutoSize=true };
        shortcut.Click+=(_,_)=> {
            using var dialog=new Form { Text="Keyboard shortcut",ClientSize=new Size(440,180),StartPosition=FormStartPosition.CenterParent };
            var box=new TextBox { Left=20,Top=45,Width=395,ReadOnly=true,Text=options.Shortcut==""?"CTRL+ALT+N":options.Shortcut };
            var help=new Label { Left=20,Top=15,Width=400,Text="Press Ctrl + Alt + a letter, then Save." };
            box.KeyDown+=(_,e)=> { e.SuppressKeyPress=true; if(e.Control&&e.Alt&&e.KeyCode>=Keys.A&&e.KeyCode<=Keys.Z) box.Text="CTRL+ALT+"+e.KeyCode; };
            var save=new Button { Text="Save",Left=20,Top=95,Width=100,Height=38 };
            var clear=new Button { Text="Remove",Left=135,Top=95,Width=100,Height=38 };
            void Apply(string chord) { try { DesktopOptions.InstallShortcut(chord); options.Shortcut=chord; options.Save(); shortcut.Text=chord==""?"Set keyboard shortcut…":"Shortcut: "+chord; dialog.Close(); } catch(Exception ex) { MessageBox.Show(dialog,ex.Message,"Shortcut could not be saved"); } }
            save.Click+=(_,_)=> { if(box.Text!="") Apply(box.Text); }; clear.Click+=(_,_)=>Apply("");
            dialog.Controls.AddRange([help,box,save,clear]); dialog.BackColor=Color.FromArgb(21,24,36); DawnPanel.Style(dialog); dialog.ShowDialog(this);
        };
        settings.Controls.AddRange([shortcut,notices]);
        var reminder=new Label { Text="Make sure Bluetooth is turned on on both devices. You shouldn't need WiFi for the app to work.",AutoSize=true,MaximumSize=new Size(590,0),Margin=new Padding(0,10,0,10) };
        layout.Controls.AddRange([title,heading,status,reminder,settingsButton,settings]);
        Controls.Add(layout);
        DawnPanel.Style(layout); heading.Font=new Font("Arial",24,FontStyle.Bold); title.ForeColor=Color.FromArgb(188,183,231); status.BackColor=Color.FromArgb(27,32,48);
        reminder.Font=new Font("Arial",9); reminder.ForeColor=Color.FromArgb(182,184,204);
        var footer=new System.Windows.Forms.Panel { Dock=DockStyle.Bottom,Height=60,BackColor=Color.FromArgb(21,24,36),Padding=new Padding(30,6,30,12) };
        var portfolio=new LinkLabel {
            Text="Unrelated, I'm a math undergrad looking for internships. You can see my other work here.",
            Dock=DockStyle.Fill,TextAlign=ContentAlignment.BottomRight,Font=new Font("Arial",8),
            LinkColor=Color.FromArgb(182,184,204),ActiveLinkColor=Color.FromArgb(237,238,248),
            VisitedLinkColor=Color.FromArgb(182,184,204),LinkBehavior=LinkBehavior.HoverUnderline,
            AccessibleName="Unrelated, I'm a math undergrad looking for internships. You can see my other work here."
        };
        portfolio.LinkClicked+=(_,_)=> {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://sebastian-lashmet-portfolio.pages.dev/") { UseShellExecute=true }); }
            catch(Exception ex) { MessageBox.Show(this,ex.Message,"Could not open link"); }
        };
        footer.Controls.Add(portfolio); Controls.Add(footer);
        notification.DoubleClick+=(_,_)=> { Show(); WindowState=FormWindowState.Normal; Activate(); };
        notification.Visible=!preview;
        FormClosed+=(_,_)=>notification.Dispose();
        if(!preview && options.Shortcut!="") try { DesktopOptions.InstallShortcut(options.Shortcut); } catch(Exception ex) { status.Text="Shortcut needs saving again: "+ex.Message; }
        refresh.Click += async (_, _) => await RefreshPhones(false);
        connect.Click += async (_, _) => await RunConnection();
        disconnect.Text = "Pause connection";
        disconnect.Click += (_, _) => Stop();
        local.Click += async (_, _) => {
            using var dialog = new OpenFileDialog { Filter = "Baseline JPEG|*.jpg;*.jpeg" };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            try {
                if (new FileInfo(dialog.FileName).Length > Wire.MaxBytes) throw new IOException("JPEG exceeds 8 MiB.");
                byte[] bytes = await File.ReadAllBytesAsync(dialog.FileName);
                Wire.ValidateJpeg(bytes);
                await CopyImage(bytes);
                status.Text = "Local image copied. Try Ctrl+V in an unsent draft.";
            } catch (Exception ex) { status.Text = ex.Message; }
        };
        FormClosing += (_, _) => { closing = true; Stop(); };
        if(!preview) Shown += async (_, _) => await RefreshPhones(true);
    }

    async Task RefreshPhones(bool autoConnect)
    {
        if (refreshing || session != null) return;
        refreshing = true;
        refresh.Enabled = false;
        connect.Enabled = false;
        bool foundRemembered = false;
        try {
            var found = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true));
            if (closing) return;
            phones.Items.Clear();
            foreach (var item in found) phones.Items.Add(new Phone(item.Name, item.Id));
            for (int i = 0; i < phones.Items.Count; i++) {
                if (((Phone)phones.Items[i]!).Id == rememberedPhone) {
                    phones.SelectedIndex = i; foundRemembered = true; break;
                }
            }
            if (!foundRemembered && phones.Items.Count > 0) phones.SelectedIndex = 0;
            status.Text = foundRemembered ? "Remembered your phone. Open Notebook Scanner there." :
                "Choose your paired phone and Connect once. Next time both apps reconnect automatically when opened.";
        } catch (Exception ex) { status.Text = "Bluetooth enumeration failed: " + ex.Message; }
        finally {
            refreshing = false;
            if (!closing) { refresh.Enabled = true; connect.Enabled = phones.Items.Count > 0; }
        }
        if (autoConnect && foundRemembered && !closing) await RunConnection();
    }

    async Task RunConnection()
    {
        if (phones.SelectedItem is not Phone phone || session != null || closing) return;
        session = new CancellationTokenSource();
        var token = session.Token;
        connect.Enabled = refresh.Enabled = phones.Enabled = local.Enabled = false;
        disconnect.Enabled = true;
        try {
            await ConnectionRetry.Run(async stop => {
                try { await ReceiveFrom(phone, stop); }
                finally { ReleaseConnection(); }
            }, (ex, delay) => {
                status.Text = $"Waiting for {phone.Name}. Open Notebook Scanner on your phone. Retrying in {delay.TotalSeconds:0}s. {ex.Message}";
            }, token);
        } catch (OperationCanceledException) { if (!closing) status.Text = "Connection paused. Click Connect to resume."; }
        catch (UnauthorizedAccessException ex) { if (!closing) status.Text = "Bluetooth access or pairing needs attention. " + ex.Message + " Check Windows Bluetooth settings, then click Connect to retry."; }
        catch (Exception ex) { if (!closing) status.Text = "Connection ended: " + ex.Message; }
        finally {
            Stop(); ReleaseConnection();
            session?.Dispose(); session = null;
            if (!closing && !IsDisposed) {
                connect.Enabled = refresh.Enabled = phones.Enabled = local.Enabled = true;
                disconnect.Enabled = false;
            }
        }
    }

    async Task ReceiveFrom(Phone phone, CancellationToken token)
    {
            if (closing) return;
            status.Text = $"Connecting to {phone.Name}… Open Notebook Scanner on your phone.";
            using (var setup = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                setup.CancelAfter(TimeSpan.FromSeconds(40));
                device = await BluetoothDevice.FromIdAsync(phone.Id).AsTask(setup.Token)
                    ?? throw new IOException("Phone unavailable.");
                if (!device.DeviceInformation.Pairing.IsPaired)
                    throw new UnauthorizedAccessException("The selected phone is no longer paired.");
                var result = await device.GetRfcommServicesForIdAsync(RfcommServiceId.FromUuid(Wire.ServiceId), BluetoothCacheMode.Uncached).AsTask(setup.Token);
                if (result.Error is BluetoothError.ConsentRequired or BluetoothError.DisabledByUser or BluetoothError.DisabledByPolicy)
                    throw new UnauthorizedAccessException("Windows reports " + result.Error + ".");
                if (result.Error != BluetoothError.Success)
                    throw new IOException("Windows could not access the phone's scanner service (" + result.Error + "). Check Bluetooth permissions and retry.");
                if (result.Services.Count == 0)
                    throw new IOException("The phone app is not ready yet. On its first use, select this laptop and tap Connect to this laptop.");
                service = result.Services[0];
                socket = new StreamSocket();
                await socket.ConnectAsync(service.ConnectionHostName, service.ConnectionServiceName,
                    SocketProtectionLevel.BluetoothEncryptionWithAuthentication).AsTask(setup.Token);
            }
            using var input = socket.InputStream.AsStreamForRead();
            using var output = socket.OutputStream.AsStreamForWrite();
            await output.WriteAsync(Wire.Magic, token);
            await output.FlushAsync(token);
            // A failed attempt to another phone must not replace the last connection.
            rememberedPhone = phone.Id;
            string saveWarning = "";
            try { DeviceMemory.Write(memoryPath, phone.Id); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                saveWarning = " This phone could not be remembered for the next launch.";
            }
            status.Text = "Connected. Take a photo on your phone." + saveWarning;
            while (!token.IsCancellationRequested) {
                byte[] jpeg = await Wire.Receive(input, token);
                token.ThrowIfCancellationRequested();
                // Await resumes on the WinForms STA thread: clipboard calls must stay here.
                await CopyImage(jpeg, token);
                if(options.Notifications) try { notification.ShowBalloonTip(4000,"Notebook Scanner","Photo copied to clipboard.",ToolTipIcon.Info); } catch(InvalidOperationException) {}
                await output.WriteAsync(new byte[] { 6 }, token);
                await output.FlushAsync(token);
                status.Text = $"Photo copied ({jpeg.Length / 1024} KiB). Press Ctrl+V to paste.";
            }
    }

    void ReleaseConnection()
    {
        socket?.Dispose(); socket = null;
        service?.Dispose(); service = null;
        device?.Dispose(); device = null;
    }

    static async Task CopyImage(byte[] jpeg, CancellationToken stop = default)
    {
        using var stream = new MemoryStream(jpeg);
        using var decoded = Image.FromStream(stream, false, true);
        using var bitmap = new Bitmap(decoded);
        for (int attempt = 0; ; attempt++) {
            stop.ThrowIfCancellationRequested();
            try { Clipboard.SetDataObject(new DataObject(DataFormats.Bitmap, bitmap), true); return; }
            catch (System.Runtime.InteropServices.ExternalException) when (attempt < 9) { await Task.Delay(100, stop); }
        }
    }

    void Stop() { session?.Cancel(); socket?.Dispose(); socket = null; }
    sealed record Phone(string Name, string Id) { public override string ToString() => Name; }
}
