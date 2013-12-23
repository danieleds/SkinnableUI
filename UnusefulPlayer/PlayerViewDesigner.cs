using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using UnusefulPlayer.PlayerControls;

namespace UnusefulPlayer
{
    class PlayerViewDesigner : PlayerView
    {
        public new bool DesignSkinMode { get { return true; } }
        public bool DebugShowPaints { get; set; }

        // Variabili helper per il dragging in design mode
        PlayerControl draggingControl;
        Point draggingPosition; // Posizione corrente del puntatore (coordinate relative a this). Ha senso solo se draggingControl != null.
        Bitmap draggingBitmap; // Ha senso solo se draggingControl != null.
        Container draggingControlOriginalContainer; // Il Container in cui si trovava il controllo prima del dragging. Ha senso solo se draggingControl != null.
        PointF draggingOffset; // Ha senso solo se draggingControl != null o se dragStarting = true.
        bool dragStarting = false; // true se è stato fatto un MouseDown e stiamo aspettando un delta di spostamento sufficiente.
        Point dragStartPosition; // Posizione del MouseDown iniziale (coordinate relative alla finestra). Ha senso solo se draggingControl != null.

        PlayerControl resizingControl;
        Direction resizingDirection;

        private Random rand = new Random();

        [Flags]
        private enum Direction
        {
            None = 0,
            Left = 1 << 0,
            Up = 1 << 1,
            Right = 1 << 2,
            Down = 1 << 3
        };

        public delegate void SelectionChangedEventHandler(object sender, EventArgs e);
        /// <summary>
        /// Si verifica quando la selezione del controllo corrente varia.
        /// </summary>
        public event SelectionChangedEventHandler SelectionChanged;

        public delegate void SelectedObjectPropertyChangedEventHandler(object sender, EventArgs e);
        /// <summary>
        /// Si verifica quando una proprietà dell'oggetto attualmente selezionato viene modificata dall'interno.
        /// </summary>
        public event SelectedObjectPropertyChangedEventHandler SelectedObjectPropertyChanged;

        public delegate void DesignerControlsTreeChangedEventHandler(object sender, EventArgs e);
        public event DesignerControlsTreeChangedEventHandler DesignerControlsTreeChanged;

        public PlayerViewDesigner()
        {
            DebugShowPaints = false;
        }

        private PlayerControl selectedControl;
        public PlayerControl SelectedControl
        {
            get { return selectedControl; }
            set
            {
                if (selectedControl != value)
                {
                    if (selectedControl != null)
                    {
                        selectedControl.Resize -= selectedControl_MetaControlsNeedRepaint;
                        selectedControl.Move -= selectedControl_MetaControlsNeedRepaint;
                    }

                    selectedControl = value;
                    
                    if (selectedControl != null)
                    {
                        selectedControl.Resize += selectedControl_MetaControlsNeedRepaint;
                        selectedControl.Move += selectedControl_MetaControlsNeedRepaint;
                    }
                    this.Invalidate();
                    if (SelectionChanged != null) SelectionChanged(this, new EventArgs());
                }
            }
        }

