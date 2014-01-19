using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;

namespace PlayerUI.PlayerControls
{
    public class ToggleButton : PlayerControl
    {

        public ToggleButton(SemanticType c) : base(c)
        {
            this.Size = new SizeF(75, 30);
        }

        private bool pressed;
        private bool over;
        Animation anim;
        NinePatch lastDrawnPatch = null;

        public delegate void CheckedChangedEventHandler(object sender, EventArgs e);
        public event CheckedChangedEventHandler CheckedChanged;

        private void set9P(Bitmap value, Action<NinePatch> setter)
        {
            if (value == null)
                setter(null);
            else
                setter(new NinePatch(value));
            this.Invalidate();
        }

        protected NinePatch backgroundNormal9P;
        [DefaultValue(null)]
        public Bitmap BackgroundNormal9P
        {
            get { return backgroundNormal9P != null ? backgroundNormal9P.Image : null; }
            set { set9P(value, v => this.backgroundNormal9P = v); }
        }

        protected NinePatch backgroundOver9P;
        [DefaultValue(null)]
        public Bitmap BackgroundOver9P
        {
            get { return backgroundOver9P != null ? backgroundOver9P.Image : null; }
            set { set9P(value, v => this.backgroundOver9P = v); }
        }

        protected NinePatch backgroundPressed9P;
        [DefaultValue(null)]
        public Bitmap BackgroundPressed9P
        {
            get { return backgroundPressed9P != null ? backgroundPressed9P.Image : null; }
            set { set9P(value, v => this.backgroundPressed9P = v); }
        }

        protected NinePatch backgroundCheckedNormal9P;
        [DefaultValue(null)]
        public Bitmap BackgroundCheckedNormal9P
        {
            get { return backgroundCheckedNormal9P != null ? backgroundCheckedNormal9P.Image : null; }
            set { set9P(value, v => this.backgroundCheckedNormal9P = v); }
        }

        protected NinePatch backgroundCheckedOver9P;
        [DefaultValue(null)]
        public Bitmap BackgroundCheckedOver9P
        {
            get { return backgroundCheckedOver9P != null ? backgroundCheckedOver9P.Image : null; }
            set { set9P(value, v => this.backgroundCheckedOver9P = v); }
        }

        protected NinePatch backgroundCheckedPressed9P;
        [DefaultValue(null)]
        public Bitmap BackgroundCheckedPressed9P
        {
            get { return backgroundCheckedPressed9P != null ? backgroundCheckedPressed9P.Image : null; }
            set { set9P(value, v => this.backgroundCheckedPressed9P = v); }
        }

        /// <summary>
        /// True per attivare le animazioni su mouse enter / mouse leave.
        /// </summary>
        [DefaultValue(false)]
        public bool EnterLeave9PAnimation { get; set; }

        private bool checked_;
        public bool Checked
        {
            get { return checked_; }
            set
            {
                checked_ = value;
                this.Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, new EventArgs());
            }
        }

        private string text;
        public string Text { get { return text; } set { text = value; if(!checked_) this.Invalidate(); } }

        private string checkedText;
        public string CheckedText { get { return checkedText; } set { checkedText = value; if(checked_) this.Invalidate(); } }
        
        protected override void OnPaint(System.Drawing.Graphics g)
        {
            var actualText = this.checked_ ? this.CheckedText : this.Text;
            var strSize = g.MeasureString(actualText, this.Font);
            var contentBox = new RectangleF(0, 0, this.Size.Width, this.Size.Height);

            var patch =
                this.pressed ?
                    this.checked_ ? backgroundCheckedPressed9P : backgroundPressed9P
                : this.over ?
                    this.checked_ ? backgroundCheckedOver9P : backgroundOver9P
                :
                    this.checked_ ? backgroundCheckedNormal9P : backgroundNormal9P;

            if (patch != null)
            {
                patch = anim != null && anim.IsRunning() ? anim.GetCurrentFrame() : patch;
                lastDrawnPatch = patch;
                contentBox = patch.GetContentBox(this.Size);
                patch.Paint(g, this.Size);
            }
            else
                drawDefaultToggleButton(g);

            g.SetClip(contentBox, System.Drawing.Drawing2D.CombineMode.Intersect);
            g.DrawString(actualText, this.Font, new SolidBrush(this.ForeColor), contentBox.Width / 2 - strSize.Width / 2 + contentBox.Left, contentBox.Height / 2 - strSize.Height / 2 + contentBox.Top);
        }

