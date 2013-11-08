using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;

namespace UnusefulPlayer.PlayerControls
{
    class TrackBar : PlayerControl
    {

        public TrackBar(SemanticType c) : base(c)
        {
            this.Size = new SizeF(120, 12);
        }
        private bool pressed;
        private bool hover;

        private const int INDICATOR_MAX_WIDTH = 10;

        public delegate void ValueChangedEventHandler(object sender, EventArgs e);
        public event ValueChangedEventHandler ValueChanged;

        public delegate void UserChangedValueEventHandler(object sender, EventArgs e);
        public event UserChangedValueEventHandler UserChangedValue;

        private NinePatch backgroundNormal9P;
        [DefaultValue(null)]
        public Bitmap BackgroundNormal9P
        {
            get { return backgroundNormal9P != null ? backgroundNormal9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundNormal9P = null;
                else
                    this.backgroundNormal9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        private NinePatch backgroundIndicatorNormal9P;
        [DefaultValue(null)]
        public Bitmap BackgroundIndicatorNormal9P
        {
            get { return backgroundIndicatorNormal9P != null ? backgroundIndicatorNormal9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundIndicatorNormal9P = null;
                else
                    this.backgroundIndicatorNormal9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        private NinePatch backgroundIndicatorHover9P;
        [DefaultValue(null)]
        public Bitmap BackgroundIndicatorHover9P
        {
            get { return backgroundIndicatorHover9P != null ? backgroundIndicatorHover9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundIndicatorHover9P = null;
                else
                    this.backgroundIndicatorHover9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        private NinePatch backgroundIndicatorPressed9P;
        [DefaultValue(null)]
        public Bitmap BackgroundIndicatorPressed9P
        {
            get { return backgroundIndicatorPressed9P != null ? backgroundIndicatorPressed9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundIndicatorPressed9P = null;
                else
                    this.backgroundIndicatorPressed9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        private int minimum = 0;
        public int Minimum
        {
            get { return minimum; }
            set
            {
                if (maximum < value)
                    throw new ArgumentOutOfRangeException("Minimum should be less than Maximum.");
                minimum = value;
                this.Value = Math.Max(this.value, minimum);
                this.Invalidate();
            }
        }

        private int maximum = 100;
        public int Maximum
        {
            get { return maximum; }
            set
            {
                if (value < minimum)
                    throw new ArgumentOutOfRangeException("Maximum should be more than Minimum.");
                maximum = value;
                this.Value = Math.Min(this.value, maximum);
                this.Invalidate();
            }
        }

        private int value = 0;
        public int Value
        {
            get { return value; }
            set
            {
                if (value > maximum || value < minimum)
                    throw new ArgumentOutOfRangeException("Value should be between Maximum and Minimum.");
                this.value = value;
                if (ValueChanged != null) ValueChanged(this, new EventArgs());
                this.Invalidate();
            }
        }

        protected override void OnPaint(System.Drawing.Graphics g)
        {
            var contentBox = new RectangleF(0, 0, this.Size.Width, this.Size.Height);

            // Disegno sfondo
            if (backgroundNormal9P != null)
            {
                contentBox = backgroundNormal9P.GetContentBox(this.Size);
                backgroundNormal9P.Paint(g, this.Size);
            }
            else
                drawDefaultBackground(g);

            // Disegno indicatore
            var cx_pos = this.value * contentBox.Width / (this.maximum - this.minimum);
            var indicator_box = new RectangleF(contentBox.X, contentBox.Y, cx_pos + ((float)INDICATOR_MAX_WIDTH / 2), contentBox.Height);
            if (pressed)
            {
                if (backgroundIndicatorPressed9P != null)
                {
                    backgroundIndicatorPressed9P.Paint(g, indicator_box);
                }
                else
                    drawDefaultIndicator(g, indicator_box);
            }
            else if(hover)
            {
                if (backgroundIndicatorHover9P != null)
                {
                    backgroundIndicatorHover9P.Paint(g, indicator_box);
                }
                else
                    drawDefaultIndicator(g, indicator_box);
            }
            else
            {
                if (backgroundIndicatorNormal9P != null)
                {
                    backgroundIndicatorNormal9P.Paint(g, indicator_box);
                }
                else
                    drawDefaultIndicator(g, indicator_box);
            }
        }

        private void drawDefaultBackground(Graphics g)
        {
            g.FillRectangle(SystemBrushes.Control, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
            g.DrawRectangle(SystemPens.ControlDark, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
        }

        private void drawDefaultIndicator(Graphics g, RectangleF indicatorBox)
        {
            const float w = 6;
            g.FillRectangle(SystemBrushes.ControlDarkDark, indicatorBox.X + indicatorBox.Width - ((float)INDICATOR_MAX_WIDTH / 2) - (w / 2), indicatorBox.Y, w, indicatorBox.Height);
        }

        public override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.pressed = true;
                setValueFromMouseLocation(e.Location);
                this.Invalidate();
            }
        }

        private void setValueFromMouseLocation(Point mouseLocation)
        {
            var contentBox = backgroundNormal9P == null
                ? new RectangleF(0, 0, this.Size.Width, this.Size.Height)
                : backgroundNormal9P.GetContentBox(this.Size);

            // (mouseLocation.X - contentBox.X) : contentBox.Width = x : (maximum - minimum)
            var fvalue = (mouseLocation.X - contentBox.X) * (this.maximum - this.minimum) / contentBox.Width;
            var intvalue = (int)Math.Round(fvalue, 0, MidpointRounding.ToEven);
            if (intvalue > maximum) intvalue = maximum;
            else if (intvalue < minimum) intvalue = minimum;
            this.Value = intvalue;
            if (UserChangedValue != null) UserChangedValue(this, new EventArgs());
        }

        public override void OnMouseMove(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (this.pressed)
            {
                setValueFromMouseLocation(e.Location);
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

        public override void OnMouseHover(EventArgs e)
        {
            base.OnMouseHover(e);
            this.hover = true;
            this.Invalidate();
        }

        public override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.hover = false;
            this.Invalidate();
        }

        // FIXME Si rischia di inserire inutilmente le solite risorse duplicate, ad esempio se due bottoni usano la stessa bgImage...
        public override System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument document, Dictionary<string, System.IO.MemoryStream> resources)
        {
            var node = base.GetXmlElement(document, resources);
            var inv = System.Globalization.NumberFormatInfo.InvariantInfo;
            node.SetAttribute("minimum", this.Minimum.ToString(inv));
            node.SetAttribute("maximum", this.Maximum.ToString(inv));
            node.SetAttribute("value", this.Value.ToString(inv));

            if (this.backgroundNormal9P != null)
            {
                System.IO.MemoryStream m = new System.IO.MemoryStream();
                this.backgroundNormal9P.Image.Save(m, System.Drawing.Imaging.ImageFormat.Png);
                String filename = SerializationHelper.PKG_RES_PREFIX + resources.Count + ".png";
                resources.Add(filename, m);
                node.SetAttribute("backgroundNormal9P", filename);
            }

            if (this.backgroundIndicatorNormal9P != null)
            {
                System.IO.MemoryStream m = new System.IO.MemoryStream();
                this.backgroundIndicatorNormal9P.Image.Save(m, System.Drawing.Imaging.ImageFormat.Png);
                String filename = SerializationHelper.PKG_RES_PREFIX + resources.Count + ".png";
                resources.Add(filename, m);
                node.SetAttribute("backgroundIndicatorNormal9P", filename);
            }

            if (this.backgroundIndicatorHover9P != null)
            {
                System.IO.MemoryStream m = new System.IO.MemoryStream();
                this.backgroundIndicatorHover9P.Image.Save(m, System.Drawing.Imaging.ImageFormat.Png);
                String filename = SerializationHelper.PKG_RES_PREFIX + resources.Count + ".png";
                resources.Add(filename, m);
                node.SetAttribute("backgroundIndicatorHover9P", filename);
            }

            if (this.backgroundIndicatorPressed9P != null)
            {
                var m = new System.IO.MemoryStream();
                this.backgroundIndicatorPressed9P.Image.Save(m, System.Drawing.Imaging.ImageFormat.Png);
                String filename = SerializationHelper.PKG_RES_PREFIX + resources.Count + ".png";
                resources.Add(filename, m);
                node.SetAttribute("backgroundIndicatorPressed9P", filename);
            }

            return node;
        }

        public override void FromXmlElement(System.Xml.XmlElement element, Dictionary<string, System.IO.MemoryStream> resources)
        {
            base.FromXmlElement(element, resources);
            SerializationHelper.LoadInteger(element, "minimum", s => this.Minimum = s);
            SerializationHelper.LoadInteger(element, "maximum", s => this.Maximum = s);
            SerializationHelper.LoadInteger(element, "value", s => this.Value = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundNormal9P", resources, s => this.BackgroundNormal9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundIndicatorNormal9P", resources, s => this.BackgroundIndicatorNormal9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundIndicatorHover9P", resources, s => this.BackgroundIndicatorHover9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundIndicatorPressed9P", resources, s => this.BackgroundIndicatorPressed9P = s);
        }

    }
}