        /// <summary>
        /// Event handler che viene chiamato quando è necessario ridisegnare i metacontrolli
        /// (ad esempio, i resize handle).
        /// Una situazione in cui viene chiamato è, ad esempio, quando si ridimensiona
        /// o si sposta il controllo selezionato.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void selectedControl_MetaControlsNeedRepaint(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        /// <summary>
        /// Dato un controllo e un punto, determina quale resize handle è disponibile su quel punto.
        /// </summary>
        /// <param name="c">Controllo</param>
        /// <param name="p">Punto</param>
        /// <returns></returns>
        private Direction WhatResizeHandle(PlayerControl c, PointF p)
        {
            Direction dir = Direction.None;
            var loc = c.GetAbsoluteLocation();
            if (loc.X + c.Size.Width - 5 <= p.X && p.X <= loc.X + c.Size.Width + 5
                && loc.Y + c.Size.Height - 5 <= p.Y && p.Y <= loc.Y + c.Size.Height + 5)
                dir = Direction.Right | Direction.Down;
            else if (loc.X - 6 <= p.X && p.X <= loc.X + 5
                && loc.Y - 6 <= p.Y && p.Y <= loc.Y + 5)
                dir = Direction.Left | Direction.Up;
            else if (loc.X + c.Size.Width - 5 <= p.X && p.X <= loc.X + c.Size.Width + 5
                && loc.Y - 6 <= p.Y && p.Y <= loc.Y + 5)
                dir = Direction.Right | Direction.Up;
            else if (loc.X - 6 <= p.X && p.X <= loc.X + 5
                && loc.Y + c.Size.Height - 5 <= p.Y && p.Y <= loc.Y + c.Size.Height + 5)
                dir = Direction.Left | Direction.Down;
            else if (loc.X + c.Size.Width - 2 <= p.X && p.X <= loc.X + c.Size.Width + 5
                && loc.Y <= p.Y && p.Y <= loc.Y + c.Size.Height)
                dir = Direction.Right;
            else if (loc.Y + c.Size.Height - 2 <= p.Y && p.Y <= loc.Y + c.Size.Height + 5
                && loc.X <= p.X && p.X <= loc.X + c.Size.Width)
                dir = Direction.Down;
            else if (loc.Y - 6 <= p.Y && p.Y <= loc.Y + 2
                && loc.X <= p.X && p.X <= loc.X + c.Size.Width)
                dir = Direction.Up;
            else if (loc.X - 6 <= p.X && p.X <= loc.X + 2
                && loc.Y <= p.Y && p.Y <= loc.Y + c.Size.Height)
                dir = Direction.Left;

            return dir;
        }

        private Tuple<float, float, PlayerControl> RecursiveHitTest(int x, int y)
        {
            if (this.containerControl.HitTest(new PointF(x, y)) == false)
                return null;

            var closerContainer = this.containerControl;
            PlayerControl finalHit = this.containerControl;
            float absX = 0, absY = 0;
            do
            {
                var shiftedLocation = new PointF(x - absX, y - absY);
                var tmp = closerContainer.Controls.FirstOrDefault(k => k.HitTest(shiftedLocation));
                if (tmp == null)
                    break;
                else
                {
                    absX += tmp.Left;
                    absY += tmp.Top;
                    finalHit = tmp;
                    if (tmp.GetType() == typeof(PlayerControls.Container))
                    {
                        closerContainer = (PlayerControls.Container)tmp;
                    }
                    else
                    {
                        finalHit = tmp;
                        break;
                    }
                }
            } while (true);

            return new Tuple<float, float, PlayerControl>(absX, absY, finalHit);
        }

        public override void SetSkin(Skin skin)
        {
            base.SetSkin(skin);
            if (DesignerControlsTreeChanged != null) DesignerControlsTreeChanged(this, new EventArgs());
            this.SelectedControl = null;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Disegna il rettangolo di selezione
            if (selectedControl != null)
                drawSelectionMetacontrols(e.Graphics);

            if (draggingControl != null)
            {
                e.Graphics.DrawImageUnscaled(this.draggingBitmap, this.draggingPosition.X - (int)this.draggingOffset.X, this.draggingPosition.Y - (int)this.draggingOffset.Y);
            }

            if (DebugShowPaints)
            {
                var b = new SolidBrush(Color.FromArgb(60, rand.Next(255), rand.Next(255), rand.Next(255)));
                e.Graphics.FillRectangle(b, e.ClipRectangle);
            }
        }

