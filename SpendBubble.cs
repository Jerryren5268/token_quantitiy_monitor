using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

// A short, click-through decoration. It never performs I/O or takes focus.
class SpendBubble : Form {
    readonly Form pet;
    readonly string amount;
    readonly Timer timer=new Timer();
    readonly Stopwatch elapsed=Stopwatch.StartNew();
    readonly float scale;
    public SpendBubble(Form owner,string value){
        pet=owner;amount=value;scale=owner.ClientSize.Width/144f;
        FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;
        AutoScaleMode=AutoScaleMode.None;ClientSize=new Size((int)(166*scale),(int)(38*scale));
        BackColor=Style.Cream;DoubleBuffered=true;TopMost=owner.TopMost;
        using(var shape=Style.Round(ClientRectangle,(int)(10*scale)))Region=new Region(shape);
        timer.Interval=25;timer.Tick+=(s,e)=>Advance();Place(0);timer.Start();
    }
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x00000020|0x00000080;return p;}}
    protected override void WndProc(ref Message m){if(m.Msg==0x0084){m.Result=new IntPtr(-1);return;}if(m.Msg==0x0021){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
    void Place(double progress){var area=Screen.FromRectangle(pet.Bounds).WorkingArea;
        int x=pet.Left+(pet.Width-Width)/2;int y=pet.Top-Height-4-(int)(25*scale*progress);
        if(pet.Top-Height-30*scale<area.Top)y=pet.Bottom+4-(int)(15*scale*progress);
        Location=new Point(Math.Max(area.Left,Math.Min(x,area.Right-Width)),Math.Max(area.Top,Math.Min(y,area.Bottom-Height)));
    }
    void Advance(){if(pet.IsDisposed||!pet.Visible||elapsed.ElapsedMilliseconds>=1800){Close();return;}
        double p=elapsed.Elapsed.TotalMilliseconds/1800;Place(p);Opacity=p<0.55?1:Math.Max(0.01,(1-p)/0.45);Invalidate();}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        float s=scale;using(var gold=new SolidBrush(Color.FromArgb(238,178,60)))e.Graphics.FillEllipse(gold,9*s,9*s,20*s,20*s);
        using(var ink=new Pen(Color.FromArgb(161,105,33),2*s))e.Graphics.DrawLine(ink,19*s,13*s,19*s,25*s);
        using(var font=Style.Font(13*s,true))TextRenderer.DrawText(e.Graphics,amount,font,new Rectangle((int)(34*s),0,Width-(int)(39*s),Height),Style.Orange,TextFormatFlags.VerticalCenter|TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);
    }
    protected override void Dispose(bool disposing){if(disposing){timer.Stop();timer.Dispose();}base.Dispose(disposing);}
}
