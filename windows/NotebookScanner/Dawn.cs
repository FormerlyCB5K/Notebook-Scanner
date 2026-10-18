using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace NotebookScanner;

sealed class DawnPanel : FlowLayoutPanel
{
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    public static void Chrome(Form form) {
        int dark=1,caption=ColorTranslator.ToWin32(Color.FromArgb(21,24,36)),text=ColorTranslator.ToWin32(Color.FromArgb(237,238,248));
        DwmSetWindowAttribute(form.Handle,20,ref dark,4); DwmSetWindowAttribute(form.Handle,35,ref caption,4); DwmSetWindowAttribute(form.Handle,36,ref text,4);
    }
    public DawnPanel() { DoubleBuffered = true; ResizeRedraw = true; }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.FromArgb(21,24,36));
        Glow(e.Graphics, .80f,.35f, Color.FromArgb(66,113,82,120));
        Glow(e.Graphics, .15f,.30f, Color.FromArgb(80,67,102,140));
        for (int i=0;i<28;i++) {
            bool right=i<14; int n=i%14;
            float x=Width*(right ? .77f+(n*37%17)/100f : .13f+(n*23%13)/100f);
            float y=Height*(right ? .18f+(n*31%22)/100f : .60f+(n*19%18)/100f);
            using var paint=new SolidBrush(Color.FromArgb(85+n*8,217,227,255));
            using var halo=new SolidBrush(Color.FromArgb(20,187,201,255)); e.Graphics.FillEllipse(halo,x-2,y-2,6,6);
            float size=1.5f+(n%3)*.5f; e.Graphics.FillEllipse(paint,x,y,size,size);
        }
    }
    void Glow(Graphics g,float x,float y,Color color)
    {
        using var path=new GraphicsPath(); path.AddEllipse(Width*(x-.8f),Height*(y-.8f),Width*1.6f,Height*1.6f);
        using var brush=new PathGradientBrush(path) { CenterColor=color, SurroundColors=[Color.Transparent] };
        g.FillPath(brush,path);
    }
    public static void Style(Control root)
    {
        root.ForeColor=Color.FromArgb(237,238,248);
        foreach(Control c in root.Controls) {
            c.Font=new Font("Arial",10);
            if(c is Button b) { b.FlatStyle=FlatStyle.Flat; b.FlatAppearance.BorderColor=Color.FromArgb(80,80,108); b.BackColor=Color.FromArgb(27,32,48); b.Padding=new Padding(10,5,10,5); }
            else if(c is ComboBox or TextBox) c.BackColor=Color.FromArgb(27,32,48);
            else c.BackColor=Color.Transparent;
            Style(c);
        }
    }
}

sealed class DesktopOptions
{
    public bool Notifications {get;set;}=true;
    public string Shortcut {get;set;}="";
    static string FilePath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"NotebookScanner","settings.json");
    public static DesktopOptions Load() { try { return JsonSerializer.Deserialize<DesktopOptions>(File.ReadAllText(FilePath))??new(); } catch(Exception ex) when(ex is IOException or JsonException or UnauthorizedAccessException) { return new(); } }
    public void Save() { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!); File.WriteAllText(FilePath,JsonSerializer.Serialize(this)); }
    public static void InstallShortcut(string chord)
    {
        string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"Notebook Scanner.lnk");
        object? shell=null, link=null;
        try {
            shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);
            dynamic s=shell!; link=s.CreateShortcut(path); dynamic l=link;
            l.TargetPath=Environment.ProcessPath!; l.WorkingDirectory=AppContext.BaseDirectory;
            l.Description="Notebook Scanner"; l.Hotkey=chord; l.Save();
        } finally {
            if(link!=null) Marshal.FinalReleaseComObject(link);
            if(shell!=null) Marshal.FinalReleaseComObject(shell);
        }
    }
}
