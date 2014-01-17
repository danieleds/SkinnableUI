using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using UnusefulPlayer.PlayerControls;
using ExtensionMethods;

namespace UnusefulPlayer
{
    public class PlayerViewDesigner : PlayerView
    {
        public bool DebugShowPaints { get; set; }
        public bool DebugShowRuler { get; set; }
        public bool DrawWindowDecorations { get; set; }

        // dimensioni resize handles
        const int HANDLE_W = 6, HANDLE_H = 6;

        /// <summary>
        /// Indica di quanto estendere l'area del rettangolo da invalidare per quando si ridisegna un controllo.
        /// Valori più alti permettono di evitare tagli non voluti quando si sposta o si ridimensiona velocemente il controllo.
        /// </summary>
        const float CONTROL_CLIP_RECTANGLE_PADDING = 10;

        // Variabili helper per il dragging in design mode
        PlayerControl draggingControl;
        Point draggingPosition; // Posizione corrente del puntatore (coordinate relative a this). Ha senso solo se draggingControl != null.
        Bitmap draggingBitmap; // Ha senso solo se draggingControl != null.
        bool showDraggingBitmap; // Se false, la bitmap che viene mostrata durante il dragging viene nascosta. Ha senso solo se draggingControl != null.
        Container draggingControlOriginalContainer; // Il Container in cui si trovava il controllo prima del dragging. Ha senso solo se draggingControl != null.
        PointF draggingOffset; // Ha senso solo se draggingControl != null o se dragStarting = true.
        bool dragStarting = false; // true se è stato fatto un MouseDown e stiamo aspettando un delta di spostamento sufficiente.
        Point dragStartPosition; // Posizione del MouseDown iniziale (coordinate relative alla finestra). Ha senso solo se draggingControl != null.

        struct ControlRectangleInfo
        {
            public SizeF size;
            public PointF abslocation;
        }

        ControlRectangleInfo selectedControlOldRect = new ControlRectangleInfo();

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
            DebugShowRuler = false;
            DrawWindowDecorations = false;
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

                        selectedControl_MetaControlsNeedRepaint(selectedControl, new EventArgs());
                    }

                    selectedControl = value;
                    
                    if (selectedControl != null)
                    {
                        selectedControl.Resize += selectedControl_MetaControlsNeedRepaint;
                        selectedControl.Move += selectedControl_MetaControlsNeedRepaint;

                        selectedControl_MetaControlsNeedRepaint(selectedControl, new EventArgs());
                    }
                    