        private void drawSelectionMetacontrols(Graphics g)
        {
            if (selectedControl != null && selectedControl != this.ContainerControl)
            {
                var selectedControlPos = selectedControl.GetAbsoluteLocation();

                const int handleW = 6, handleH = 6;

                RectangleF[] resizeHandles = {
                    new RectangleF(selectedControlPos.X - handleW, selectedControlPos.Y - handleH, handleW, handleH),
                    new RectangleF(selectedControlPos.X + (selectedControl.Size.Width / 2) - (handleW / 2), selectedControlPos.Y - handleH, handleW, handleH),
                    new RectangleF(selectedControlPos.X + selectedControl.Size.Width - 1, selectedControlPos.Y - handleH, handleW, handleH),

                    new RectangleF(selectedControlPos.X - handleW, selectedControlPos.Y + (selectedControl.Size.Height / 2) - (handleH / 2), handleW, handleH),
                    new RectangleF(selectedControlPos.X + selectedControl.Size.Width - 1, selectedControlPos.Y + (selectedControl.Size.Height / 2) - (handleH / 2), handleW, handleH),

                    new RectangleF(selectedControlPos.X - handleW, selectedControlPos.Y + selectedControl.Size.Height - 1, handleW, handleH),
                    new RectangleF(selectedControlPos.X + (selectedControl.Size.Width / 2) - (handleW / 2), selectedControlPos.Y + selectedControl.Size.Height - 1, handleW, handleH),
                    new RectangleF(selectedControlPos.X + selectedControl.Size.Width - 1, selectedControlPos.Y + selectedControl.Size.Height - 1, handleW, handleH)
                };

                /*var topLeftHnd = resizeHandles[0];
                var bottomRightHnd = resizeHandles[7];

                if (!(e.ClipRectangle.Contains(topLeftHnd.RoundUp()) && e.ClipRectangle.Contains(bottomRightHnd.RoundUp())))
                {
                    this.Invalidate(new Rectangle(
                        (int)Math.Floor(topLeftHnd.X),
                        (int)Math.Floor(topLeftHnd.Y),
                        (int)Math.Ceiling(bottomRightHnd.X + bottomRightHnd.Width),
                        (int)Math.Ceiling(bottomRightHnd.Height + bottomRightHnd.Width)));
                }*/

                // Linee tratteggiate
                Pen selectionPen = new Pen(Color.Black);
                selectionPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                g.DrawRectangle(selectionPen, selectedControlPos.X - (handleW / 2), selectedControlPos.Y - (handleH / 2), selectedControl.Size.Width + handleW - 1, selectedControl.Size.Height + handleH - 1);

                // Handles
                foreach (RectangleF handle in resizeHandles)
                {
                    g.FillRectangle(Brushes.White, handle);
                    g.DrawRectangle(Pens.Black, handle.X, handle.Y, handle.Width, handle.Height);
                }
            }

            /*if (resizingControl != null)
            {
                var loc = resizingControl.GetAbsoluteLocation();
                drawResizingMeasure(g, loc, resizingControl.Size, Direction.Up);
                drawResizingMeasure(g, loc, resizingControl.Size, Direction.Left);
                drawResizingMeasure(g, loc, resizingControl.Size, Direction.Down);
                drawResizingMeasure(g, loc, resizingControl.Size, Direction.Right);
            }*/
        }

