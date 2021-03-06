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
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections.ObjectModel;

namespace SkinnableUI.SkinnableControls
{
    public partial class Container : SkinnableControl
    {
        /// <summary>
        /// L'ordine degli elementi in questa lista rappresenta il loro z-order.
        /// Gli elementi in testa sono sopra rispetto a quelli in coda.
        /// </summary>
        protected readonly SkinnableControlCollection controls;

        public delegate void ControlAddedEventHandler(object sender, SkinnableControlEventArgs e);
        public event ControlAddedEventHandler ControlAdded;

        public delegate void ControlRemovedEventHandler(object sender, SkinnableControlEventArgs e);
        public event ControlRemovedEventHandler ControlRemoved;

        SkinnableControl lastEnterControl = null;
        private SizeF sizeBeforeResize;

        long lastDoubleClickMsec = 0;
        Point lastDoubleClickPt = new Point();
        SkinnableControl lastDoubleClickCtl = null;
        bool suppressNextClick = false;

        public partial class SkinnableControlCollection : Collection<SkinnableControl> { }

        public Container(SemanticType c) : base(c)
        {
            this.controls = new SkinnableControlCollection(this);

            this.Size = new SizeF(150, 100);
            this.TabStop = false;
        }

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
        
        [Browsable(false)]
        public SkinnableControlCollection Controls { get { return this.controls; } }

        protected void OnControlAdded(SkinnableControl c)
        {
            c.ParentView = this.ParentView;
            c.Parent = this;
            this.Invalidate();
            if (ControlAdded != null) ControlAdded(this, new SkinnableControlEventArgs(c));
        }

        protected virtual void OnControlRemoved(SkinnableControl c)
        {
            c.ParentView = null;
            c.Parent = null;
            if (lastEnterControl == c)
                lastEnterControl = null;
            this.Invalidate();
            if (ControlAdded != null) ControlRemoved(this, new SkinnableControlEventArgs(c));
        }

        public virtual void BringToFront(SkinnableControl c)
        {
            controls.MoveToFirst(c);
            c.Invalidate();
        }

        public virtual void SendToBack(SkinnableControl c)
        {
            controls.MoveToLast(c);
            c.Invalidate();
        }

        public override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (sizeBeforeResize != SizeF.Empty && sizeBeforeResize != this.Size)
            {
                foreach (var control in this.Controls)
                {
                    AdjustSizeWithAnchor(control, this.sizeBeforeResize);
                }
            }

            sizeBeforeResize = this.Size;
        }

        /// <summary>
        /// In seguito a un ridimensionamento di this, corregge la dimensione e la posizione
        /// di un controllo in accordo con la sua proprietà Anchor.
        /// </summary>
        /// <param name="control">Controllo di cui correggere dimensione e posizione</param>
        /// <param name="oldContainerSize">La vecchia dimensione di this</param>
        private void AdjustSizeWithAnchor(SkinnableControl control, SizeF oldContainerSize)
        {
            var a_left = (control.Anchor & AnchorStyles.Left) == AnchorStyles.Left;
            var a_right = (control.Anchor & AnchorStyles.Right) == AnchorStyles.Right;
            var a_top = (control.Anchor & AnchorStyles.Top) == AnchorStyles.Top;
            var a_bottom = (control.Anchor & AnchorStyles.Bottom) == AnchorStyles.Bottom;

            if (!a_left && !a_right)
            {
                // ctrl.left + (half) : sizeBeforeResize.Width = x + (half) : this.Size.Width
                var half = control.Size.Width / 2;
                control.Left = ((control.Left + half) * this.Size.Width / oldContainerSize.Width) - half;
            }
            else if (a_left && a_right)
            {
                var control_right = oldContainerSize.Width - control.Left - control.Size.Width;
                control.Size = new SizeF(this.Size.Width - control.Left - control_right, control.Size.Height);
            }
            else if (!a_left && a_right)
            {
                control.Left += this.Size.Width - oldContainerSize.Width;
            }

            if (!a_top && !a_bottom)
            {
                var half = control.Size.Height / 2;
                control.Top = ((control.Top + half) * this.Size.Height / oldContainerSize.Height) - half;
            }
            else if (a_top && a_bottom)
            {
                // ctrl.height = container.height - ctrl.top - ctrl.bottom
                // ctrl.bottom = container.height - ctrl.top - ctrl.height
                var control_bottom = oldContainerSize.Height - control.Top - control.Size.Height;
                control.Size = new SizeF(control.Size.Width, this.Size.Height - control.Top - control_bottom);
            }
            else if (!a_top && a_bottom)
            {
                control.Top += this.Size.Height - oldContainerSize.Height;
            }
        }

