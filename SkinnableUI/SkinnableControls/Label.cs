/*
 *  Copyright 2014 Daniele Di Sarli
 *
 *  This file is part of SkinnableUI.
 *
 *  SkinnableUI is free software: you can redistribute it and/or modify
 *  it under the terms of the Lesser GNU General Public License as
 *  published by the Free Software Foundation, either version 3 of the
 *  License, or (at your option) any later version.
 *
 *  SkinnableUI is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  Lesser GNU General Public License for more details.
 *
 *  You should have received a copy of the Lesser GNU General Public License
 *  along with SkinnableUI. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;

namespace SkinnableUI.SkinnableControls
{
    public class Label : SkinnableControl
    {

        public Label(SemanticType c) : base(c)
        {
            this.Size = new SizeF(75, 20);
            this.TabStop = false;
        }

        private string text = "";
        [Category("Appearance")]
        public string Text { get { return text; } set { text = value; this.Invalidate(); } }

        private ContentAlignment textAlign = ContentAlignment.MiddleLeft;
        [Category("Appearance")]
        public ContentAlignment TextAlign
        {
            get { return textAlign; }
            set { textAlign = value; this.Invalidate(); }
        }

        protected override void OnPaint(System.Drawing.Graphics g)
        {
            var strSize = g.MeasureString(this.Text, this.Font);

            float x = 0;
            if (this.textAlign == ContentAlignment.BottomCenter || this.textAlign == ContentAlignment.MiddleCenter || this.textAlign == ContentAlignment.TopCenter)
                x = this.Size.Width / 2 - strSize.Width / 2;
            else if (this.textAlign == ContentAlignment.BottomRight || this.textAlign == ContentAlignment.MiddleRight || this.textAlign == ContentAlignment.TopRight)
                x = this.Size.Width - strSize.Width;

            float y = 0;
            if (this.textAlign == ContentAlignment.MiddleCenter || this.textAlign == ContentAlignment.MiddleLeft || this.textAlign == ContentAlignment.MiddleRight)
                y = this.Size.Height / 2 - strSize.Height / 2;
            else if (this.textAlign == ContentAlignment.BottomCenter || this.textAlign == ContentAlignment.BottomLeft || this.textAlign == ContentAlignment.BottomRight)
                y = this.Size.Height - strSize.Height;

            g.DrawString(this.Text, this.Font, new SolidBrush(this.ForeColor), x, y);
        }

        public override System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument document, Dictionary<string, System.IO.MemoryStream> resources)
        {
            var node = base.GetXmlElement(document, resources);
            node.SetAttribute("text", this.Text);
            node.SetAttribute("textAlign", this.TextAlign.ToString());

            return node;
        }

        public override void FromXmlElement(System.Xml.XmlElement element, Dictionary<string, System.IO.MemoryStream> resources)
        {
            base.FromXmlElement(element, resources);
            SerializationHelper.LoadString(element, "text", s => this.Text = s);
            SerializationHelper.LoadEnum<ContentAlignment>(element, "textAlign", s => this.TextAlign = s);
        }

    }
}
