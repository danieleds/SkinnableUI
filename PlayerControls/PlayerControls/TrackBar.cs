using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;

namespace UnusefulPlayer.PlayerControls
{
    public class TrackBar : PlayerControl
    {

        public TrackBar(SemanticType c) : base(c)
        {
            this.Size = new SizeF(120, 12);
        }
        private bool pressed;
        private bool over;

        public delegate void ValueChangedEventHandler(object sender, EventArgs e);
        public event ValueChangedEventHandler ValueChanged;

        public delegate void UserChangedValueEventHandler(object sender, EventArgs e);
        public event UserChangedValueEventHandler UserChangedValue;

        private const float DEFAULT_INDICATOR_WIDTH = 10f;

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

        private NinePatch indicatorNormal9P;
        [DefaultValue(null)]
        public Bitmap IndicatorNormal9P
        {
            get { return indicatorNormal9P != null ? indicatorNormal9P.Image : null; }
            set
            {
                if (value == null)
                    this.indicatorNormal9P = null;
                else
                    this.indicatorNormal9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        private NinePatch indicatorOver9P;
        [DefaultValue(null)]
        public Bitmap IndicatorOver9P
        {
            get { return indicatorOver9P != null ? indicatorOver9P.Image : null; }
            set
            {
                if (value == null)
                    this.indicatorOver9P = null;
                else
                    this.indicatorOver9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        private NinePatch indicatorPressed9P;
        [DefaultValue(null)]
        public Bitmap IndicatorPressed9P
        {
            get { return indicatorPressed9P != null ? indicatorPressed9P.Image : null; }
            set
            {
                if (value == null)
                    this.indicatorPressed9P = null;
                else
                    this.indicatorPressed9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        private NinePatch indicatorBar9P;
        [DefaultValue(null)]
        public Bitmap IndicatorBar9P
        {
            get { return indicatorBar9P != null ? indicatorBar9P.Image : null; }
            set
            {
                if (value == null)
                    this.indicatorBar9P = null;
                else
                    this.indicatorBar9P = new NinePatch(value);
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


            float cx_pos = (float)this.value * contentBox.Width / (float)(this.maximum - this.minimum);
            var indicator_bar_box = new RectangleF(contentBox.X, contentBox.Y, cx_pos, contentBox.Height);

            // Disegno barra indicatore
            if (indicatorBar9P != null)
            {
                indicatorBar9P.Paint(g, indicator_bar_box);
            }

            // Disegno indicatore
            float indicator_width = indicatorNormal9P != null ? indicatorNormal9P.Image.Width-2 : DEFAULT_INDICATOR_WIDTH;
            var indicator_box = new RectangleF(contentBox.X + cx_pos - (indicator_width / 2f), contentBox.Y, indicator_width-1, contentBox.Height);
            if (pressed)
            {
                if (indicatorPressed9P != null)
                {
                    indicatorPressed9P.Paint(g, indicator_box);
                }
                else
                    drawDefaultIndicator(g, indicator_box);
            }
            else if(over)
            {
                if (indicatorOver9P != null)
                {
                    indicatorOver9P.Paint(g, indicator_box);
                }
                else
                    drawDefaultIndicator(g, indicator_box);
            }
            else
            {
                if (indicatorNormal9P != null)
                {
                    indicatorNormal9P.Paint(g, indicator_box);
                }
                else
                    drawDefaultIndicator(g, indicator_box);
            }

            //g.DrawRectangle(Pens.Red, contentBox.X, contentBox.Y, contentBox.Width, contentBox.Height);
            //g.DrawRectangle(Pens.Blue, indicator_bar_box.X, indicator_bar_box.Y, indicator_bar_box.Width, indicator_bar_box.Height);
            //g.DrawRectangle(Pens.Blue, indicator_box.X, indicator_box.Y, indicator_box.Width-1, indicator_box.Height);
            //g.DrawLine(Pens.Red, contentBox.X + cx_pos, 0, contentBox.X + cx_pos, this.Size.Height);
        }

        private void drawDefaultBackground(Graphics g)
        {
            g.FillRectangle(SystemBrushes.Control, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
            g.DrawRectangle(SystemPens.ControlDark, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
        }

        private void drawDefaultIndicator(Graphics g, RectangleF indicatorBox)
        {
            const float w = 6;
            g.FillRectangle(SystemBrushes.ControlDarkDark, indicatorBox.X + indicatorBox.Width - (DEFAULT_INDICATOR_WIDTH / 2) - (w / 2), indicatorBox.Y, w, indicatorBox.Height);
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

        public override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.over = true;
            this.Invalidate();
        }

        public override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.over = false;
            this.Invalidate();
        }

        public override System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument document, Dictionary<string, System.IO.MemoryStream> resources)
        {
            var node = base.GetXmlElement(document, resources);
            var inv = System.Globalization.NumberFormatInfo.InvariantInfo;
            node.SetAttribute("minimum", this.Minimum.ToString(inv));
            node.SetAttribute("maximum", this.Maximum.ToString(inv));
            node.SetAttribute("value", this.Value.ToString(inv));

            SerializationHelper.SetNinePatch(this.backgroundNormal9P, "backgroundNormal9P", resources, node);
            SerializationHelper.SetNinePatch(this.indicatorNormal9P, "indicatorNormal9P", resources, node);
            SerializationHelper.SetNinePatch(this.indicatorOver9P, "indicatorOver9P", resources, node);
            SerializationHelper.SetNinePatch(this.indicatorPressed9P, "indicatorPressed9P", resources, node);
            SerializationHelper.SetNinePatch(this.indicatorBar9P, "indicatorBar9P", resources, node);

            return node;
        }

        public override void FromXmlElement(System.Xml.XmlElement element, Dictionary<string, System.IO.MemoryStream> resources)
        {
            base.FromXmlElement(element, resources);
            SerializationHelper.LoadInteger(element, "minimum", s => this.Minimum = s);
            SerializationHelper.LoadInteger(element, "maximum", s => this.Maximum = s);
            SerializationHelper.LoadInteger(element, "value", s => this.Value = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundNormal9P", resources, s => this.BackgroundNormal9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "indicatorNormal9P", resources, s => this.IndicatorNormal9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "indicatorOver9P", resources, s => this.IndicatorOver9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "indicatorPressed9P", resources, s => this.IndicatorPressed9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "indicatorBar9P", resources, s => this.IndicatorBar9P = s);
        }

    }
}