                    if (SelectionChanged != null) SelectionChanged(this, new EventArgs());
                }
            }
        }

        /// <summary>
        /// Event handler che viene chiamato quando è necessario ridisegnare i metacontrolli
        /// (ad esempio, i resize handle).
        /// Viene chiamato quando il controllo selezionato viene ridimensionato o spostato, o al
        /// MouseUp dopo un ridimensionamento (per nascondere i righelli).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void selectedControl_MetaControlsNeedRepaint(object sender, EventArgs e)
        {
            if (selectedControl == this.containerControl)
            {
                this.Invalidate();
                return;
            }

            // FIXME Il clipping funziona, ma non considera i righelli (che vengono tagliati fuori)...
            // questa è una mancanza (da fixare) di getMetaControlsOuterRectangle().
            // Per ora risolviamo invalidando tutto, correggere non ne vale la pena (bisognerebbe
            // effettuare misurazioni del testo, calcolare rotazioni, ecc). Il problema comunque non
            // è grave visto che è limitato al designer, e al momento non causa problemi di performance.

            if (DebugShowRuler)
            {
                this.Invalidate();
            }
            else
            {
                Rectangle clip;
                
                // Repaint della vecchia posizione/dimensione del controllo (non necessariamente il solito controllo attualmente selezionato)
                clip = getMetaControlsOuterRectangle(selectedControlOldRect.abslocation, selectedControlOldRect.size).RoundUp();
                this.Invalidate(clip);

                // Repaint della nuova posizione/dimensione del controllo
                var absloc = selectedControl.GetAbsoluteLocation();
                clip = getMetaControlsOuterRectangle(absloc, selectedControl.Size).RoundUp();
                this.Invalidate(clip);

                selectedControlOldRect.abslocation = absloc;
                selectedControlOldRect.size = selectedControl.Size;
            }
        }

        /// <summary>
        /// Dato un controllo e un punto, determina quale resize handle è disponibile su quel punto.
        /// </summary>
        /// <param name="c">Controllo</param>
        /// <param name="p">Punto</param>
        /// <returns></returns>
        private Direction WhatResizeHandle(PlayerControl c, PointF p)
        {
            /*RectangleF[] resizeHandles = getResizeHandlesRectangles(c.GetAbsoluteLocation(), c.Size);
            var match = (from h in resizeHandles
                        where p.X >= h.Location.X && p.X <= h.Location.X + h.Width
                        && p.Y >= h.Location.Y && p.Y <= h.Location.Y + h.Height
                        select h).FirstOrDefault();

            if (match == resizeHandles[0]) return Direction.Up | Direction.Left;
            else if (match == resizeHandles[1]) return Direction.Up;
            else if (match == resizeHandles[2]) return Direction.Up | Direction.Right;
            else if (match == resizeHandles[3]) return Direction.Left;
            else if (match == resizeHandles[4]) return Direction.Right;
            else if (match == resizeHandles[5]) return Direction.Down | Direction.Left;
            else if (match == resizeHandles[6]) return Direction.Down;
            else if (match == resizeHandles[7]) return Direction.Down | Direction.Right;
            else return Direction.None;*/

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
            float absX = closerContainer.Left, absY = closerContainer.Top;
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
                    if (tmp is PlayerControls.Container)
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

            if (DrawWindowDecorations)
                drawWindowDecorations(e.Graphics);
            
            // Disegna il rettangolo di selezione
            if (selectedControl != null)
                drawSelectionMetacontrols(e.Graphics);

            if (DebugShowRuler && resizingControl != null)
                drawResizingMeasure(e.Graphics);

            if (draggingControl != null && showDraggingBitmap)
            {
                e.Graphics.DrawImageUnscaled(this.draggingBitmap, this.draggingPosition.X - (int)this.draggingOffset.X, this.draggingPosition.Y - (int)this.draggingOffset.Y);
            }

            if (DebugShowPaints)
            {
                var b = new SolidBrush(Color.FromArgb(60, rand.Next(255), rand.Next(255), rand.Next(255)));
                e.Graphics.FillRectangle(b, e.ClipRectangle);
            }
        }

        RectangleF[] getResizeHandlesRectangles(PointF controlAbsoluteLocation, SizeF controlSize)
        {
            RectangleF[] resizeHandles = {
                    new RectangleF(controlAbsoluteLocation.X - HANDLE_W, controlAbsoluteLocation.Y - HANDLE_H, HANDLE_W, HANDLE_H),
                    new RectangleF(controlAbsoluteLocation.X + (controlSize.Width / 2) - (HANDLE_W / 2), controlAbsoluteLocation.Y - HANDLE_H, HANDLE_W, HANDLE_H),
                    new RectangleF(controlAbsoluteLocation.X + controlSize.Width - 1, controlAbsoluteLocation.Y - HANDLE_H, HANDLE_W, HANDLE_H),

                    new RectangleF(controlAbsoluteLocation.X - HANDLE_W, controlAbsoluteLocation.Y + (controlSize.Height / 2) - (HANDLE_H / 2), HANDLE_W, HANDLE_H),
                    new RectangleF(controlAbsoluteLocation.X + controlSize.Width - 1, controlAbsoluteLocation.Y + (controlSize.Height / 2) - (HANDLE_H / 2), HANDLE_W, HANDLE_H),

                    new RectangleF(controlAbsoluteLocation.X - HANDLE_W, controlAbsoluteLocation.Y + controlSize.Height - 1, HANDLE_W, HANDLE_H),
                    new RectangleF(controlAbsoluteLocation.X + (controlSize.Width / 2) - (HANDLE_W / 2), controlAbsoluteLocation.Y + controlSize.Height - 1, HANDLE_W, HANDLE_H),
                    new RectangleF(controlAbsoluteLocation.X + controlSize.Width - 1, controlAbsoluteLocation.Y + controlSize.Height - 1, HANDLE_W, HANDLE_H)
                };

            return resizeHandles;
        }

        /// <summary>
        /// Restituisce il rettangolo che inscrive tutti i metacontrolli (resize handles, righelli).
        /// </summary>
        /// <returns></returns>
        RectangleF getMetaControlsOuterRectangle(PointF controlAbsoluteLocation, SizeF controlSize)
        {
            var resizeHandles = getResizeHandlesRectangles(controlAbsoluteLocation, controlSize);

            var topLeftHnd = resizeHandles[0];
            var bottomRightHnd = resizeHandles[7];
            // FIXME Includere righelli!!!
            return new RectangleF(
                topLeftHnd.X,
                topLeftHnd.Y,
                bottomRightHnd.X + bottomRightHnd.Width - topLeftHnd.X + 1,
                bottomRightHnd.Y + bottomRightHnd.Height - topLeftHnd.Y + 1
            ).Expand(CONTROL_CLIP_RECTANGLE_PADDING);
        }

        private System.Drawing.Drawing2D.GraphicsPath getWindowDecorationsPath()
        {
            RectangleF clientArea = new RectangleF(this.containerControl.Location, this.containerControl.Size);
            float titleHeight = 25;
            float border = 5;

            var gp = new System.Drawing.Drawing2D.GraphicsPath();
            gp.AddRectangle(new RectangleF(clientArea.X - 1, clientArea.Y - 1, clientArea.Width + 1, clientArea.Height + 1));
            gp.AddRectangle(new RectangleF(clientArea.X - border - 1, clientArea.Y - titleHeight - 1, clientArea.Width + 2*border + 1, clientArea.Height + titleHeight + border + 1));

            return gp;
        }

        private void drawWindowDecorations(Graphics g)
        {
            if(this.containerControl != null) {
                var brushBack = new SolidBrush(Color.FromArgb(255, 95, 186, 207));
                var penBorder = new Pen(Color.FromArgb(255, 79, 150, 170));

                var path = getWindowDecorationsPath();
                g.FillPath(brushBack, path);
                g.DrawPath(penBorder, path);
            }
        }

        private void drawSelectionMetacontrols(Graphics g)
        {
            if (selectedControl != null)
            {
                var selectedControlPos = selectedControl.GetAbsoluteLocation();

                RectangleF[] resizeHandles = getResizeHandlesRectangles(selectedControlPos, selectedControl.Size);

                // Linee tratteggiate
                Pen selectionPen = new Pen(Color.Black);
                selectionPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                g.DrawRectangle(selectionPen, selectedControlPos.X - (HANDLE_W / 2), selectedControlPos.Y - (HANDLE_H / 2), selectedControl.Size.Width + HANDLE_W - 1, selectedControl.Size.Height + HANDLE_H - 1);

                // Handles
                RectangleF[] handlesToDraw = resizeHandles;
                if (selectedControl == this.ContainerControl)
                    handlesToDraw = new RectangleF[] { resizeHandles[4], resizeHandles[6], resizeHandles[7] };

                foreach (RectangleF handle in handlesToDraw)
                {
                    g.FillRectangle(Brushes.White, handle);
                    g.DrawRectangle(Pens.Black, handle.X, handle.Y, handle.Width, handle.Height);
                }
            }
        }

        private void drawResizingMeasure(Graphics g)
        {
            if (resizingControl != null)
            {
                var loc = resizingControl.GetAbsoluteLocation();

                bool drawUp = (resizingDirection & Direction.Up) != Direction.Up;
                bool drawLeft = (resizingDirection & Direction.Left) != Direction.Left;
                bool drawDown = (resizingDirection & Direction.Down) != Direction.Down;
                bool drawRight = (resizingDirection & Direction.Right) != Direction.Right;
                if (drawLeft && drawRight) drawRight = false;
                if (drawUp && drawDown) drawDown = false;

                if (drawUp) drawSingleResizingMeasure(g, loc, resizingControl.Size, Direction.Up);
                if (drawLeft) drawSingleResizingMeasure(g, loc, resizingControl.Size, Direction.Left);
                if (drawDown) drawSingleResizingMeasure(g, loc, resizingControl.Size, Direction.Down);
                if (drawRight) drawSingleResizingMeasure(g, loc, resizingControl.Size, Direction.Right);
            }
        }

        private void drawSingleResizingMeasure(Graphics g, PointF controlAbsoluteLocation, SizeF controlSize, Direction direction)
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

            float barHeight = strSize.Height + textVerticalShift;
            g.DrawLine(Pens.Blue, loc.X, loc.Y, loc.X, loc.Y - barHeight);
            g.DrawLine(Pens.Blue, loc.X + size.Width, loc.Y, loc.X + size.Width, loc.Y - barHeight);

            int realTextVerticalShift = (direction == Direction.Down || direction == Direction.Right) ? 0 : textVerticalShift;

            if (strSize.Width + textLateralMargin + 15 < size.Width)
            {
                // Non c'è spazio a disposizione: spostiamo il testo più all'esterno
                PointF strPos = new PointF(loc.X + size.Width / 2 - strSize.Width / 2, loc.Y - strSize.Height - realTextVerticalShift);

                g.DrawLine(Pens.Blue, loc.X, loc.Y - strSize.Height / 2 - realTextVerticalShift, strPos.X - textLateralMargin, loc.Y - strSize.Height / 2 - realTextVerticalShift);
                g.DrawLine(Pens.Blue, strPos.X + strSize.Width + textLateralMargin, loc.Y - strSize.Height / 2 - realTextVerticalShift, loc.X + size.Width, loc.Y - strSize.Height / 2 - realTextVerticalShift);

                g.DrawString(size.Width.ToString(), this.Font, Brushes.Blue, strPos);
            }
            else
            {
                float inverseShift = 0;
                if (direction == Direction.Down || direction == Direction.Right)
                    inverseShift = 2 * strSize.Height;

                PointF strPos = new PointF(loc.X + size.Width / 2 - strSize.Width / 2, loc.Y - 2 * strSize.Height - realTextVerticalShift + inverseShift);

                g.DrawLine(Pens.Blue, loc.X, loc.Y - strSize.Height / 2 - realTextVerticalShift, loc.X + size.Width, loc.Y - strSize.Height / 2 - realTextVerticalShift);

                g.DrawString(size.Width.ToString(), this.Font, Brushes.Blue, strPos);
            }

            g.Transform = t;
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);

            this.showDraggingBitmap = true;
            
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
                    this.showDraggingBitmap = info != null;
                    if (info != null)
                    {
                        var differentContainer = (info.Item3 != this.draggingControlOriginalContainer);
                        if(differentContainer)
                            e.Effect = DragDropEffects.Move;
                        else
                            e.Effect = DragDropEffects.Scroll; // Usiamo Scroll per rappresentare il fatto che stiamo spostando all'interno dello stesso contenitore originale.
                        var oldDraggingPosition = this.draggingPosition;
                        this.draggingPosition = this.PointToClient(new Point(e.X, e.Y));
                        if (this.draggingPosition != oldDraggingPosition) {
                            this.Invalidate(new RectangleF(oldDraggingPosition.X - draggingOffset.X, oldDraggingPosition.Y - draggingOffset.Y, draggingControl.Size.Width, draggingControl.Size.Height).Expand(CONTROL_CLIP_RECTANGLE_PADDING).RoundUp());
                            this.Invalidate(new RectangleF(draggingPosition.X - draggingOffset.X, draggingPosition.Y - draggingOffset.Y, draggingControl.Size.Width, draggingControl.Size.Height).Expand(CONTROL_CLIP_RECTANGLE_PADDING).RoundUp());
                        }
                    }
                    else
                    {
                        e.Effect = DragDropEffects.None;
                        this.Invalidate(new RectangleF(draggingPosition.X - draggingOffset.X, draggingPosition.Y - draggingOffset.Y, draggingControl.Size.Width, draggingControl.Size.Height).Expand(CONTROL_CLIP_RECTANGLE_PADDING).RoundUp());
                    }
                }
                else e.Effect = DragDropEffects.None;
            }
        }

        protected override void OnDragLeave(EventArgs e)
        {
            base.OnDragLeave(e);

            this.showDraggingBitmap = false;
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
                if (hitInfo.Item3 is Container)
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
            if (selectedControl != null)
            {
                Direction resizeDir = WhatResizeHandle(selectedControl, e.Location);
                if (resizeDir != Direction.None)
                {
                    if (selectedControl != this.containerControl ||
                        ((resizeDir & Direction.Left) != Direction.Left // containerControl non può ridimensionarsi a sx
                        && (resizeDir & Direction.Up) != Direction.Up)) // containerControl non può ridimensionarsi in alto
                    {
                        this.resizingControl = selectedControl;
                        this.resizingDirection = resizeDir;
                    }
                }
            }

            // Gestione selezione e dragging (se non è stato cliccato un resize handle)
            if (resizingControl == null)
            {
                if (DrawWindowDecorations && getWindowDecorationsPath().IsVisible(e.Location))
                {
                    // E' stata cliccata la decorazione della finestra
                    this.SelectedControl = this.containerControl;
                }
                else
                {
                    var hitInfo = RecursiveHitTest(e.X, e.Y);
                    if (hitInfo != null && hitInfo.Item3 != this.containerControl)
                    {
                        PlayerControl ctl = hitInfo.Item3;
                        this.SelectedControl = ctl;

                        this.dragStarting = true;
                        this.dragStartPosition = e.Location;
                        this.draggingOffset.X = e.X - hitInfo.Item1;
                        this.draggingOffset.Y = e.Y - hitInfo.Item2;
                    }
                    else if (hitInfo != null && hitInfo.Item3 == this.containerControl)
                    {
                        this.SelectedControl = this.containerControl;
                    }
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

                    this.DoDragDrop(new DataObject(typeof(PlayerControl).FullName, this.draggingControl), DragDropEffects.Move | DragDropEffects.Scroll);

                }
            }

            if (draggingControl == null && resizingControl == null)
            {
                // Non stiamo né draggando né ridimensionando.

                bool actionSet = false;

                // Controlliamo se siamo sopra a un resize handle
                if (selectedControl != null)
                {
                    var cc = selectedControl == this.containerControl;
                    Direction resizeDir = WhatResizeHandle(selectedControl, e.Location);
                    actionSet = true;
                    if ((resizeDir == Direction.Left && !cc) || resizeDir == Direction.Right)
                        this.Cursor = Cursors.SizeWE;
                    else if ((resizeDir == Direction.Up && !cc) || resizeDir == Direction.Down)
                        this.Cursor = Cursors.SizeNS;
                    else if ((resizeDir == (Direction.Up | Direction.Right) && !cc) || (resizeDir == (Direction.Down | Direction.Left) && !cc))
                        this.Cursor = Cursors.SizeNESW;
                    else if ((resizeDir == (Direction.Up | Direction.Left) && !cc) || resizeDir == (Direction.Down | Direction.Right))
                        this.Cursor = Cursors.SizeNWSE;
                    else
                        actionSet = false;
                }

                if (!actionSet)
                    this.Cursor = Cursors.Default;
            }

            if (resizingControl != null)
            {
                const int minWidth = 11, minHeight = 11;
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
                selectedControl_MetaControlsNeedRepaint(selectedControl, new EventArgs());
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
