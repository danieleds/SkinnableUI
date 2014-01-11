using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections.ObjectModel;

namespace UnusefulPlayer.PlayerControls
{
    class Container : PlayerControl
    {
        /// <summary>
        /// L'ordine degli elementi in questa lista rappresenta il loro z-order.
        /// Gli elementi in testa sono sopra rispetto a quelli in coda.
        /// </summary>
        protected LinkedList<PlayerControl> controls = new LinkedList<PlayerControl>();

        public delegate void ControlAddedEventHandler(object sender, PlayerControlEventArgs e);
        public event ControlAddedEventHandler ControlAdded;

        public delegate void ControlRemovedEventHandler(object sender, PlayerControlEventArgs e);
        public event ControlRemovedEventHandler ControlRemoved;

        PlayerControl lastHoverControl = null;
        private SizeF sizeBeforeResize;

        public Container(SemanticType c) : base(c)
        {
            this.Size = new SizeF(150, 100);
            //controls.CollectionChanged += controls_CollectionChanged;
        }

        /*void controls_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // FIXME Spostare qui la logica di AddPlayerControl e RemovePlayerControl
        }*/

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

        /// <summary>
        /// Attenzione: aggiungere e rimuovere controlli direttamente da questa lista non genera eventi, né inizializza le proprietà dei controlli.
        /// </summary>
        [Browsable(false)]
        public LinkedList<PlayerControl> Controls { get { return this.controls; } }

        // FIXME Rimuovere e usare ObservableCollection
        public void AddPlayerControl(PlayerControl c)
        {
            this.controls.AddFirst(c);
            c.ParentView = this.ParentView;
            c.Parent = this;
            this.Invalidate();
            if (ControlAdded != null) ControlAdded(this, new PlayerControlEventArgs(c));
        }

        public virtual void RemovePlayerControl(PlayerControl c)
        {
            this.controls.Remove(c);
            c.ParentView = null;
            c.Parent = null;
            if (lastHoverControl == c)
                lastHoverControl = null;
            this.Invalidate();
            if (ControlAdded != null) ControlRemoved(this, new PlayerControlEventArgs(c));
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
            // FIXME Lasciare che le posizioni e le dimensioni diventino con la virgola?
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
                    if (ctrl.GetType() == typeof(PlayerControls.Container))
                    {
                        containers.Push((PlayerControls.Container)ctrl);
                    }
                }
            }

            return result;
        }

        #region Events

        protected override void OnPaint(Graphics g)
        {
            if (backgroundNormal9P != null)
                backgroundNormal9P.Paint(g, this.Size);
            else
                drawDefaultContainer(g);
            
            foreach (PlayerControl c in this.controls.Reverse())
            {
                c.InternalPaint(g);
            }
        }

        private void drawDefaultContainer(Graphics g)
        {
            g.FillRectangle(new SolidBrush(Color.FromArgb(25, 0, 0, 0)), 0, 0, this.Size.Width - 1, this.Size.Height - 1);
            g.DrawRectangle(Pens.White, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
        }

        public override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            PlayerControl ctl = controls.FirstOrDefault(c => c.HitTest(e.Location));
            if (ctl != null)
            {
                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(ctl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(ctl.Top, 0, MidpointRounding.ToEven), e.Delta);
                ctl.OnMouseDown(e2);
            }
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
                    if (lastHoverControl != ctl)
                    {
                        // Lanciamo mouseLeave / mouseHover sul vecchio / nuovo controllo sopra il quale ci troviamo.
                        if (lastHoverControl != null)
                            lastHoverControl.OnMouseLeave(new EventArgs());
                        lastHoverControl = ctl;
                        ctl.OnMouseHover(new EventArgs());
                    }

                    MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(ctl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(ctl.Top, 0, MidpointRounding.ToEven), e.Delta);
                    ctl.OnMouseMove(e2);
                }
                else
                {
                    // Non ci troviamo sopra a nessun controllo: se qualcuno dei nostri
                    // controlli aveva il mouseHover, gli chiamiamo mouseLeave.
                    if (lastHoverControl != null)
                    {
                        lastHoverControl.OnMouseLeave(new EventArgs());
                        lastHoverControl = null;
                    }
                }
            }
            else
            {
                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(ctl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(ctl.Top, 0, MidpointRounding.ToEven), e.Delta);
                ctl.OnMouseMove(e2);
            }

            /*
            foreach (var ctl in controls)
            {
                var capture = ctl.Capture;
                var hit = ctl.HitTest(e.Location);

                if (hit)
                {
                    someoneHit = true;
                    if (lastHoverControl != ctl)
                    {
                        // Lanciamo mouseLeave / mouseHover sul vecchio / nuovo controllo sopra il quale ci troviamo.
                        if (lastHoverControl != null)
                            lastHoverControl.OnMouseLeave(new EventArgs());
                        lastHoverControl = ctl;
                        ctl.OnMouseHover(new EventArgs());
                    }
                }

                if (capture || hit)
                {
                    // Lanciamo l'evento mouseMove
                    MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(ctl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(ctl.Top, 0, MidpointRounding.ToEven), e.Delta);
                    ctl.OnMouseMove(e2);
                }
            }

            if (!someoneHit)
            {
                // Non ci troviamo sopra a nessun controllo: se qualcuno dei nostri
                // controlli aveva il mouseHover, gli chiamiamo mouseLeave.
                if (lastHoverControl != null)
                {
                    lastHoverControl.OnMouseLeave(new EventArgs());
                    lastHoverControl = null;
                }
            }*/
        }

        public override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            PlayerControl ctl = controls.FirstOrDefault(c => c.Capture || c.HitTest(e.Location));
            if (ctl != null)
            {
                var capture = ctl.Capture;
                var hit = ctl.HitTest(e.Location);

                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(ctl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(ctl.Top, 0, MidpointRounding.ToEven), e.Delta);
                ctl.OnMouseUp(e2);

                if (e.Button == MouseButtons.Left && capture && hit)
                    ctl.OnClick(new EventArgs()); // FIXME Solo l'elemento più annidato deve ricevere l'onClick??

                if (capture && !hit)
                {
                    // Il capturing è finito e ora ci ritroviamo su un controllo diverso... lanciamo
                    // gli eventi MouseLeave / MouseHover
                    ctl.OnMouseLeave(new EventArgs());
                    PlayerControl hoverctl = controls.FirstOrDefault(c => c.HitTest(e.Location));
                    if (hoverctl != null)
                    {
                        lastHoverControl = hoverctl;
                        hoverctl.OnMouseHover(new EventArgs());
                    }
                }
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

        #endregion

        public override System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument document, Dictionary<string, System.IO.MemoryStream> resources)
        {
            var node = base.GetXmlElement(document, resources);
            SerializationHelper.SetNinePatch(this.backgroundNormal9P, "backgroundNormal9P", resources, node);

            foreach (var item in this.Controls.Reverse()) // FIXME Z-index
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
                var ctype = (PlayerControls.PlayerControl.SemanticType)Enum.Parse(typeof(PlayerControls.PlayerControl.SemanticType), child.Name);

                PlayerControls.PlayerControl.SemanticTypeMeta info =
                    PlayerControls.PlayerControl.GetPlayerControlInstanceInfo(ctype);

                PlayerControls.PlayerControl c = (PlayerControls.PlayerControl)Activator.CreateInstance(info.InstanceType, new object[] { ctype });
                this.AddPlayerControl(c);
                c.FromXmlElement(child, resources);
            }

        }

    }
}
