using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections.ObjectModel;

namespace PlayerUI.PlayerControls
{
    public partial class Container : PlayerControl
    {
        /// <summary>
        /// L'ordine degli elementi in questa lista rappresenta il loro z-order.
        /// Gli elementi in testa sono sopra rispetto a quelli in coda.
        /// </summary>
        protected readonly PlayerControlCollection controls;

        public delegate void ControlAddedEventHandler(object sender, PlayerControlEventArgs e);
        public event ControlAddedEventHandler ControlAdded;

        public delegate void ControlRemovedEventHandler(object sender, PlayerControlEventArgs e);
        public event ControlRemovedEventHandler ControlRemoved;

        PlayerControl lastEnterControl = null;
        private SizeF sizeBeforeResize;

        long lastDoubleClickMsec = 0;
        Point lastDoubleClickPt = new Point();
        PlayerControl lastDoubleClickCtl = null;
        bool suppressNextClick = false;

        public partial class PlayerControlCollection : Collection<PlayerControl> { }

        public Container(SemanticType c) : base(c)
        {
            this.controls = new PlayerControlCollection(this);

            this.Size = new SizeF(150, 100);
            this.TabStop = false;
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

        [DefaultValue(false)]
        public override bool TabStop { get; set; }
        
        [Browsable(false)]
        public PlayerControlCollection Controls { get { return this.controls; } }

        protected void OnControlAdded(PlayerControl c)
        {
            c.ParentView = this.ParentView;
            c.Parent = this;
            this.Invalidate();
            if (ControlAdded != null) ControlAdded(this, new PlayerControlEventArgs(c));
        }

        protected virtual void OnControlRemoved(PlayerControl c)
        {
            c.ParentView = null;
            c.Parent = null;
            if (lastEnterControl == c)
                lastEnterControl = null;
            this.Invalidate();
            if (ControlAdded != null) ControlRemoved(this, new PlayerControlEventArgs(c));
        }

        public virtual void BringToFront(PlayerControl c)
        {
            controls.MoveToFirst(c);
            c.Invalidate();
        }

        public virtual void SendToBack(PlayerControl c)
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
        private void AdjustSizeWithAnchor(PlayerControl control, SizeF oldContainerSize)
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

        public List<PlayerControl> GetAllChildren()
        {
            var result = new List<PlayerControl>();
            var containers = new Stack<PlayerControls.Container>();
            result.Add(this);
            containers.Push(this);

            while (containers.Count > 0)
            {
                var parent = containers.Pop();
                foreach (var ctrl in parent.Controls)
                {
                    result.Add(ctrl);
                    if (ctrl is PlayerControls.Container)
                    {
                        containers.Push((PlayerControls.Container)ctrl);
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
        public PlayerControl GetNextControl(PlayerControl ctl, bool forward)
        {
            IEnumerable<PlayerControl> ctrls;

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

        private PlayerControl focusedControl;
        public PlayerControl FocusedControl
        {
            get { return this.focusedControl; }
            set
            {
                PlayerControl oldFocusCtl = this.focusedControl;
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

            PlayerControl ctl = GetNextControl(this.FocusedControl, forward);
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
            
            foreach (PlayerControl c in this.controls.Reverse())
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
            PlayerControl ctl = controls.FirstOrDefault(c => c.HitTest(e.Location));
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
            lastDoubleClickMsec = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            lastDoubleClickPt = e.Location;
            lastDoubleClickCtl = ctl;
        }

        public override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            //var someoneHit = false;

            PlayerControl ctl = controls.FirstOrDefault(c => c.Capture);
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

            PlayerControl ctl = controls.FirstOrDefault(c => c.Capture);
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
                    PlayerControl enterctl = controls.FirstOrDefault(c => c.HitTest(e.Location));
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

            PlayerControl ctl = controls.FirstOrDefault(c => c.HitTest(e.Location));
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
                PlayerControls.PlayerControl c = SerializationHelper.GetPlayerControlInstanceFromTagName(child.Name);
                this.OnControlAdded(c);
                c.FromXmlElement(child, resources);
            }

        }

    }
}
