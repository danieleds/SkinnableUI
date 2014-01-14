﻿using System;
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

        private static Bitmap tmpBmp = new Bitmap(1, 1);
        private float GetHeaderHeight()
        {
            SizeF result;
            using (var g = Graphics.FromImage(tmpBmp))
            {
                result = g.MeasureString("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz,_!|", this.Font);
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
            var contentBox = new RectangleF(0, 0, this.Size.Width, this.Size.Height);

            if (backgroundNormal9P != null)
            {
                contentBox = backgroundNormal9P.GetContentBox(this.Size);
                backgroundNormal9P.Paint(g, this.Size);
            }
            else
                drawDefaultBackground(g);

            drawDefaultItems(g);
            drawScrollbar(g);
        }

        private void drawDefaultBackground(Graphics g)
        {
            var headerHeight = GetHeaderHeight();
            g.FillRectangle(Brushes.White, 0, 0, this.Size.Width - 1, this.Size.Height - 1);

            g.FillRectangle(SystemBrushes.ButtonFace, 1, 0, this.Size.Width - 2, headerHeight+1);
            float width_sum = 0;
            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                g.FillRectangle(SystemBrushes.ButtonFace, width_sum, 0, col.Width, headerHeight);
                g.DrawRectangle(SystemPens.ButtonShadow, width_sum, 0, col.Width, headerHeight);

                var s = g.Save();
                g.SetClip(new RectangleF(width_sum, 0, col.Width - 3, headerHeight), System.Drawing.Drawing2D.CombineMode.Intersect);
                var strSize = g.MeasureString(columns[i].Title, this.Font);
                g.DrawString(col.Title, this.Font, Brushes.Black, width_sum + 5, headerHeight / 2 - strSize.Height / 2 + 1);
                g.Restore(s);

                width_sum += col.Width;
            }

            g.DrawRectangle(SystemPens.ButtonShadow, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
        }

        private void drawDefaultItems(Graphics g)
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

                if (over && selected)
                {
                    bg = Brushes.LightBlue;
                    border = Pens.LightBlue;
                }
                else if (over)
                {
                    bg = Brushes.AliceBlue;
                    border = Pens.LightBlue;
                }
                else if (selected)
                {
                    bg = Brushes.LightBlue;
                    border = Pens.LightBlue;
                }

                g.FillRectangle(bg, 1, 0 + rowHeight * i, totWidth - 1, rowHeight);
                g.DrawRectangle(border, 1, 0 + rowHeight * i, totWidth - 1, rowHeight);

                float width_sum = 0;
                for (int j = 0; j < columns.Count; j++)
                {
                    var col = columns[j];

                    var s = g.Save();
                    g.SetClip(new RectangleF(width_sum, (rowHeight * i) + 1, col.Width - 3, rowHeight - 2), System.Drawing.Drawing2D.CombineMode.Intersect);
                    var content = "";
                    if (j < item.Values.Count)
                    {
                        if (items[i].Values[j] != null)
                            content = items[i].Values[j].ToString();
                        else
                            content = "";
                    }
                    var strSize = g.MeasureString(content, this.Font);
                    g.DrawString(content, this.Font, Brushes.Black, width_sum + 5, (rowHeight * i) + (rowHeight / 2 - strSize.Height / 2 + 1));
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
        }

    }
}