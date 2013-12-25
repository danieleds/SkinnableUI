using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Xml;
using ExtensionMethods;

namespace UnusefulPlayer.PlayerControls
{
    abstract class PlayerControl
    {
        [System.AttributeUsage(System.AttributeTargets.Field)]
        public class SemanticTypeMeta : System.Attribute
        {
            public string Title;
            public Type InstanceType;

            public SemanticTypeMeta(string title, Type instanceType)
            {
                this.Title = title;
                this.InstanceType = instanceType;
            }
        }

        public enum SemanticType
        {
            [SemanticTypeMeta("Container", typeof(PlayerControls.Container))] Container,
            [SemanticTypeMeta("Play", typeof(PlayerControls.Button))] Play,
            [SemanticTypeMeta("Pause", typeof(PlayerControls.Button))] Pause,
            [SemanticTypeMeta("Play/Pause", typeof(PlayerControls.ToggleButton))] PlayPause,
            [SemanticTypeMeta("Stop", typeof(PlayerControls.Button))] Stop,
            [SemanticTypeMeta("Back", typeof(PlayerControls.Button))] Back,
            [SemanticTypeMeta("Forward", typeof(PlayerControls.Button))] Forward,
            [SemanticTypeMeta("Song Progress", typeof(PlayerControls.TrackBar))] SongProgress,
            [SemanticTypeMeta("Current Time", typeof(PlayerControls.Label))] CurrentTime,
            [SemanticTypeMeta("Total Time", typeof(PlayerControls.Label))] TotalTime,
            [SemanticTypeMeta("Remaining Time", typeof(PlayerControls.Label))] RemainingTime,
            [SemanticTypeMeta("Title", typeof(PlayerControls.Label))] Title,
            [SemanticTypeMeta("Artist", typeof(PlayerControls.Label))] Artist,
            [SemanticTypeMeta("Album", typeof(PlayerControls.Label))] Album,
            [SemanticTypeMeta("Year", typeof(PlayerControls.Label))] Year,
            [SemanticTypeMeta("Free Text", typeof(PlayerControls.Label))] FreeText,
            [SemanticTypeMeta("Playlist", typeof(PlayerControls.ListView))] Playlist
        }

        protected PlayerControl(SemanticType controlType)
        {
            this.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.Semantic = controlType;
        }

        public static SemanticTypeMeta GetPlayerControlInstanceInfo(SemanticType t)
        {
            var tp = typeof(SemanticType);
            var fi = tp.GetField(t.ToString());
            var attrs = fi.GetCustomAttributes(typeof(SemanticTypeMeta), true);
            return (SemanticTypeMeta)attrs[0];
        }

        [Browsable(false)]
        public string DisplayName
        {
            get
            {
                var meta = GetPlayerControlInstanceInfo(this.semantic);
                return meta.Title + " (" + meta.InstanceType.Name + ")";
            }
        }

        public delegate void MouseDownEventHandler(object sender, MouseEventArgs e);
        public event MouseDownEventHandler MouseDown;

        public delegate void MouseMoveEventHandler(object sender, MouseEventArgs e);
        public event MouseMoveEventHandler MouseMove;

        public delegate void MouseUpEventHandler(object sender, MouseEventArgs e);
        public event MouseUpEventHandler MouseUp;

        public delegate void ClickEventHandler(object sender, EventArgs e);
        public event ClickEventHandler Click;

        public delegate void MouseHoverEventHandler(object sender, EventArgs e);
        public event MouseHoverEventHandler MouseHover;

        public delegate void MouseLeaveEventHandler(object sender, EventArgs e);
        public event MouseLeaveEventHandler MouseLeave;

        public delegate void ResizeEventHandler(object sender, EventArgs e);
        public event ResizeEventHandler Resize;

        public delegate void MoveEventHandler(object sender, EventArgs e);
        public event MoveEventHandler Move;

        [Browsable(false)]
        public PlayerView ParentView { get; set; }

        public Container parent = null;
        [Browsable(false)]
        public Container Parent
        {
            get { return this.parent; }
            set
            {
                if (value == null)
                {
                    if (this.parent != null && this.parent.Controls.Contains(this))
                        this.parent.RemovePlayerControl(this);
                    this.parent = value;
                }
                else if (value != this.parent)
                {
                    if (this.parent != null && this.parent.Controls.Contains(this))
                        this.parent.RemovePlayerControl(this);
                    if (!value.Controls.Contains(this))
                        value.AddPlayerControl(this);
                    this.parent = value;
                }
            }
        }

