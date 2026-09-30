using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

class PixelPet : Form {
    [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr v);
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr h);
    readonly AccountPipe pipe=new AccountPipe();readonly QuotaCard card;readonly System.Windows.Forms.Timer animation=new System.Windows.Forms.Timer();
    readonly System.Windows.Forms.Timer refreshTimer=new System.Windows.Forms.Timer(); string headAmount="—",headNote="等待更新";
    SpendBubble spendBubble;
    readonly NotifyIcon tray=new NotifyIcon();readonly EventWaitHandle wake; RegisteredWaitHandle wakeRegistration;readonly string settingsFile;
    readonly Bitmap[] frames=new Bitmap[4];int frame=-1,tick;bool dragging,pressed,quitting;Point down,origin;int logicalSize=96,refreshMinutes=5,petScalePercent=100;
    public PixelPet(EventWaitHandle wakeEvent){wake=wakeEvent;settingsFile=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LMServiceQuota","settings.json");
        Text="LMService 像素猫";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;AutoScaleMode=AutoScaleMode.None;ClientSize=new Size(144,140);BackColor=Color.Magenta;TransparencyKey=Color.Magenta;DoubleBuffered=true;Cursor=Cursors.Hand;
        frames[0]=CatArt.Frame(false,0);frames[1]=CatArt.Frame(false,1);frames[2]=CatArt.Frame(true,0);frames[3]=CatArt.Frame(true,1);
        var area=Screen.PrimaryScreen.WorkingArea;Location=new Point(area.Right-150,area.Bottom-145);LoadPosition();
        card=new QuotaCard(pipe);card.OverPet=()=>Bounds.Contains(Cursor.Position);
        card.QuotaChanged=(amount,note)=>{headAmount=amount;headNote=note;Invalidate();};
        card.SpendingDetected=(amount)=>{if(!Visible||IsDisposed)return;if(spendBubble!=null&&!spendBubble.IsDisposed)spendBubble.Close();spendBubble=new SpendBubble(this,amount);spendBubble.Show(this);};
        card.SettingsRequested=ShowRefreshSettings;card.SetRefreshMode(refreshMinutes);refreshTimer.Interval=Math.Max(1,refreshMinutes)*60000;refreshTimer.Tick+=async(s,e)=>{if(card.CanAutoRefresh)await card.RefreshData();};
        var menu=new ContextMenuStrip{Font=Style.Font(9)};
        menu.Items.Add("查看额度",null,async(s,e)=>{ShowPet();card.ShowNear(Bounds);await card.OnOpen();});
        menu.Items.Add("桌宠设置",null,(s,e)=>ShowRefreshSettings());
        var pinned=new ToolStripMenuItem("始终置顶"){Checked=TopMost,CheckOnClick=true};pinned.CheckedChanged+=(s,e)=>{TopMost=pinned.Checked;card.TopMost=TopMost;SavePosition();};menu.Items.Add(pinned);
        menu.Items.Add("隐藏到托盘",null,(s,e)=>{card.Hide();Hide();animation.Stop();});menu.Items.Add(new ToolStripSeparator());menu.Items.Add("退出桌宠",null,(s,e)=>Quit());ContextMenuStrip=menu;
        tray.Text="LMService 像素猫 · 双击显示";using(var icoBmp=CatArt.Frame(false,0)){var h=icoBmp.GetHicon();using(var ico=Icon.FromHandle(h))tray.Icon=(Icon)ico.Clone();DestroyIcon(h);}tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>ShowPet();tray.Visible=true;
        wakeRegistration=ThreadPool.RegisterWaitForSingleObject(wake,(s,t)=>{try{if(!IsDisposed)BeginInvoke((Action)ShowPet);}catch{}},null,-1,false);animation.Interval=100;animation.Tick+=(s,e)=>{tick++;int next=(tick%48==40||tick%48==41?2:0)+(tick%28>=20?1:0);if(next!=frame)SetFrame(next);};
        Shown+=async(s,e)=>{FitSize();ClampPosition();SetFrame(0);animation.Start();await card.RestoreState();if(refreshMinutes>0)await card.RefreshData();if(!IsDisposed)ApplyRefreshMode();};
        MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;pressed=true;dragging=false;down=Cursor.Position;origin=Location;Capture=true;};
        MouseMove+=(s,e)=>{if(!pressed)return;var p=Cursor.Position;int dx=p.X-down.X,dy=p.Y-down.Y;if(Math.Abs(dx)>4||Math.Abs(dy)>4)dragging=true;if(dragging){Location=new Point(origin.X+dx,origin.Y+dy);if(card.Visible)card.Hide();}};
        MouseUp+=async(s,e)=>{if(e.Button!=MouseButtons.Left||!pressed)return;pressed=false;Capture=false;if(dragging){ClampPosition();SavePosition();return;}if(card.Visible)card.Hide();else{card.TopMost=TopMost;card.ShowNear(Bounds);await card.OnOpen();}};
        MouseCaptureChanged+=(s,e)=>{if(pressed&&!Capture){pressed=false;ClampPosition();SavePosition();}};
        FormClosing+=(s,e)=>{if(!quitting){quitting=true;SavePosition();}if(spendBubble!=null&&!spendBubble.IsDisposed)spendBubble.Close();refreshTimer.Stop();animation.Stop();tray.Visible=false;pipe.Dispose();card.Dispose();};
        FormClosed+=(s,e)=>{if(wakeRegistration!=null)wakeRegistration.Unregister(null);tray.Dispose();animation.Dispose();refreshTimer.Dispose();foreach(var f in frames)f.Dispose();};
    }
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);
    void ApplyRefreshMode(){refreshTimer.Stop();card.SetRefreshMode(refreshMinutes);if(refreshMinutes>0){refreshTimer.Interval=refreshMinutes*60000;refreshTimer.Start();}}
    void ShowRefreshSettings(){card.WithSettings(()=>{
        using(var dialog=new RefreshSettings(refreshMinutes,petScalePercent)){
            if(dialog.ShowDialog(card.Visible?(IWin32Window)card:this)!=DialogResult.OK)return;
            int previous=refreshMinutes,previousScale=petScalePercent;var previousPosition=Location;
            int center=Left+Width/2,bottom=Bottom;
            refreshMinutes=dialog.RefreshMinutes;petScalePercent=dialog.PetScalePercent;
            if(spendBubble!=null&&!spendBubble.IsDisposed)spendBubble.Close();
            FitSize();Location=new Point(center-Width/2,bottom-Height);ClampPosition();
            if(!SavePosition()){
                refreshMinutes=previous;petScalePercent=previousScale;FitSize();Location=previousPosition;ClampPosition();
                MessageBox.Show("设置保存失败，请检查本地目录权限。","桌宠设置");
            }
            ApplyRefreshMode();if(card.Visible)card.ShowNear(Bounds);
        }
    });}
    void Quit(){quitting=true;SavePosition();Close();}
    void ShowPet(){Show();ClampPosition();animation.Start();Activate();}
    void FitSize(){
        double dpi=96;try{uint value=GetDpiForWindow(Handle);if(value>0)dpi=value;}catch{}
        logicalSize=Math.Max(48,(int)Math.Round(dpi*petScalePercent/100.0));
        ClientSize=new Size((int)Math.Round(logicalSize*1.5),(int)Math.Round(logicalSize*140.0/96));
        if(frame>=0)SetFrame(frame);
    }
    void ClampPosition(){var area=Screen.FromRectangle(Bounds).WorkingArea;Location=new Point(Math.Max(area.Left,Math.Min(Left,area.Right-Width)),Math.Max(area.Top,Math.Min(Top,area.Bottom-Height)));}
    void LoadPosition(){try{if(!File.Exists(settingsFile))return;var d=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(settingsFile));Location=new Point(Convert.ToInt32(d["x"]),Convert.ToInt32(d["y"]));TopMost=Style.Yes(d,"topmost");var size=Style.Number(d,"petScalePercent");if(size.HasValue)petScalePercent=(int)Math.Max(50,Math.Min(200,size.Value));var frequency=Style.Number(d,"refreshMinutes");if(frequency.HasValue)refreshMinutes=(int)Math.Max(0,Math.Min(1440,frequency.Value));ClampPosition();}catch{}}
    bool SavePosition(){try{Directory.CreateDirectory(Path.GetDirectoryName(settingsFile));var text=new JavaScriptSerializer().Serialize(new {x=Left,y=Top,topmost=TopMost,refreshMinutes=refreshMinutes,petScalePercent=petScalePercent});var tmp=settingsFile+".tmp";File.WriteAllText(tmp,text);if(File.Exists(settingsFile))File.Replace(tmp,settingsFile,null);else File.Move(tmp,settingsFile);return true;}catch{return false;}}
    void SetFrame(int value){frame=value;var b=frames[frame];var region=new Region();region.MakeEmpty();float scale=logicalSize/32f;float offsetX=(ClientSize.Width-logicalSize)/2f,offsetY=ClientSize.Height-logicalSize;using(var badge=Style.Round(new Rectangle(1,1,ClientSize.Width-2,(int)offsetY-2),8))region.Union(badge);for(int y=0;y<32;y++){int x=0;while(x<32){while(x<32&&b.GetPixel(x,y).A==0)x++;int start=x;while(x<32&&b.GetPixel(x,y).A!=0)x++;if(x>start)region.Union(new RectangleF(offsetX+start*scale,offsetY+y*scale,(x-start)*scale,scale));}}var old=Region;Region=region;if(old!=null)old.Dispose();Invalidate();}
    protected override void OnPaint(PaintEventArgs e){if(frame<0)return;e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(frames[frame],new Rectangle((ClientSize.Width-logicalSize)/2,ClientSize.Height-logicalSize,logicalSize,logicalSize));
        float factor=logicalSize/96f;int badgeHeight=ClientSize.Height-logicalSize;
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var badge=Style.Round(new Rectangle(1,1,ClientSize.Width-2,badgeHeight-2),8))using(var fill=new SolidBrush(Style.Cream))e.Graphics.FillPath(fill,badge);
        using(var font=Style.Font(13*factor,true))TextRenderer.DrawText(e.Graphics,headAmount,font,new Rectangle(0,1,Width,(int)(24*factor)),Style.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        using(var font=Style.Font(6.8f*factor))TextRenderer.DrawText(e.Graphics,headNote,font,new Rectangle(0,(int)(24*factor),Width,(int)(17*factor)),Style.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);}
    protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x02E0){FitSize();ClampPosition();}}

    static void Preview(string directory){Directory.CreateDirectory(directory);using(var sprite=CatArt.Frame(false,0)){sprite.Save(Path.Combine(directory,"cat-32.png"));var iconHandle=sprite.GetHicon();using(var icon=Icon.FromHandle(iconHandle))using(var file=File.Create(Path.Combine(directory,"cat.ico")))icon.Save(file);DestroyIcon(iconHandle);using(var image=new Bitmap(384,384)){using(var g=Graphics.FromImage(image)){g.Clear(Color.FromArgb(246,238,224));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(sprite,new Rectangle(48,48,288,288));}image.Save(Path.Combine(directory,"cat-preview.png"),ImageFormat.Png);}}
        var sample=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>("{\"logged_in\":true,\"personal\":{\"data\":{\"balance\":40000000,\"used\":1200000,\"unit\":500000,\"subscriptions\":[{\"id\":59,\"total\":100000000,\"used\":8100000,\"ends\":1791046800,\"status\":\"active\"}]},\"fetched_at\":1790589600},\"site\":{\"data\":{\"windows\":[{\"name\":\"主额度\",\"period\":\"1 周\",\"used_percent\":52,\"resets\":1791047818}],\"collected_at\":\"2026-09-28T10:00:00Z\"},\"fetched_at\":1790589600}}");
        using(var c=new QuotaCard(null,true)){c.Preview(sample);c.StartPosition=FormStartPosition.Manual;c.Location=new Point(-20000,-20000);c.Show();Application.DoEvents();c.PerformLayout();using(var b=new Bitmap(c.Width,c.Height)){c.DrawToBitmap(b,c.ClientRectangle);b.Save(Path.Combine(directory,"quota-card.png"));}c.PreviewAccount();c.PerformLayout();using(var b=new Bitmap(c.Width,c.Height)){c.DrawToBitmap(b,c.ClientRectangle);b.Save(Path.Combine(directory,"account-card.png"));}}
    }
    [STAThread] public static void Main(string[] args){try{try{SetProcessDpiAwarenessContext(new IntPtr(-4));}catch{SetProcessDPIAware();}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        if(args.Length==2&&args[0]=="--preview"){Preview(args[1]);return;}
        string sid=WindowsIdentity.GetCurrent().User.Value;bool fresh;
        using(var mutex=new Mutex(true,"Local\\LMServicePixelPet_"+sid,out fresh))using(var wakeEvent=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\LMServicePixelPetWake_"+sid)){if(!fresh){wakeEvent.Set();return;}Application.Run(new PixelPet(wakeEvent));}
        }catch(Exception ex){MessageBox.Show("桌宠启动失败："+ex.Message,"LMService 像素猫",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
}


