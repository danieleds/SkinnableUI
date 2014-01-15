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
    public class ListView : PlayerControl
    {
        float curViewPosition = 0;
        ListViewRow curOverRow = null;
        float? paintedColumnsTotalWidth = null;
        List<ListViewRow> selectedRows = new List<ListViewRow>();

        public ListView(SemanticType c) : base(c)
        {
            this.Size = new SizeF(250, 100);
            items.CollectionChanged += items_CollectionChanged;
            columns.CollectionChanged += columns_CollectionChanged;
        }

        void columns_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.Invalidate();
        }

        void items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.Invalidate();
        }

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

        protected NinePatch backgroundHeaderBar9P;
        [DefaultValue(null)]
        public Bitmap BackgroundHeaderBar9P
        {
            get { return backgroundHeaderBar9P != null ? backgroundHeaderBar9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundHeaderBar9P = null;
                else
                    this.backgroundHeaderBar9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        protected NinePatch backgroundColumnHeader9P;
        [DefaultValue(null)]
        public Bitmap BackgroundColumnHeader9P
        {
            get { return backgroundColumnHeader9P != null ? backgroundColumnHeader9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundColumnHeader9P = null;
                else
                    this.backgroundColumnHeader9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        protected NinePatch backgroundRowOver9P;
        [DefaultValue(null)]
        public Bitmap BackgroundRowOver9P
        {
            get { return backgroundRowOver9P != null ? backgroundRowOver9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundRowOver9P = null;
                else
                    this.backgroundRowOver9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        protected NinePatch backgroundRowSelected9P;
        [DefaultValue(null)]
        public Bitmap BackgroundRowSelected9P
        {
            get { return backgroundRowSelected9P != null ? backgroundRowSelected9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundRowSelected9P = null;
                else
                    this.backgroundRowSelected9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        protected NinePatch backgroundRowSelectedOver9P;
        [DefaultValue(null)]
        public Bitmap BackgroundRowSelectedOver9P
        {
            get { return backgroundRowSelectedOver9P != null ? backgroundRowSelectedOver9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundRowSelectedOver9P = null;
                else
                    this.backgroundRowSelectedOver9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        protected NinePatch backgroundRowNormal9P;
        [DefaultValue(null)]
        public Bitmap BackgroundRowNormal9P
        {
            get { return backgroundRowNormal9P != null ? backgroundRowNormal9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundRowNormal9P = null;
                else
                    this.backgroundRowNormal9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        private Font headerFont = SystemFonts.DefaultFont;
        public Font HeaderFont
        {
            get { return headerFont; }
            set { headerFont = (value == null ? SystemFonts.DefaultFont : value); this.Invalidate(); }
        }

        private Color headerForeColor = Color.Black;
        [DefaultValue(typeof(Color), "0x000000")]
        public Color HeaderForeColor
        {
            get { return headerForeColor; }
            set { headerForeColor = value; this.Invalidate(); }
        }

        private static Bitmap tmpBmp = new Bitmap(1, 1);
        private float GetHeaderHeight()
        {
            SizeF result;
            using (var g = Graphics.FromImage(tmpBmp))
            {
                result = g.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz,_!|", this.headerFont);
            }

            return result.Height * 1.5f;
        }

        private float GetRowHeight()
        {
            return GetHeaderHeight();
        }

        private ObservableCollection<ListViewRow> items = new ObservableCollection<ListViewRow>();
        public ObservableCollection<ListViewRow> Items { get { return items; } }

        private ObservableCollection<ListViewColumn> columns = new ObservableCollection<ListViewColumn>();
        public ObservableCollection<ListViewColumn> Columns { get { return columns; } }

        public class ListViewRow
        {
            public ListViewRow() {
                this.Values = new System.Collections.ArrayList();
            }
            public System.Collections.ArrayList Values { get; set; }
        }

        public class ListViewColumn
        {
            public ListViewColumn()
            {
                this.Width = 100;
            }

            public string Title { get; set; }
            public float Width { get; set; }
        }

        protected override void OnPaint(System.Drawing.Graphics g)
        {
            //var contentBox = new RectangleF(0, 0, this.Size.Width, this.Size.Height);

            drawBackground(g);
            drawItems(g);
            drawScrollbar(g);
        }

        private void drawBackground(Graphics g)
        {
            var headerHeight = GetHeaderHeight();

            if(backgroundNormal9P != null)
                backgroundNormal9P.Paint(g, this.Size);
            else
                g.FillRectangle(Brushes.White, 0, 0, this.Size.Width - 1, this.Size.Height - 1);

            if(backgroundHeaderBar9P != null)
                backgroundHeaderBar9P.Paint(g, new RectangleF(0, 0, this.Size.Width - 1, headerHeight));
            else
                g.FillRectangle(SystemBrushes.ButtonFace, 1, 0, this.Size.Width - 2, headerHeight+1);

            float width_sum = 0;
            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                var s = g.Save();
                var strSize = g.MeasureString(columns[i].Title, this.headerFont);

                if (backgroundColumnHeader9P != null)
                {
                    var rect = new RectangleF(width_sum, 0, col.Width, headerHeight);
                    backgroundColumnHeader9P.Paint(g, rect);

                    RectangleF contentbox = backgroundColumnHeader9P.GetContentBox(rect.Size);
                    RectangleF textBox = new RectangleF(rect.X + contentbox.X, rect.Y + contentbox.Y, contentbox.Width, contentbox.Height);
                    g.SetClip(textBox, System.Drawing.Drawing2D.CombineMode.Intersect);
                    g.DrawString(col.Title, this.headerFont, new SolidBrush(this.headerForeColor), textBox.X, textBox.Y + textBox.Height / 2 - strSize.Height / 2 + 1);
                }
                else
                {
                    g.FillRectangle(SystemBrushes.ButtonFace, width_sum, 0, col.Width, headerHeight);
                    g.DrawRectangle(SystemPens.ButtonShadow, width_sum, 0, col.Width, headerHeight);

                    g.SetClip(new RectangleF(width_sum, 0, col.Width - 3, headerHeight), System.Drawing.Drawing2D.CombineMode.Intersect);
                    g.DrawString(col.Title, this.headerFont, new SolidBrush(this.headerForeColor), width_sum + 5, headerHeight / 2 - strSize.Height / 2 + 1);
                }

                g.Restore(s);

                width_sum += col.Width;
            }

            if (backgroundNormal9P == null)
                g.DrawRectangle(SystemPens.ButtonShadow, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
        }

        private void drawItems(Graphics g)
        {
            var headerHeight = GetHeaderHeight();
            var rowHeight = GetRowHeight();
            var viewHeight = this.Size.Height - headerHeight;
            float totWidth = this.columns.Sum(c => c.Width);

            var t = g.Save();
            g.SetClip(new RectangleF(1, headerHeight + 1, this.Size.Width - 2, viewHeight - 2), System.Drawing.Drawing2D.CombineMode.Intersect);
            g.TranslateTransform(0, -curViewPosition + headerHeight + 1);

            for (int i = 0; i < items.Count; i++)
            {
                ListViewRow item = items[i];

                bool over = curOverRow == item;
                bool selected = selectedRows.Contains(item);

                Brush bg = Brushes.Transparent;
                Pen border = Pens.Transparent;
                NinePatch patch = backgroundRowNormal9P;

                if (over && selected)
                {
                    bg = Brushes.LightBlue;
                    border = Pens.LightBlue;
                    patch = backgroundRowSelectedOver9P;
                }
                else if (over)
                {
                    bg = Brushes.AliceBlue;
                    border = Pens.LightBlue;
                    patch = backgroundRowOver9P;
                }
                else if (selected)
                {
                    bg = Brushes.LightBlue;
                    border = Pens.LightBlue;
                    patch = backgroundRowSelected9P;
                }

                if (patch != null)
                    patch.Paint(g, new RectangleF(1, 0 + rowHeight * i, totWidth - 1, rowHeight));
                else
                {
                    g.FillRectangle(bg, 1, 0 + rowHeight * i, totWidth - 1, rowHeight);
                    g.DrawRectangle(border, 1, 0 + rowHeight * i, totWidth - 1, rowHeight);
                }

                float width_sum = 0;
                for (int j = 0; j < columns.Count; j++)
                {
                    var col = columns[j];

                    var s = g.Save();
                    
                    var content = "";
                    if (j < item.Values.Count)
                    {
                        if (items[i].Values[j] != null)
                            content = items[i].Values[j].ToString();
                        else
                            content = "";
                    }
                    var strSize = g.MeasureString(content, this.Font);

                    if (patch != null)
                    {
                        RectangleF contentbox = patch.GetContentBox(new SizeF(col.Width, rowHeight));
                        RectangleF textBox = new RectangleF(width_sum + contentbox.X, (rowHeight * i) + contentbox.Y, contentbox.Width, contentbox.Height);
                        g.SetClip(textBox, System.Drawing.Drawing2D.CombineMode.Intersect);
                        g.DrawString(content, this.Font, new SolidBrush(this.ForeColor), textBox.X, textBox.Y + (textBox.Height / 2 - strSize.Height / 2 + 1));
                    }
                    else
                    {
                        g.SetClip(new RectangleF(width_sum, (rowHeight * i) + 1, col.Width - 3, rowHeight - 2), System.Drawing.Drawing2D.CombineMode.Intersect);
                        g.DrawString(content, this.Font, new SolidBrush(this.ForeColor), width_sum + 5, (rowHeight * i) + (rowHeight / 2 - strSize.Height / 2 + 1));
                    }

                    g.Restore(s);

                    width_sum += col.Width;
                }

                this.paintedColumnsTotalWidth = width_sum;
            }

            g.Restore(t);
        }

        private void drawScrollbar(Graphics g)
        {
            var headerHeight = GetHeaderHeight();
            var rowHeight = GetRowHeight();
            var viewHeight = this.Size.Height - headerHeight;
            var contentHeight = rowHeight * this.items.Count;
            if (contentHeight > viewHeight)
            {
                var barw = 3;
                var x = this.Size.Width - barw - 1;
                g.FillRectangle(Brushes.Gray, x, headerHeight + (curViewPosition * viewHeight / contentHeight), barw, (viewHeight * viewHeight) / contentHeight);
            }
        }

        public ListViewRow RowHitTest(float x, float y)
        {
            if (this.items.Count == 0)
                return null;

            if (!paintedColumnsTotalWidth.HasValue || paintedColumnsTotalWidth.Value == 0 || x > paintedColumnsTotalWidth.Value)
                return null;

            var rowHeight = GetRowHeight();

            if (rowHeight == 0)
                return null;

            y -= GetHeaderHeight();
            if (y < 0)
                return null;

            int pos = (int)((curViewPosition + y) / rowHeight);
            if (pos >= this.items.Count)
                return null;
            else
                return this.items[pos];
        }

        public override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            AdjustCurViewPosition();
        }

        public override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ListViewRow item = RowHitTest(e.X, e.Y);

                // FIXME Implementare multiselect con CTRL / SHIFT
                this.selectedRows.Clear();
                if (item != null)
                    this.selectedRows.Add(item);

                this.Invalidate();
            }
        }

        public override void OnMouseUp(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                //this.pressed = false;
                this.Invalidate();
            }
        }

        public override void OnMouseMove(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            this.curOverRow = RowHitTest(e.X, e.Y);
            this.Invalidate();
        }

        public override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            //this.enter = true;
            //this.Invalidate();
        }

        public override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            //this.enter = false;
            this.curOverRow = null;
            this.Invalidate();
        }

        public override void OnMouseWheel(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            
            curViewPosition -= e.Delta;
            AdjustCurViewPosition();

            this.Invalidate();
        }

        private void AdjustCurViewPosition()
        {
            if (curViewPosition < 0)
            {
                curViewPosition = 0;
                this.Invalidate();
            }
            else
            {
                var headerHeight = GetHeaderHeight();
                var rowHeight = GetRowHeight();

                var viewHeight = this.Size.Height - headerHeight;
                var contentHeight = rowHeight * this.items.Count;

                if (contentHeight < viewHeight)
                {
                    curViewPosition = 0;
                    this.Invalidate();
                }
                else
                {
                    if (curViewPosition > contentHeight - viewHeight)
                    {
                        curViewPosition = contentHeight - viewHeight;
                        this.Invalidate();
                    }
                }
            }
        }

        public override System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument document, Dictionary<string, System.IO.MemoryStream> resources)
        {
            var node = base.GetXmlElement(document, resources);

            var colsChild = document.CreateElement("Columns");
            foreach (var col in this.Columns)
            {
                var el = document.CreateElement("Column");
                el.SetAttribute("title", col.Title);
                el.SetAttribute("width", System.Xml.XmlConvert.ToString(col.Width));
                colsChild.AppendChild(el);
            }
            node.AppendChild(colsChild);

            var rowsChild = document.CreateElement("Items");
            foreach (var row in this.Items)
            {
                var el = document.CreateElement("Item");
                foreach(var value in row.Values) {
                    var v = document.CreateElement("Value");
                    // Con la conversione in stringa si perdono molte informazioni
                    // sugli oggetti che stiamo memorizzando, ma non è rilevante
                    // in quanto le stiamo salvando al fine di creare una skin (che
                    // non contiene logica ma è solamente descrittiva).
                    v.SetAttribute("value", value.ToString());
                    
                    el.AppendChild(v);
                }
                rowsChild.AppendChild(el);
            }
            node.AppendChild(rowsChild);

            SerializationHelper.SetNinePatch(this.backgroundNormal9P, "backgroundNormal9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundHeaderBar9P, "backgroundHeaderBar9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundColumnHeader9P, "backgroundColumnHeader9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundRowOver9P, "backgroundRowOver9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundRowSelected9P, "backgroundRowSelected9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundRowSelected9P, "backgroundRowSelected9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundRowNormal9P, "backgroundRowNormal9P", resources, node);

            node.SetAttribute("headerFont", new FontConverter().ConvertToInvariantString(this.headerFont));
            node.SetAttribute("headerForeColor", string.Format("#{0:x6}", this.headerForeColor.ToArgb()));

            return node;
        }

        public override void FromXmlElement(System.Xml.XmlElement element, Dictionary<string, System.IO.MemoryStream> resources)
        {
            base.FromXmlElement(element, resources);

            foreach (System.Xml.XmlElement child in element.ChildNodes)
            {
                if (child.Name == "Columns")
                {
                    foreach (System.Xml.XmlElement col in child.ChildNodes)
                    {
                        var c = new ListViewColumn();
                        c.Title = col.GetAttribute("title");
                        c.Width = (float)System.Xml.XmlConvert.ToDecimal(col.GetAttribute("width"));
                        this.Columns.Add(c);
                    }
                }
                else if (child.Name == "Items")
                {
                    foreach (System.Xml.XmlElement item in child.ChildNodes)
                    {
                        var r = new ListViewRow();
                        foreach (System.Xml.XmlElement value in item.ChildNodes)
                        {
                            r.Values.Add(value.GetAttribute("value"));
                        }
                        this.Items.Add(r);
                    }
                }
            }

            SerializationHelper.LoadBitmapFromResources(element, "backgroundNormal9P", resources, s => this.BackgroundNormal9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundHeaderBar9P", resources, s => this.BackgroundHeaderBar9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundColumnHeader9P", resources, s => this.BackgroundColumnHeader9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundRowOver9P", resources, s => this.BackgroundRowOver9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundRowSelected9P", resources, s => this.BackgroundRowSelected9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundRowSelected9P", resources, s => this.BackgroundRowSelected9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundRowNormal9P", resources, s => this.BackgroundRowNormal9P = s);

            SerializationHelper.LoadFont(element, "headerFont", s => this.HeaderFont = s);
            SerializationHelper.LoadColor(element, "headerForeColor", s => this.HeaderForeColor = s);
        }

    }
}
