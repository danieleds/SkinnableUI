﻿/*
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
using System.Windows.Forms;
using SkinnableUI.SkinnableControls;
using System.Drawing;

namespace SkinnableUI
{
    /* Contenitore di oggetti PlayerControl */
    public class SkinnableView : UserControl
    {
        /// <summary>
        /// L'ordine degli elementi in questa lista rappresenta il loro z-order.
        /// Gli elementi in testa sono sopra rispetto a quelli in coda.
        /// </summary>
        protected Container containerControl;
        public Animator animator = new Animator();

        [Obsolete("Questa proprietà ha un comportamento nullo. Usare ContainerControl per impostare un contenitore di primo livello.")]
        new public ControlCollection Controls { get; private set; }

        public bool BlockInputEvents { get; set; }

        /// <summary>
        /// TRUE se containerControl deve essere ancorato al playerView (una sorta di Dock=Fill).
        /// Il valore false è usato per esempio nel designer.
        /// </summary>
        public bool DockContainerControl { get; set; }

        public SkinnableView()
        {
            BlockInputEvents = false;
            DockContainerControl = true;

            var cc = new Container(SkinnableControl.SemanticType.Container);
            cc.Top = 0;
            cc.Left = 0;
            cc.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top;
            this.ContainerControl = cc;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        public bool DesignSkinMode { get { return this is SkinnableViewDesigner; } }

        public Container ContainerControl
        {
            get { return this.containerControl; }
            set
            {
                this.containerControl = value;
                if (this.containerControl != null)
                {
                    containerControl.ParentView = this;
                    containerControl.Resize += containerControl_Resize;
                }
            }
        }

        void containerControl_Resize(object sender, EventArgs e)
        {
            if (DockContainerControl)
            {
                this.Size = new Size((int)containerControl.Size.Width, (int)containerControl.Size.Height);
            }
        }

        public Skin GetSkin()
        {
            var resources = new Dictionary<string, System.IO.MemoryStream>();

            var doc = new System.Xml.XmlDocument();
            var declar = doc.CreateXmlDeclaration("1.0", System.Text.Encoding.UTF8.WebName, null);
            doc.AppendChild(declar);

            var root = doc.CreateElement("Theme");
            root.SetAttribute("version", "1.0");
            root.AppendChild(this.ContainerControl.GetXmlElement(doc, resources));

            doc.AppendChild(root);

            return new Skin(doc, resources);
        }

        public void SaveSkin(string fileName)
        {
            SerializationHelper.SaveSkinPackage(fileName, this.GetSkin());
        }

        public virtual void SetSkin(Skin skin)
        {
            var doc = skin.Xml;
            var resources = skin.Resources;

            var theme = doc.GetElementsByTagName("Theme")[0];

            this.ContainerControl = new Container(SkinnableControl.SemanticType.Container);
            this.ContainerControl.ParentView = this;
            this.ContainerControl.FromXmlElement((System.Xml.XmlElement)theme.ChildNodes[0], resources);

            this.ContainerControl.Location = new PointF();
            this.Size = new Size((int)this.containerControl.Size.Width, (int)this.containerControl.Size.Height);
        }

        public void LoadSkin(string fileName)
        {
            this.SetSkin(SerializationHelper.OpenSkinPackage(fileName));
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    return true;
                case Keys.Shift | Keys.Right:
                case Keys.Shift | Keys.Left:
                case Keys.Shift | Keys.Up:
                case Keys.Shift | Keys.Down:
                    return true;
                case Keys.Tab:
                case Keys.Shift | Keys.Tab:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!BlockInputEvents)
            {
                if (e.KeyCode == Keys.Tab && e.Modifiers == Keys.None)
                {
                    if (this.containerControl != null)
                        if (this.containerControl.DoTab(true, true) == false)
                            this.containerControl.DoTab(true, true);
                }
                else if (e.KeyCode == Keys.Tab && e.Modifiers == Keys.Shift)
                {
                    if (this.containerControl != null)
                        if (this.containerControl.DoTab(false, true) == false)
                            this.containerControl.DoTab(false, true);
                }
                else
                {
                    if (this.containerControl != null)
                        this.containerControl.OnKeyDown(e);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (DockContainerControl)
            {
                if (this.ContainerControl != null)
                {
                    if (this.Width != 0 && this.Height != 0) // Per evitare bug su minimize della form
                    {
                        this.ContainerControl.Left = 0;
                        this.ContainerControl.Top = 0;
                        this.ContainerControl.Size = new SizeF(this.Width, this.Height);
                    }
                }
            }
        }

#region Events

        protected override void OnPaint(PaintEventArgs e)
        {
            //e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
            this.containerControl.InternalPaint(e.Graphics);
            base.OnPaint(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!BlockInputEvents)
            {
                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(this.containerControl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(this.containerControl.Top, 0, MidpointRounding.ToEven), e.Delta);
                this.containerControl.OnMouseDown(e2);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!BlockInputEvents)
            {
                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(this.containerControl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(this.containerControl.Top, 0, MidpointRounding.ToEven), e.Delta);
                this.containerControl.OnMouseMove(e2);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!BlockInputEvents)
            {
                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(this.containerControl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(this.containerControl.Top, 0, MidpointRounding.ToEven), e.Delta);
                this.containerControl.OnMouseUp(e2);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!BlockInputEvents)
            {
                this.containerControl.OnMouseLeave(e);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if(!BlockInputEvents)
            {
                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(this.containerControl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(this.containerControl.Top, 0, MidpointRounding.ToEven), e.Delta);
                this.containerControl.OnMouseWheel(e2);
            }
        }

#endregion

    }
}