        private void drawResizingMeasure(Graphics g, PointF controlAbsoluteLocation, SizeF controlSize, Direction direction)
        {
            var loc = controlAbsoluteLocation;
            var size = controlSize;

            var str = "";
            if (direction == Direction.Up || direction == Direction.Down)
                str = size.Width.ToString();
            else if (direction == Direction.Left || direction == Direction.Right)
                str = size.Height.ToString();
            else throw new ArgumentException("Invalid direction");

            var strSize = g.MeasureString(str, this.Font);

            const int textVerticalShift = 6; // Quanto spostare il testo dal bordo superiore (o inferiore) del controllo.
            const int textLateralMargin = 3; // Spazio vuoto a destra e a sinistra (o sopra e sotto) del testo

            var t = g.Transform;
            if (direction == Direction.Left || direction == Direction.Right)
            {
                g.TranslateTransform(-controlAbsoluteLocation.X, -controlAbsoluteLocation.Y, System.Drawing.Drawing2D.MatrixOrder.Append);
                g.RotateTransform(-90, System.Drawing.Drawing2D.MatrixOrder.Append);
                g.TranslateTransform(controlAbsoluteLocation.X, controlAbsoluteLocation.Y + size.Height, System.Drawing.Drawing2D.MatrixOrder.Append);

                size = new SizeF(size.Height, size.Width);
            }

            if (direction == Direction.Down)
                g.TranslateTransform(0, size.Height + strSize.Height + textVerticalShift, System.Drawing.Drawing2D.MatrixOrder.Append);
            else if (direction == Direction.Right)
                g.TranslateTransform(size.Height + strSize.Height + textVerticalShift, 0, System.Drawing.Drawing2D.MatrixOrder.Append);

            var barHeight = strSize.Height + textVerticalShift;
            g.DrawLine(Pens.Blue, loc.X, loc.Y, loc.X, loc.Y - barHeight);
            g.DrawLine(Pens.Blue, loc.X + size.Width, loc.Y, loc.X + size.Width, loc.Y - barHeight);

            var realTextVerticalShift = (direction == Direction.Down || direction == Direction.Right) ? 0 : textVerticalShift;

            if (strSize.Width + textLateralMargin + 15 < size.Width)
            {
                var strPos = new PointF(loc.X + size.Width / 2 - strSize.Width / 2, loc.Y - strSize.Height - realTextVerticalShift);

                g.DrawLine(Pens.Blue, loc.X, loc.Y - strSize.Height / 2 - realTextVerticalShift, strPos.X - textLateralMargin, loc.Y - strSize.Height / 2 - realTextVerticalShift);
                g.DrawLine(Pens.Blue, strPos.X + strSize.Width + textLateralMargin, loc.Y - strSize.Height / 2 - realTextVerticalShift, loc.X + size.Width, loc.Y - strSize.Height / 2 - realTextVerticalShift);

                g.DrawString(size.Width.ToString(), this.Font, Brushes.Blue, strPos);
            }
            else
            {
                var strPos = new PointF(loc.X + size.Width / 2 - strSize.Width / 2, loc.Y - 2 * strSize.Height - realTextVerticalShift);

                g.DrawLine(Pens.Blue, loc.X, loc.Y - strSize.Height / 2 - realTextVerticalShift, loc.X + size.Width, loc.Y - strSize.Height / 2 - realTextVerticalShift);

                g.DrawString(size.Width.ToString(), this.Font, Brushes.Blue, strPos);
            }

            g.Transform = t;
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            
            if (ControlDropAllowed(this.PointToClient(new Point(e.X, e.Y)), true) != null)
                e.Effect = DragDropEffects.Copy;
            else if (draggingControl != null && ControlDropAllowed(this.PointToClient(new Point(e.X, e.Y)), false) != null)
                e.Effect = DragDropEffects.Move;
            else
                e.Effect = DragDropEffects.None;
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            // FIXME Su DragLeave, nascondere il controllo draggato!! Altrimenti si ferma su un bordo e fa schifo.

            if (e.Data.GetDataPresent(typeof(PlayerControls.PlayerControl.SemanticType)))
            {
                var info = ControlDropAllowed(this.PointToClient(new Point(e.X, e.Y)), true);
                if (info != null)
                {
                    e.Effect = DragDropEffects.Copy;
                    this.SelectedControl = info.Item3;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else if (e.Data.GetDataPresent(typeof(PlayerControl).FullName))
            {
                if (draggingControl != null)
                {
                    var info = ControlDropAllowed(this.PointToClient(new Point(e.X, e.Y)), false);
                    if (info != null)
                    {
                        var differentContainer = (info.Item3 != this.draggingControlOriginalContainer);
                        if(differentContainer)
                            e.Effect = DragDropEffects.Move;
                        else
                            e.Effect = DragDropEffects.Scroll; // Usiamo Scroll per rappresentare il fatto che stiamo spostando all'interno dello stesso contenitore originale.
                        var oldDraggingPosition = this.draggingPosition;
                        this.draggingPosition = this.PointToClient(new Point(e.X, e.Y));
                        if (this.draggingPosition != oldDraggingPosition)
                            this.Invalidate();
                    }
                    else
                    {
                        e.Effect = DragDropEffects.None;
                    }
                }
                else e.Effect = DragDropEffects.None;
            }
        }

        protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
        {
            base.OnGiveFeedback(e);

            e.UseDefaultCursors = e.Effect == DragDropEffects.Move;
            if (!e.UseDefaultCursors)
            {
                Cursor.Current = Cursors.Default;
            }
        }

        protected override void OnQueryContinueDrag(QueryContinueDragEventArgs e)
        {
            base.OnQueryContinueDrag(e);

            var leftPressed = (e.KeyState & 1) == 1;
            var rightPressed = (e.KeyState & 2) == 2;
            var middlePressed = (e.KeyState & 16) == 16;
            var noMouseButtonsPressed = !(leftPressed || rightPressed || middlePressed);

            if (e.EscapePressed || noMouseButtonsPressed)
            {
                if (draggingControl != null)
                {
                    // Stavamo facendo il dragging di un nostro controllo
                    // Rimettiamo il controllo al suo posto
                    this.draggingControlOriginalContainer.AddPlayerControl(draggingControl);
                    this.SelectedControl = draggingControl;
                    this.draggingControl = null;
                } 
            }

            // ATTENZIONE!! Non tocchiamo e.Action: vogliamo il comportamento predefinito (cancel se e solo se viene premuto ESC).
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);

            if (e.Data.GetDataPresent(typeof(PlayerControls.PlayerControl.SemanticType)))
            {
                // Drop dalla toolbox

                var dropInfo = ControlDropAllowed(this.PointToClient(new Point(e.X, e.Y)), true);
                if (dropInfo != null)
                {
                    PlayerControls.PlayerControl.SemanticType ctype =
                        (PlayerControls.PlayerControl.SemanticType)e.Data.GetData(typeof(PlayerControls.PlayerControl.SemanticType));

                    PlayerControls.PlayerControl.SemanticTypeMeta info =
                        PlayerControls.PlayerControl.GetPlayerControlInstanceInfo(ctype);

                    e.Effect = DragDropEffects.Copy;

                    // Istanziamo un nuovo oggetto del tipo draggato, e lo aggiungiamo al playerView
                    PlayerControls.PlayerControl c = (PlayerControls.PlayerControl)Activator.CreateInstance(info.InstanceType, new object[] { ctype });

                    dropInfo.Item3.AddPlayerControl(c);
                    var location = this.PointToClient(new Point(e.X, e.Y));
                    // Non posizioniamo sotto al mouse le coordinate (0,0) del controllo: lo spostiamo un pochino in alto e a sinistra.
                    const float mouseOffsetX = 7, mouseOffsetY = 7;
                    c.Location = new PointF(Math.Max(0, location.X - dropInfo.Item1 - mouseOffsetX), Math.Max(0, location.Y - dropInfo.Item2 - mouseOffsetY));

                    if (DesignerControlsTreeChanged != null) DesignerControlsTreeChanged(this, new EventArgs());
                    this.SelectedControl = c;

                    this.Focus();
                }
            }
            else if (e.Data.GetDataPresent(typeof(PlayerControl).FullName))
            {
                // Drop di un controllo
                var c = (PlayerControl)e.Data.GetData(typeof(PlayerControl).FullName);

                if (c.Parent != null)
                {
                    // Qualcuno lo ha già riaggiunto alla View prima di noi (probabilmente OnQueryContinueDrag che
                    // credeva che il dragging fosse stato annullato).
                    // Togliamolo così più sotto lo reinseriamo al posto giusto.
                    c.Parent = null;
                    this.SelectedControl = null;
                }

                System.Diagnostics.Debug.Print("#" + this.ContainerControl.Controls.Count.ToString());
                var dropInfo = ControlDropAllowed(this.PointToClient(new Point(e.X, e.Y)), false);
                if (dropInfo != null)
                {
                    var newParent = dropInfo.Item3;
                    if (newParent != c)
                    {
                        newParent.AddPlayerControl(c);
                    }
                    var location = this.PointToClient(new Point(e.X, e.Y));
                    c.Location = new PointF(location.X - this.draggingOffset.X - dropInfo.Item1, location.Y - this.draggingOffset.Y - dropInfo.Item2);
                    this.draggingControl = null;
                    if (DesignerControlsTreeChanged != null) DesignerControlsTreeChanged(this, new EventArgs());
                    this.SelectedControl = c;
                    if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
                }
            }
        }

        /// <summary>
        /// Se nella posizione specificata (relativa a this) è possibile fare il drop di un controllo, restituisce
        /// il Container in cui inserire il controllo e la sua posizione relativa a this.
        /// Altrimenti restituisce null.
        /// </summary>
        /// <param name="point">Posizione (relativa a this)</param>
        /// <param name="onlyInContainers">True se si vuole evitare il drop su un controllo che non è un Container</param>
        /// <returns></returns>
        private Tuple<float, float, PlayerControls.Container> ControlDropAllowed(Point point, bool onlyInContainers)
        {
            var location = point;
            var hitInfo = RecursiveHitTest(location.X, location.Y);
            if (hitInfo != null)
            {
                if (hitInfo.Item3.GetType() == typeof(Container))
                {
                    return new Tuple<float, float, PlayerControls.Container>(hitInfo.Item1, hitInfo.Item2, (Container)hitInfo.Item3);
                }
                else if (onlyInContainers == false)
                {
                    return new Tuple<float, float, PlayerControls.Container>(hitInfo.Item1 - hitInfo.Item3.Left, hitInfo.Item2 - hitInfo.Item3.Top, hitInfo.Item3.Parent);
                }
            }
            return null;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Gestione resize handle
            if (selectedControl != null && selectedControl != this.containerControl)
            {
                Direction resizeDir = WhatResizeHandle(selectedControl, e.Location);
                if (resizeDir != Direction.None)
                {
                    this.resizingControl = selectedControl;
                    this.resizingDirection = resizeDir;
                }
            }

            // Gestione selezione e dragging (se non è stato cliccato un resize handle)
            if (resizingControl == null)
            {
                var hitInfo = RecursiveHitTest(e.X, e.Y);
                if (hitInfo != null && hitInfo.Item3 != this.containerControl)
                {
                    PlayerControl ctl = hitInfo.Item3;
                    this.SelectedControl = ctl;
                    // Spostiamo il controllo in primo piano
                    ctl.Parent.Controls.Remove(ctl);
                    ctl.Parent.Controls.AddFirst(ctl);


                    this.dragStarting = true;
                    this.dragStartPosition = e.Location;
                    this.draggingOffset.X = e.X - hitInfo.Item1;
                    this.draggingOffset.Y = e.Y - hitInfo.Item2;


                    //this.Cursor = Cursors.SizeAll;
                }
                else if (hitInfo != null && hitInfo.Item3 == this.containerControl)
                {
                    this.SelectedControl = this.containerControl;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (this.dragStarting && this.draggingControl == null && this.SelectedControl != null)
            {
                // Prima di far partire il drag controlliamo se il pulsante premuto è il sinistro e se abbiamo superato il delta di distanza
                var delta = SystemInformation.DragSize;
                if (e.Button == System.Windows.Forms.MouseButtons.Left
                    && (Math.Abs(this.dragStartPosition.X - e.X) >= delta.Width || Math.Abs(this.dragStartPosition.Y - e.Y) >= delta.Height))
                {
                    this.dragStarting = false;

                    this.draggingControl = this.SelectedControl;
                    this.draggingControlOriginalContainer = draggingControl.Parent;
                    this.draggingBitmap = selectedControl.ToBitmap();
                    this.draggingControl.Parent.RemovePlayerControl(this.draggingControl);
                    this.SelectedControl = null;

                    System.Diagnostics.Debug.Print("->" + this.ContainerControl.Controls.Count.ToString());
                    this.DoDragDrop(new DataObject(typeof(PlayerControl).FullName, this.draggingControl), DragDropEffects.Move | DragDropEffects.Scroll);

                }
            }

            if (draggingControl == null && resizingControl == null)
            {
                // Non stiamo né draggando né ridimensionando.

                bool actionSet = false;

                // Controlliamo se siamo sopra a un resize handle
                if (selectedControl != null && selectedControl != this.containerControl)
                {
                    Direction resizeDir = WhatResizeHandle(selectedControl, e.Location);
                    actionSet = true;
                    if (resizeDir == Direction.Left || resizeDir == Direction.Right)
                        this.Cursor = Cursors.SizeWE;
                    else if (resizeDir == Direction.Up || resizeDir == Direction.Down)
                        this.Cursor = Cursors.SizeNS;
                    else if (resizeDir == (Direction.Up | Direction.Right) || resizeDir == (Direction.Down | Direction.Left))
                        this.Cursor = Cursors.SizeNESW;
                    else if (resizeDir == (Direction.Up | Direction.Left) || resizeDir == (Direction.Down | Direction.Right))
                        this.Cursor = Cursors.SizeNWSE;
                    else
                        actionSet = false;
                }

                // Controlliamo se siamo sopra a un controllo spostabile
                /*if (!actionSet)
                {
                    var hitInfo = RecursiveHitTest(e.X, e.Y);
                    if (hitInfo != null && hitInfo.Item3 != this.containerControl)
                    {
                        this.Cursor = Cursors.SizeAll;
                        actionSet = true;
                    }
                }*/

                if (!actionSet)
                    this.Cursor = Cursors.Default;
            }

            if (resizingControl != null)
            {
                const int minWidth = 11, minHeight = 11;
                // FIXME Spostarlo dal mousemove! E' costoso! Usare soluzione simile a absParentPos (vedi sopra)
                var resizingCtrlPos = resizingControl.GetAbsoluteLocation();

                if ((resizingDirection & Direction.Down) == Direction.Down)
                    resizingControl.Size = new SizeF(resizingControl.Size.Width, Math.Max(e.Y - resizingCtrlPos.Y, minHeight));
                if ((resizingDirection & Direction.Right) == Direction.Right)
                    resizingControl.Size = new SizeF(Math.Max(e.X - resizingCtrlPos.X, minWidth), resizingControl.Size.Height);
                if ((resizingDirection & Direction.Up) == Direction.Up)
                {
                    float tmp = resizingControl.Size.Height + (resizingCtrlPos.Y - e.Y);
                    if (tmp >= minHeight)
                    {
                        resizingControl.Top += (e.Y - resizingCtrlPos.Y);
                        resizingControl.Size = new SizeF(resizingControl.Size.Width, tmp);
                    }
                }
                if ((resizingDirection & Direction.Left) == Direction.Left)
                {
                    float tmp = resizingControl.Size.Width + (resizingCtrlPos.X - e.X);
                    if (tmp >= minWidth)
                    {
                        resizingControl.Left += (e.X - resizingCtrlPos.X);
                        resizingControl.Size = new SizeF(tmp, resizingControl.Size.Height);
                    }
                }

                resizingControl.OnResize(new EventArgs());
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            this.dragStarting = false;

            if (resizingControl != null)
            {
                resizingControl = null;
                this.Invalidate();
                if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyData == Keys.Delete)
            {
                if (this.SelectedControl != null && this.SelectedControl != this.ContainerControl)
                {
                    this.SelectedControl.Parent = null;
                    this.SelectedControl = null;
                    if (DesignerControlsTreeChanged != null) DesignerControlsTreeChanged(this, new EventArgs());
                }
            }
            else if (e.KeyData == Keys.Down)
            {
                if (this.SelectedControl != null && this.SelectedControl != this.ContainerControl)
                {
                    this.SelectedControl.Top += 1;
                    if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
                }
            }
            else if (e.KeyData == Keys.Left)
            {
                if (this.SelectedControl != null && this.SelectedControl != this.ContainerControl)
                {
                    this.SelectedControl.Left -= 1;
                    if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
                }
            }
            else if (e.KeyData == Keys.Right)
            {
                if (this.SelectedControl != null && this.SelectedControl != this.ContainerControl)
                {
                    this.SelectedControl.Left += 1;
                    if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
                }
            }
            else if (e.KeyData == Keys.Up)
            {
                if (this.SelectedControl != null && this.SelectedControl != this.ContainerControl)
                {
                    this.SelectedControl.Top -= 1;
                    if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
                }
            }
            else if (e.KeyData == (Keys.Up | Keys.Shift))
            {
                if (this.SelectedControl != null && this.SelectedControl != this.ContainerControl)
                {
                    this.SelectedControl.Size = new SizeF(this.SelectedControl.Size.Width, this.SelectedControl.Size.Height - 1);
                    if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
                }
            }
            else if (e.KeyData == (Keys.Down | Keys.Shift))
            {
                if (this.SelectedControl != null && this.SelectedControl != this.ContainerControl)
                {
                    this.SelectedControl.Size = new SizeF(this.SelectedControl.Size.Width, this.SelectedControl.Size.Height + 1);
                    if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
                }
            }
            else if (e.KeyData == (Keys.Left | Keys.Shift))
            {
                if (this.SelectedControl != null && this.SelectedControl != this.ContainerControl)
                {
                    this.SelectedControl.Size = new SizeF(this.SelectedControl.Size.Width - 1, this.SelectedControl.Size.Height);
                    if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
                }
            }
            else if (e.KeyData == (Keys.Right | Keys.Shift))
            {
                if (this.SelectedControl != null && this.SelectedControl != this.ContainerControl)
                {
                    this.SelectedControl.Size = new SizeF(this.SelectedControl.Size.Width + 1, this.SelectedControl.Size.Height);
                    if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
                }
            }
        }

    }
}