        public List<SkinnableControl> GetAllChildren()
        {
            var result = new List<SkinnableControl>();
            var containers = new Stack<SkinnableControls.Container>();
            result.Add(this);
            containers.Push(this);

            while (containers.Count > 0)
            {
                var parent = containers.Pop();
                foreach (var ctrl in parent.Controls)
                {
                    result.Add(ctrl);
                    if (ctrl is SkinnableControls.Container)
                    {
                        containers.Push((SkinnableControls.Container)ctrl);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Restituisce il prossimo (o precedente) controllo secondo le proprietà TabOrder e TabStop.
        /// Dopo aver raggiunto l'ultimo controllo restituisce null.
        /// </summary>
        /// <param name="ctl">Controllo da cui iniziare la ricerca. Se è null, parte dall'inizio (o dalla fine).</param>
        /// <param name="forward"></param>
        /// <returns></returns>
        public SkinnableControl GetNextControl(SkinnableControl ctl, bool forward)
        {
            IEnumerable<SkinnableControl> ctrls;

            if (ctl == null)
            {
                if(forward)
                    ctrls = from c in this.controls
                            where (c.TabStop == true || c is Container)
                            orderby c.TabIndex, c.Top, c.Left
                            select c;
                else
                    ctrls = from c in this.controls
                            where (c.TabStop == true || c is Container)
                            orderby c.TabIndex descending, c.Top descending, c.Left descending
                            select c;
            }
            else
            {
                if (forward)
                {
                    ctrls = from c in this.controls
                            where (c.TabStop == true || c is Container)
                            && c.TabIndex >= ctl.TabIndex
                            && (c.TabIndex == ctl.TabIndex ?
                            (c.Top != ctl.Top ? c.Top > ctl.Top : c.Left > ctl.Left)
                            : true)
                            orderby c.TabIndex, c.Top, c.Left
                            select c;
                }
                else
                {
                    ctrls = from c in this.controls
                            where (c.TabStop == true || c is Container)
                            && c.TabIndex <= ctl.TabIndex
                            && (c.TabIndex == ctl.TabIndex ?
                            (c.Top != ctl.Top ? c.Top < ctl.Top : c.Left < ctl.Left)
                            : true)
                            orderby c.TabIndex descending, c.Top descending, c.Left descending
                            select c;
                }
            }
            
            return ctrls.FirstOrDefault(c => c != ctl);
        }

        private SkinnableControl focusedControl;
        [Browsable(false)]
        public SkinnableControl FocusedControl
        {
            get { return this.focusedControl; }
            set
            {
                SkinnableControl oldFocusCtl = this.focusedControl;
                this.focusedControl = value;
                if(oldFocusCtl != null) oldFocusCtl.Invalidate();
                if(value != null) value.Invalidate();
            }
        }

        /// <summary>
        /// Sposta ricorsivamente il focus al controllo successivo (o precedente).
        /// Restituisce false se i controlli sono finiti, altrimenti true.
        /// </summary>
        /// <param name="forward"></param>
        /// <returns></returns>
        public bool DoTab(bool forward, bool showTabFocus)
        {
            if (this.FocusedControl is Container)
            {
                Container container = (Container)this.FocusedControl;
                bool finished = !container.DoTab(forward, showTabFocus);
                if (!finished)
                    return true;
            }

            SkinnableControl ctl = GetNextControl(this.FocusedControl, forward);
            if(this.focusedControl != null) this.focusedControl.IsShowingFocusRect = false;
            if(ctl != null) ctl.IsShowingFocusRect = showTabFocus;
            this.FocusedControl = ctl;

            if (ctl == null)
                return false;
            else
            {
                if (ctl is Container)
                {
                    Container container = (Container)ctl;
                    if (container.focusedControl != null) container.focusedControl.IsShowingFocusRect = false;
                    container.FocusedControl = container.GetNextControl(null, forward);
                    if (container.focusedControl != null) container.focusedControl.IsShowingFocusRect = showTabFocus;
                }
                return true;
            }
        }

        public override void PaintFocusRect(Graphics g)
        {
            
        }

        #region Events

        protected override void OnPaint(Graphics g)
        {
            if (backgroundNormal9P != null)
                backgroundNormal9P.Paint(g, this.Size);
            
            foreach (SkinnableControl c in this.controls.Reverse())
            {
                c.InternalPaint(g);
            }

            if (this.ParentView != null && this.ParentView.DesignSkinMode)
            {
                Pen p = new Pen(Color.Gray);
                p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                g.DrawRectangle(p, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
            }
        }

        public override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            SkinnableControl ctl = controls.FirstOrDefault(c => c.HitTest(e.Location));
            if (ctl != null)
            {
                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(ctl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(ctl.Top, 0, MidpointRounding.ToEven), e.Delta);
                ctl.OnMouseDown(e2);
                this.FocusedControl = ctl;
            }
            
            // Gestione doppio click
            if ((DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond - lastDoubleClickMsec) <= System.Windows.Forms.SystemInformation.DoubleClickTime)
            {
                Size dbsz = System.Windows.Forms.SystemInformation.DoubleClickSize;
                if (Math.Abs(lastDoubleClickPt.X - e.X) <= dbsz.Width && Math.Abs(lastDoubleClickPt.Y - e.Y) <= dbsz.Height)
                {
                    suppressNextClick = true;
                    if (ctl == null)
                        base.OnMouseDoubleClick(new MouseEventArgs(e.Button, e.Clicks + 1, e.X, e.Y, e.Delta));
                    else
                        ctl.OnMouseDoubleClick(new MouseEventArgs(e.Button, e.Clicks + 1, e.X - (int)Math.Round(ctl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(ctl.Top, 0, MidpointRounding.ToEven), e.Delta));
                }
            }
            else
            {
                lastDoubleClickMsec = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                lastDoubleClickPt = e.Location;
                lastDoubleClickCtl = ctl;
            }
        }

        public override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            //var someoneHit = false;

            SkinnableControl ctl = controls.FirstOrDefault(c => c.Capture);
            if (ctl == null)
            {
                ctl = controls.FirstOrDefault(c => c.HitTest(e.Location));
                if (ctl != null)
                {
                    if (lastEnterControl != ctl)
                    {
                        // Lanciamo mouseLeave / mouseEnter sul vecchio / nuovo controllo sopra il quale ci troviamo.
                        if (lastEnterControl != null)
                            lastEnterControl.OnMouseLeave(new EventArgs());
                        lastEnterControl = ctl;
                        ctl.OnMouseEnter(new EventArgs());
                    }

                    MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(ctl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(ctl.Top, 0, MidpointRounding.ToEven), e.Delta);
                    ctl.OnMouseMove(e2);
                }
                else
                {
                    // Non ci troviamo sopra a nessun controllo: se qualcuno dei nostri
                    // controlli aveva il mouseEnter, gli chiamiamo mouseLeave.
                    if (lastEnterControl != null)
                    {
                        lastEnterControl.OnMouseLeave(new EventArgs());
                        lastEnterControl = null;
                    }
                }
            }
            else
            {
                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(ctl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(ctl.Top, 0, MidpointRounding.ToEven), e.Delta);
                ctl.OnMouseMove(e2);
            }
        }

        public override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            SkinnableControl ctl = controls.FirstOrDefault(c => c.Capture);
            if(ctl == null) ctl = controls.FirstOrDefault(c => c.HitTest(e.Location));

            if (ctl == null)
            {
                // Gestione click
                if (!this.suppressNextClick)
                {
                    var capture = this.Capture;
                    var hit = this.IsInside(e.Location);

                    if (e.Button == MouseButtons.Left && capture && hit)
                        this.OnClick(new EventArgs());
                }
            }
            else
            {
                var capture = ctl.Capture;
                var hit = ctl.HitTest(e.Location);

                // Gestione click
                if (e.Button == MouseButtons.Left && capture && hit && !this.suppressNextClick)
                    ctl.OnClick(new EventArgs());

                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(ctl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(ctl.Top, 0, MidpointRounding.ToEven), e.Delta);
                ctl.OnMouseUp(e2);

                if (capture && !hit)
                {
                    // Il capturing è finito e ora ci ritroviamo su un controllo diverso... lanciamo
                    // gli eventi MouseLeave / MouseEnter
                    ctl.OnMouseLeave(new EventArgs());
                    SkinnableControl enterctl = controls.FirstOrDefault(c => c.HitTest(e.Location));
                    if (enterctl != null)
                    {
                        lastEnterControl = enterctl;
                        enterctl.OnMouseEnter(new EventArgs());
                    }
                }
            }

            this.suppressNextClick = false;
        }

        public override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (lastEnterControl != null)
            {
                lastEnterControl.OnMouseLeave(new EventArgs());
                lastEnterControl = null;
            }
        }

        public override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            SkinnableControl ctl = controls.FirstOrDefault(c => c.HitTest(e.Location));
            if (ctl != null)
            {
                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(ctl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(ctl.Top, 0, MidpointRounding.ToEven), e.Delta);
                ctl.OnMouseWheel(e2);
            }
        }

        public override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.None)
            {
                if (this.focusedControl != null)
                {
                    if (this.focusedControl is Container)
                        this.focusedControl.OnKeyDown(e);
                    else
                        this.focusedControl.OnClick(new EventArgs());
                }
            }
            else
            {
                base.OnKeyDown(e);
                if (!e.Handled)
                {
                    if (this.focusedControl != null)
                        this.focusedControl.OnKeyDown(e);
                }
            }
        }

        #endregion

        public override System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument document, Dictionary<string, System.IO.MemoryStream> resources)
        {
            var node = base.GetXmlElement(document, resources);
            SerializationHelper.SetNinePatch(this.backgroundNormal9P, "backgroundNormal9P", resources, node);

            foreach (var item in this.Controls.Reverse())
            {
                node.AppendChild(item.GetXmlElement(document, resources));
            }

            return node;
        }

        public override void FromXmlElement(System.Xml.XmlElement element, Dictionary<string, System.IO.MemoryStream> resources)
        {
            base.FromXmlElement(element, resources);

            SerializationHelper.LoadBitmapFromResources(element, "backgroundNormal9P", resources, s => this.BackgroundNormal9P = s);

            foreach (System.Xml.XmlElement child in element.ChildNodes)
            {
                SkinnableControls.SkinnableControl c = SerializationHelper.GetPlayerControlInstanceFromTagName(child.Name);
                this.OnControlAdded(c);
                c.FromXmlElement(child, resources);
            }

        }

    }
}