        /// <summary>
        /// Ottiene la posizione di questo controllo relativamente al padre top-level
        /// </summary>
        /// <returns></returns>
        public PointF GetAbsoluteLocation()
        {
            float lx = 0, ly = 0;
            var c = this;
            while (c.Parent != null)
            {
                lx += c.location.X;
                ly += c.location.Y;
                c = c.Parent;
            }

            return new PointF(lx, ly);
        }

        [Browsable(false)]
        public bool Capture { get; set; }

        private PointF location = new PointF(0, 0);
        [Browsable(false)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public PointF Location
        {
            get { return this.location; }
            set { this.location = value; this.InvalidateParent(); OnMove(new EventArgs()); }
        }

        public float Top
        {
            get { return this.location.Y; }
            set { this.location.Y = value; this.InvalidateParent(); OnMove(new EventArgs()); }
        }

        public float Left
        {
            get { return this.location.X; }
            set { this.location.X = value; this.InvalidateParent(); OnMove(new EventArgs()); }
        }

        [DefaultValue(AnchorStyles.Top | AnchorStyles.Left)]
        public AnchorStyles Anchor { get; set; }

        private SizeF size = new SizeF(50, 50);
        [Description("The size of the control in pixels.")]
        public SizeF Size
        {
            get { return this.size; }
            set
            {
                var oldsize = this.size;
                this.size = value;
                this.InvalidateParent(new RectangleF(this.Left, this.Top, Math.Max(this.size.Width, oldsize.Width), Math.Max(this.size.Height, oldsize.Height)));
                //this.InvalidateParent();
                OnResize(new EventArgs()); // FIXME Generare l'evento anche alla creazione del controllo?
            }
        }

        private SemanticType semantic; // FIXME: A cosa lo inizializziamo??
        [Browsable(false)]
        public SemanticType Semantic
        {
            get { return this.semantic; }
            set 
            {
                SemanticTypeMeta i = GetPlayerControlInstanceInfo(value);
                if (i.InstanceType == this.GetType())
                {
                    this.semantic = value;
                }
                else
                {
                    throw new Exception("ControlType not valid for this control. This is a " + this.GetType().FullName + ", but a " + i.InstanceType.FullName + " was required.");
                }
            }
        }

        private Font font = SystemFonts.DefaultFont;
        public Font Font
        {
            get { return font; }
            set { font = (value == null ? SystemFonts.DefaultFont : value); this.Invalidate(); }
        }

        private Color foreColor = Color.Black;
        [DefaultValue(typeof(Color), "0x000000")]
        public Color ForeColor
        {
            get { return foreColor; }
            set { foreColor = value; this.Invalidate(); }
        }

        protected abstract void OnPaint(Graphics g);

        public void InternalPaint(Graphics g)
        {
            var s = g.Save();
            // FIXME Se impostiamo una posizione non intera, si rischia di perdere il bordo sx o sup del controllo...
            g.TranslateTransform(location.X, location.Y);
            g.SetClip(new RectangleF(new PointF(), this.size), System.Drawing.Drawing2D.CombineMode.Intersect);
            this.OnPaint(g);
            g.Restore(s);
        }

        public Bitmap ToBitmap()
        {
            Bitmap b = new Bitmap((int)Math.Ceiling(this.Size.Width), (int)Math.Ceiling(this.Size.Height));
            var g = Graphics.FromImage(b);
            using (g)
            {
                //g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                this.OnPaint(g);
            }
            return b;
        }

        /// <summary>
        /// Invalida il controllo corrente
        /// </summary>
        public void Invalidate()
        {
            Invalidate(new RectangleF(0, 0, this.size.Width, this.size.Height));
            /*if (ParentView != null)
            {
                var absLoc = this.GetAbsoluteLocation();
                ParentView.Invalidate(new Rectangle((int)absLoc.X, (int)absLoc.Y, (int)this.size.Width + 1, (int)this.size.Height + 1));
            }*/
        }

        /// <summary>
        /// Invalida una particolare area del controllo corrente
        /// </summary>
        public void Invalidate(RectangleF rc)
        {
            if (ParentView != null)
            {
                var absLoc = this.GetAbsoluteLocation();
                ParentView.Invalidate(new Rectangle(
                    (int)Math.Floor(absLoc.X + rc.X),
                    (int)Math.Floor(absLoc.Y + rc.Y),
                    (int)Math.Ceiling(rc.Width),
                    (int)Math.Ceiling(rc.Height)));
            }
        }

        /// <summary>
        /// Invalida il controllo genitore (sia che sia un Container che un PlayerView)
        /// </summary>
        private void InvalidateParent()
        {
            if (this.Parent != null)
                this.Parent.Invalidate();
            else if (this.ParentView != null)
                this.ParentView.Invalidate();
        }

        private void InvalidateParent(RectangleF rc)
        {
            if (this.Parent != null)
                this.Parent.Invalidate(rc);
            else if (this.ParentView != null)
                this.ParentView.Invalidate(rc.RoundUp());
        }

        public virtual XmlElement GetXmlElement(XmlDocument document, Dictionary<string, System.IO.MemoryStream> resources)
        {
            XmlElement node = document.CreateElement(this.Semantic.ToString());
            // FIXME Z-index
            var inv = System.Globalization.NumberFormatInfo.InvariantInfo;
            node.SetAttribute("x", this.Left.ToString(inv));
            node.SetAttribute("y", this.Top.ToString(inv));
            node.SetAttribute("width", this.Size.Width.ToString(inv));
            node.SetAttribute("height", this.Size.Height.ToString(inv));
            node.SetAttribute("anchor", this.Anchor.ToString());
            node.SetAttribute("forecolor", string.Format("#{0:x6}", this.ForeColor.ToArgb()));
            node.SetAttribute("font", new FontConverter().ConvertToInvariantString(this.Font));
            
            return node;
        }

        public virtual void FromXmlElement(XmlElement element, Dictionary<String, System.IO.MemoryStream> resources)
        {
            SerializationHelper.LoadFloat(element, "x", s => this.Left = s);
            SerializationHelper.LoadFloat(element, "y", s => this.Top = s);
            SerializationHelper.LoadFloat(element, "width", s => this.Size = new SizeF(s, this.Size.Height));
            SerializationHelper.LoadFloat(element, "height", s => this.Size = new SizeF(this.Size.Width, s));
            SerializationHelper.LoadEnum<AnchorStyles>(element, "anchor", s => this.Anchor = s);
            SerializationHelper.LoadColor(element, "forecolor", s => this.ForeColor = s);
            SerializationHelper.LoadFont(element, "font", s => this.Font = s);
        }

        // Lavora nelle coordinate globali
        public bool HitTest(PointF p)
        {
            return this.IsInside(new PointF(p.X - location.X, p.Y - location.Y));
        }

        // Lavora nelle coordinate del controllo
        public virtual bool IsInside(PointF p)
        {
            // FIXME Opzione per permettere di escludere le parti trasparenti! http://bobpowell.net/region_from_bitmap.aspx
            var inRectangle = new RectangleF(new PointF(), this.size).Contains(p);
            /*if (inRectangle)
            {
                var b = this.to
            }*/
            return inRectangle;
        }
        
        public virtual void OnMouseDown(MouseEventArgs e)
        {
            this.Capture = true;
            if(MouseDown != null) MouseDown(this, e);
        }

        public virtual void OnMouseMove(MouseEventArgs e)
        {
            if (MouseMove != null) MouseMove(this, e);
        }

        public virtual void OnMouseUp(MouseEventArgs e)
        {
            this.Capture = false;
            if (MouseUp != null) MouseUp(this, e);
        }

        public virtual void OnClick(EventArgs e)
        {
            if (Click != null) Click(this, e);
        }

        public virtual void OnMouseHover(EventArgs e)
        {
            if (MouseHover != null) MouseHover(this, e);
        }

        public virtual void OnMouseLeave(EventArgs e)
        {
            if (MouseLeave != null) MouseLeave(this, e);
        }

        public virtual void OnResize(EventArgs e)
        {
            if (Resize != null) Resize(this, e);
        }

        public virtual void OnMove(EventArgs e)
        {
            if (Move != null) Move(this, e);
        }
    }
}
