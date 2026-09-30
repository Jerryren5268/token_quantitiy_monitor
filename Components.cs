using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

static class Style {
    public static Color Ink=Color.FromArgb(67,53,47),Muted=Color.FromArgb(134,119,108),Cream=Color.FromArgb(255,250,242),Orange=Color.FromArgb(217,120,69),Line=Color.FromArgb(238,222,201),Mint=Color.FromArgb(97,150,125);
    public static Font Font(float size,bool bold=false){return new Font("Microsoft YaHei UI",size*1.333333f,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);}
    public static GraphicsPath Round(Rectangle r,int radius){var p=new GraphicsPath();int d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
    public static Dictionary<string,object> Obj(object o){return o as Dictionary<string,object> ?? new Dictionary<string,object>();}
    public static object Get(object o,string k){object v;return Obj(o).TryGetValue(k,out v)?v:null;}
    public static string Text(object o,string k,string fallback=""){var v=Get(o,k);return v==null?fallback:Convert.ToString(v);}
    public static bool Yes(object o,string k){return Get(o,k) is bool && (bool)Get(o,k);}
    public static double? Number(object o,string k){var v=Get(o,k);double d;return v!=null&&Double.TryParse(Convert.ToString(v),out d)?(double?)d:null;}
    public static IEnumerable Items(object o){return o as IEnumerable ?? new object[0];}
    public static string Money(double? value,double? unit){if(!value.HasValue)return "—";return unit.HasValue&&unit.Value>0?"$"+(value.Value/unit.Value).ToString("N2"):value.Value.ToString("N0")+" 单位";}
    public static string Date(object value){try{if(value==null)return "—";double n;if(Double.TryParse(Convert.ToString(value),out n))return new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(n).ToLocalTime().ToString("MM/dd HH:mm");return DateTime.Parse(Convert.ToString(value),null,System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime().ToString("MM/dd HH:mm");}catch{return "—";}}
}

class SoftButton : Button {
    public bool Accent=false;
    bool hover;
    public SoftButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Font=Style.Font(9);Cursor=Cursors.Hand;Size=new Size(85,32);TabStop=true;}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var c=Accent?Style.Orange:(hover?Color.FromArgb(247,227,203):Color.FromArgb(249,239,225));if(!Enabled)c=Color.FromArgb(231,224,213);using(var p=Style.Round(new Rectangle(0,0,Width-1,Height-1),9))using(var b=new SolidBrush(c))e.Graphics.FillPath(b,p);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Accent&&Enabled?Color.White:Style.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(4,4,Width-8,Height-8));}
}

class Meter : Control {
    public double Value;
    public Meter(){Height=7;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var p=Style.Round(new Rectangle(0,0,Width-1,Height-1),3))using(var b=new SolidBrush(Style.Line))e.Graphics.FillPath(b,p);int w=(int)((Width-1)*Math.Max(0,Math.Min(100,Value))/100);if(w>=6)using(var p=Style.Round(new Rectangle(0,0,w,Height-1),3))using(var b=new SolidBrush(Value<15?Style.Orange:Style.Mint))e.Graphics.FillPath(b,p);}
}

static class CatArt {
    static void Fill(Graphics g,Color c,int x,int y,int w,int h){using(var b=new SolidBrush(c))g.FillRectangle(b,x,y,w,h);}
    public static Bitmap Frame(bool blink,int tail){
        var b=new Bitmap(32,32,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(b)){
            Color edge=Color.FromArgb(105,72,53),fur=Color.FromArgb(255,236,201),light=Color.FromArgb(255,249,226),ginger=Color.FromArgb(230,156,85),pink=Color.FromArgb(233,154,140);
            // Compact, hand-drawn pixel silhouette: ears, head, plump body and curled tail.
            Fill(g,edge,25,20,3,8);Fill(g,edge,27,18+tail,3,9-tail);Fill(g,edge,29,17+tail,2,5);Fill(g,ginger,26,22,2,5);Fill(g,ginger,28,19+tail,1,6-tail);Fill(g,fur,29,18+tail,1,3);
            Fill(g,edge,9,19,17,10);Fill(g,edge,11,28,13,2);Fill(g,fur,10,20,15,8);Fill(g,ginger,21,20,4,6);Fill(g,light,13,21,7,7);
            Fill(g,edge,6,3,2,8);Fill(g,edge,8,4,2,7);Fill(g,edge,10,6,2,5);Fill(g,ginger,7,5,2,5);Fill(g,pink,8,7,2,3);
            Fill(g,edge,24,3,2,8);Fill(g,edge,22,4,2,7);Fill(g,edge,20,6,2,5);Fill(g,fur,23,5,2,6);Fill(g,pink,22,7,2,3);
            Fill(g,edge,9,6,14,2);Fill(g,edge,6,8,20,12);Fill(g,edge,8,19,16,3);Fill(g,fur,7,9,18,10);Fill(g,fur,9,8,14,13);Fill(g,light,10,15,12,5);
            Fill(g,ginger,8,9,6,4);Fill(g,ginger,10,8,5,2);Fill(g,ginger,11,12,2,2);Fill(g,ginger,20,8,2,3);
            if(blink){Fill(g,edge,10,14,3,1);Fill(g,edge,20,14,3,1);}else{Fill(g,edge,11,12,2,3);Fill(g,edge,20,12,2,3);Fill(g,light,11,12,1,1);Fill(g,light,20,12,1,1);}
            Fill(g,pink,8,16,3,1);Fill(g,pink,22,16,2,1);Fill(g,edge,16,16,2,1);Fill(g,edge,15,18,1,1);Fill(g,edge,18,18,1,1);Fill(g,pink,16,17,2,1);
            Fill(g,edge,4,14,3,1);Fill(g,edge,4,17,3,1);Fill(g,edge,25,14,3,1);Fill(g,edge,25,17,3,1);
            Fill(g,edge,9,26,6,4);Fill(g,edge,19,26,6,4);Fill(g,light,10,26,4,3);Fill(g,fur,20,26,4,3);Fill(g,ginger,11,28,1,1);Fill(g,ginger,22,28,1,1);
        }return b;
    }
}

