using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace UnusefulPlayer.PlayerControls
{
    class ListView : PlayerControl
    {

        public ListView(SemanticType c) : base(c)
        {
            this.Size = new SizeF(75, 30);
            items.CollectionChanged += items_CollectionChanged;
        }

        void items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.Invalidate();
        }

        private bool pressed;
        private bool hover;

        protected NinePatch backgroundNormal9P;
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

        protected NinePatch backgroundHover9P;
        [DefaultValue(null)]
        public Bitmap BackgroundHover9P
        {
            get { return backgroundHover9P != null ? backgroundHover9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundHover9P = null;
                else
                    this.backgroundHover9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        protected NinePatch backgroundPressed9P;
        [DefaultValue(null)]
        public Bitmap BackgroundPressed9P
        {
            get { return backgroundPressed9P != null ? backgroundPressed9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundPressed9P = null;
                else
                    this.backgroundPressed9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        private ObservableCollection<IListViewItem> items = new ObservableCollection<IListViewItem>();

        public ObservableCollection<IListViewItem> Items { get { return items; } }

        public static interface IListViewItem
        {
            /// <summary>
            /// Restituisce il valore su un certo campo.
            /// </summary>
            /// <param name="field">Id del campo.</param>
            /// <returns></returns>
            public string getValue(int field);
        }

        protected override void OnPaint(System.Drawing.Graphics g)
        {
            var strSize = g.MeasureString(this.Text, this.Font);
            var contentBox = new RectangleF(0, 0, this.Size.Width, this.Size.Height);

            if (pressed)
            {
                if (backgroundPressed9P != null)
                {
                    contentBox = backgroundPressed9P.GetContentBox(this.Size);
                    backgroundPressed9P.Paint(g, this.Size);
                }
                else
                    drawDefaultButton(g);
            }
            else if(hover)
            {
                if (backgroundHover9P != null)
                {
                    contentBox = backgroundHover9P.GetContentBox(this.Size);
                    backgroundHover9P.Paint(g, this.Size);
                }
                else
                    drawDefaultButton(g);
            }
            else
            {
                if (backgroundNormal9P != null)
                {
                    contentBox = backgroundNormal9P.GetContentBox(this.Size);
                    backgroundNormal9P.Paint(g, this.Size);
                }
                else
                    drawDefaultButton(g);
            }
        }

        private void drawDefaultButton(Graphics g)
        {
            g.FillRectangle(SystemBrushes.ButtonFace, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
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
            node.SetAttribute("text", this.Text);

            SerializationHelper.SetNinePatch(this.backgroundNormal9P, "backgroundNormal9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundHover9P, "backgroundHover9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundPressed9P, "backgroundPressed9P", resources, node);

            return node;
        }

        public override void FromXmlElement(System.Xml.XmlElement element, Dictionary<string, System.IO.MemoryStream> resources)
        {
            base.FromXmlElement(element, resources);
            SerializationHelper.LoadString(element, "text", s => this.Text = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundNormal9P", resources, s => this.BackgroundNormal9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundHover9P", resources, s => this.BackgroundHover9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundPressed9P", resources, s => this.BackgroundPressed9P = s);
        }

    }
}
