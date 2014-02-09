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
    public class Button : SkinnableControl
    {

        public Button(SemanticType c) : base(c)
        {
            this.Size = new SizeF(75, 30);
        }

        private bool pressed;
        private bool over;
        Animation anim;
        NinePatch lastDrawnPatch = null;

        protected NinePatch backgroundNormal9P;
        [DefaultValue(null), Category("Appearance")]
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

        protected NinePatch backgroundOver9P;
        [DefaultValue(null), Category("Appearance")]
        public Bitmap BackgroundOver9P
        {
            get { return backgroundOver9P != null ? backgroundOver9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundOver9P = null;
                else
                    this.backgroundOver9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        protected NinePatch backgroundPressed9P;
        [DefaultValue(null), Category("Appearance")]
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

        private string text;
        [Category("Appearance")]
        public string Text { get { return text; } set { text = value; this.Invalidate(); } }

        /// <summary>
        /// True per attivare le animazioni su mouse enter / mouse leave.
        /// </summary>
        [DefaultValue(false), Category("Appearance")]
        public bool EnterLeave9PAnimation { get; set; }

        protected override void OnPaint(System.Drawing.Graphics g)
        {
            var strSize = g.MeasureString(this.Text, this.Font);
            var contentBox = new RectangleF(0, 0, this.Size.Width, this.Size.Height);

            var patch =
                this.pressed ? backgroundPressed9P
                : this.over ? backgroundOver9P
                : backgroundNormal9P;

            if (patch != null)
            {
                patch = anim != null && anim.IsRunning() ? anim.GetCurrentFrame() : patch;
                lastDrawnPatch = patch;
                contentBox = patch.GetContentBox(this.Size);
                patch.Paint(g, this.Size);
            }
            else
                drawDefaultButton(g);

            g.SetClip(contentBox, System.Drawing.Drawing2D.CombineMode.Intersect);
            g.DrawString(this.Text, this.Font, new SolidBrush(this.ForeColor), contentBox.Width / 2 - strSize.Width / 2 + contentBox.Left, contentBox.Height / 2 - strSize.Height / 2 + contentBox.Top);
        }

        private void drawDefaultButton(Graphics g)
        {
            if (this.pressed)
            {
                g.FillRectangle(SystemBrushes.ButtonShadow, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
                g.DrawRectangle(SystemPens.ButtonFace, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
            }
            else
            {
                g.FillRectangle(SystemBrushes.ButtonFace, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
                g.DrawRectangle(SystemPens.ButtonShadow, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
            }
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
                if (this.backgroundOver9P != null && this.lastDrawnPatch != null)
                    this.GetAnimator().ContinueAnimationB(ref anim, lastDrawnPatch, this.backgroundOver9P, 50, 300, this.Invalidate);
            }

            this.Invalidate();
        }

        public override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.over = false;

            if (EnterLeave9PAnimation)
            {
                if (this.backgroundNormal9P != null && this.lastDrawnPatch != null)
                    this.GetAnimator().ContinueAnimationB(ref anim, lastDrawnPatch, this.backgroundNormal9P, 50, 300, this.Invalidate);
            }

            this.Invalidate();
        }

        public override System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument document, Dictionary<string, System.IO.MemoryStream> resources)
        {
            var node = base.GetXmlElement(document, resources);
            node.SetAttribute("text", this.Text);
            node.SetAttribute("enterLeave9PAnimation", System.Xml.XmlConvert.ToString(this.EnterLeave9PAnimation));

            SerializationHelper.SetNinePatch(this.backgroundNormal9P, "backgroundNormal9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundOver9P, "backgroundOver9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundPressed9P, "backgroundPressed9P", resources, node);

            return node;
        }

        public override void FromXmlElement(System.Xml.XmlElement element, Dictionary<string, System.IO.MemoryStream> resources)
        {
            base.FromXmlElement(element, resources);
            SerializationHelper.LoadString(element, "text", s => this.Text = s);
            SerializationHelper.LoadBoolean(element, "enterLeave9PAnimation", s => this.EnterLeave9PAnimation = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundNormal9P", resources, s => this.BackgroundNormal9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundOver9P", resources, s => this.BackgroundOver9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundPressed9P", resources, s => this.BackgroundPressed9P = s);
        }

    }
}
