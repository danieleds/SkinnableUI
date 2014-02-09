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
    public class PictureBox : SkinnableControl
    {

        public PictureBox(SemanticType c) : base(c)
        {
            this.Size = new SizeF(50, 50);
            this.TabStop = false;
        }

        private Image image;
        [DefaultValue(null), Category("Appearance")]
        public Image Image { get { return image; } set { image = value; this.Invalidate(); } }

        private Image defaultImage;
        [DefaultValue(null), Category("Appearance")]
        public Image DefaultImage { get { return defaultImage; } set { defaultImage = value; this.Invalidate(); } }

        protected override void OnPaint(System.Drawing.Graphics g)
        {
            if (image != null)
            {
                g.DrawImage(image, new RectangleF(0, 0, this.Size.Width, this.Size.Height));
            }
            else if(defaultImage != null)
            {
                g.DrawImage(defaultImage, new RectangleF(0, 0, this.Size.Width, this.Size.Height));
            }
            
            if (this.ParentView != null && this.ParentView.DesignSkinMode)
            {
                Pen p = new Pen(Color.Gray);
                p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                g.DrawRectangle(p, 0, 0, this.Size.Width-1, this.Size.Height-1);
            }
            
        }

        public override System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument document, Dictionary<string, System.IO.MemoryStream> resources)
        {
            var node = base.GetXmlElement(document, resources);
            SerializationHelper.SetImage(this.image, "image", resources, node);
            SerializationHelper.SetImage(this.defaultImage, "defaultImage", resources, node);

            return node;
        }

        public override void FromXmlElement(System.Xml.XmlElement element, Dictionary<string, System.IO.MemoryStream> resources)
        {
            base.FromXmlElement(element, resources);
            SerializationHelper.LoadBitmapFromResources(element, "image", resources, s => this.Image = s);
            SerializationHelper.LoadBitmapFromResources(element, "defaultImage", resources, s => this.DefaultImage = s);
        }

    }
}