        private void drawDefaultToggleButton(Graphics g)
        {
            if (!this.Checked)
                g.FillRectangle(SystemBrushes.Control, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
            else
                g.FillRectangle(SystemBrushes.ControlDark, 0, 0, this.Size.Width - 1, this.Size.Height - 1);

            g.DrawRectangle(SystemPens.ButtonShadow, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
        }

        public override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.pressed = true;
                this.Invalidate();
            }
        }

        public override void OnMouseUp(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.pressed = false;
                this.Invalidate();
            }
        }

        public override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.over = true;

            if (EnterLeave9PAnimation)
            {
                if (this.checked_)
                {
                    if (this.backgroundCheckedOver9P != null && this.lastDrawnPatch != null)
                        this.GetAnimator().ContinueAnimationB(ref anim, lastDrawnPatch, this.backgroundCheckedOver9P, 50, 300, this.Invalidate);
                }
                else
                {
                    if (this.backgroundOver9P != null && this.lastDrawnPatch != null)
                        this.GetAnimator().ContinueAnimationB(ref anim, lastDrawnPatch, this.backgroundOver9P, 50, 300, this.Invalidate);
                }
            }

            this.Invalidate();
        }

        public override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.over = false;

            if (EnterLeave9PAnimation)
            {
                if (this.checked_)
                {
                    if (this.backgroundCheckedNormal9P != null && this.lastDrawnPatch != null)
                        this.GetAnimator().ContinueAnimationB(ref anim, lastDrawnPatch, this.backgroundCheckedNormal9P, 50, 300, this.Invalidate);
                }
                else
                {
                    if (this.backgroundNormal9P != null && this.lastDrawnPatch != null)
                        this.GetAnimator().ContinueAnimationB(ref anim, lastDrawnPatch, this.backgroundNormal9P, 50, 300, this.Invalidate);
                }
            }

            this.Invalidate();
        }

        public override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            this.Checked = !this.Checked;
        }

        public override System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument document, Dictionary<string, System.IO.MemoryStream> resources)
        {
            var node = base.GetXmlElement(document, resources);
            node.SetAttribute("text", this.Text);
            node.SetAttribute("checkedText", this.CheckedText);
            node.SetAttribute("checked", System.Xml.XmlConvert.ToString(this.Checked));
            node.SetAttribute("enterLeave9PAnimation", System.Xml.XmlConvert.ToString(this.EnterLeave9PAnimation));

            SerializationHelper.SetNinePatch(this.backgroundNormal9P, "backgroundNormal9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundOver9P, "backgroundOver9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundPressed9P, "backgroundPressed9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundCheckedNormal9P, "backgroundCheckedNormal9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundCheckedOver9P, "backgroundCheckedOver9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundCheckedPressed9P, "backgroundCheckedPressed9P", resources, node);

            return node;
        }

        public override void FromXmlElement(System.Xml.XmlElement element, Dictionary<string, System.IO.MemoryStream> resources)
        {
            base.FromXmlElement(element, resources);
            SerializationHelper.LoadString(element, "text", s => this.Text = s);
            SerializationHelper.LoadString(element, "checkedText", s => this.CheckedText = s);
            SerializationHelper.LoadBoolean(element, "checked", s => this.Checked = s);
            SerializationHelper.LoadBoolean(element, "enterLeave9PAnimation", s => this.EnterLeave9PAnimation = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundNormal9P", resources, s => this.BackgroundNormal9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundOver9P", resources, s => this.BackgroundOver9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundPressed9P", resources, s => this.BackgroundPressed9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundCheckedNormal9P", resources, s => this.BackgroundCheckedNormal9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundCheckedOver9P", resources, s => this.BackgroundCheckedOver9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundCheckedPressed9P", resources, s => this.BackgroundCheckedPressed9P = s);
        }

    }
}
