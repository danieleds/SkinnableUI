using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlayerUI.PlayerControls;
using ExtensionMethods;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PlayerUI
{
    public class PlayerViewDesigner : PlayerView
    {
        public bool DebugShowPaints { get; set; }
        public bool DebugShowRuler { get; set; }
        public bool DrawWindowDecorations { get; set; }

        /// <summary>
        /// Indica di quanto estendere l'area del rettangolo da invalidare per quando si ridisegna un controllo.
        /// Valori più alti permettono di evitare tagli non voluti quando si sposta o si ridimensiona velocemente il controllo.
        /// </summary>
        const float CONTROL_CLIP_RECTANGLE_PADDING = 10;

        const string CLIPBOARD_PLAYERCONTROLS_FORMAT = "skinPlayerControl";

        // Variabili helper per il dragging in design mode
        PlayerControl draggingControl;
        Point draggingPosition; // Posizione corrente del puntatore (coordinate relative a this). Ha senso solo se draggingControl != null.
        Bitmap draggingBitmap; // Ha senso solo se draggingControl != null.
        bool showDraggingBitmap; // Se false, la bitmap che viene mostrata durante il dragging viene nascosta. Ha senso solo se draggingControl != null.
        Container draggingControlOriginalContainer; // Il Container in cui si trovava il controllo prima del dragging. Ha senso solo se draggingControl != null.
        PointF draggingOffset; // Ha senso solo se draggingControl != null o se dragStarting = true.
        bool dragStarting = false; // true se è stato fatto un MouseDown e stiamo aspettando un delta di spostamento sufficiente.
        Point dragStartPosition; // Posizione del MouseDown iniziale (coordinate relative alla finestra). Ha senso solo se draggingControl != null.

        PlayerControl resizingControl; // Il controllo che stiamo ridimensionando.
        Direction resizingDirection; // La direzione in cui stiamo ridimensionando resizingControl. Ha senso solo se resizingControl != null.

        Collection<PlayerControl> selectedControls = new Collection<PlayerControl>();
        Dictionary<PlayerControl, MetaControls.MetaResizeHandles> selectionResizeHandles;
        Dictionary<PlayerControl, MetaControls.MetaMeasure> resizeMeasure;
        Dictionary<PlayerControl, MetaControls.MetaDragContainer> dragContainerHandles;

        bool selectingWithMouse = false; // Indica se stiamo trascinando il mouse per tracciare un rettangolo di selezione.
        PointF selectionStartPoint, selectionEndPoint; // Punti di inizio e fine del rettangolo di selezione. Hanno senso solo se selectingWithMouse != null.
        Container selectionStartContainer; // Container in cui stiamo tracciando il rettangolo di selezione. Ha senso solo se selectingWithMouse != null.

        private Random rand = new Random();

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
            selectionResizeHandles = new Dictionary<PlayerControl, MetaControls.MetaResizeHandles>();
            resizeMeasure = new Dictionary<PlayerControl, MetaControls.MetaMeasure>();
            dragContainerHandles = new Dictionary<PlayerControl, MetaControls.MetaDragContainer>();

            DebugShowPaints = false;
            DebugShowRuler = false;
            DrawWindowDecorations = false;
        }

        /// <summary>
        ///  Seleziona i controlli specificati.
        /// </summary>
        /// <param name="controls"></param>
        public void SelectMultiple(Collection<PlayerControl> controls)
        {
            // Rimuove i vecchi handler
            foreach (var ctl in this.selectedControls)
            {
                ctl.Resize -= selectedControl_MetaControlsNeedRepaint;
                ctl.Move -= selectedControl_MetaControlsNeedRepaint;

                selectedControl_MetaControlsNeedRepaint(ctl, new EventArgs());
            }

            selectedControls = new Collection<PlayerControl>();

            if (controls != null)
            {
                foreach (var ctl in controls.Distinct())
                {
                    selectedControls.Add(ctl);

                    ctl.Resize += selectedControl_MetaControlsNeedRepaint;
                    ctl.Move += selectedControl_MetaControlsNeedRepaint;

                    selectedControl_MetaControlsNeedRepaint(ctl, new EventArgs());
                }
            }

            if (SelectionChanged != null) SelectionChanged(this, new EventArgs());
        }

        public void Select(PlayerControl control)
        {
            if (control == null)
            {
                SelectMultiple(null);
            }
            else
            {
                var tmp = new Collection<PlayerControl>();
                tmp.Add(control);
                SelectMultiple(tmp);
            }
        }

        public void AddToSelection(PlayerControl control)
        {
            var tmp = this.selectedControls.ToList();
            tmp.Add(control);
            SelectMultiple(new Collection<PlayerControl>(tmp));
        }

        public bool ToggleSelection(PlayerControl control)
        {
            if (this.selectedControls.Contains(control))
            {
                Deselect(control);
                return false;
            }
            else
            {
                AddToSelection(control);
                return true;
            }
        }

        public void Deselect(PlayerControl control)
        {
            if (control != null)
            {
                var tmp = this.selectedControls.ToList();
                tmp.Remove(control);
                SelectMultiple(new Collection<PlayerControl>(tmp));
            }
        }

        public ReadOnlyCollection<PlayerControl> SelectedControls
        {
            get { return this.selectedControls.ToList().AsReadOnly(); }
        }

        private Color designerBackColor = SystemColors.Control;
        public Color DesignerBackColor
        {
            get { return designerBackColor; }
            set { designerBackColor = value; this.Invalidate(); }
        }

        /// <summary>
        /// Event handler che viene chiamato quando è necessario ridisegnare i metacontrolli
        /// (ad esempio, i resize handle).
        /// Viene chiamato quando il controllo selezionato viene ridimensionato o spostato, o al
        /// MouseUp dopo un ridimensionamento (per nascondere i righelli).
        /// </summary>
        /// <param name="sender">Il controllo i cui metacontrolli devono essere ridisegnati.</param>
        /// <param name="e"></param>
        void selectedControl_MetaControlsNeedRepaint(object sender, EventArgs e)
        {
            if (!(sender is PlayerControl))
                return;

            PlayerControl control = (PlayerControl)sender;

            
            // Handles per il resize

            MetaControls.MetaResizeHandles resizeHandles = new MetaControls.MetaResizeHandles(this);
            resizeHandles.Control = control;
            resizeHandles.IsWindow = this.containerControl == resizeHandles.Control;
            resizeHandles.ClipRectanglePadding = CONTROL_CLIP_RECTANGLE_PADDING;

            selectionResizeHandles.Remove(control);
            selectionResizeHandles.Add(control, resizeHandles);
            resizeHandles.InvalidateView();


            // Handle per il drag dei Container

            MetaControls.MetaDragContainer dragContainer = new MetaControls.MetaDragContainer(this);
            dragContainer.Control = control;
            dragContainer.IsWindow = this.containerControl == dragContainer.Control;

            dragContainerHandles.Remove(control);
            dragContainerHandles.Add(control, dragContainer);
            dragContainer.InvalidateView();


            // FIXME Il clipping funziona, ma non considera i righelli (che vengono tagliati fuori)...
            // questa è una mancanza (da fixare) di getMetaControlsOuterRectangle() in InvalidateView().
            // Per ora risolviamo invalidando tutto, correggere non ne vale la pena (bisognerebbe
            // effettuare misurazioni del testo, calcolare rotazioni, ecc). Il problema comunque non
            // è grave visto che è limitato al designer, e al momento non causa problemi di performance.

            if (DebugShowRuler)
            {
                MetaControls.MetaMeasure measure = new MetaControls.MetaMeasure(this);
                measure.Control = control;
                measure.Font = this.Font;
                measure.MeasureDirection = MetaControls.MetaMeasure.ResizeDirectionToMeasureDirection(this.resizingDirection);

                resizeMeasure.Remove(control);
                resizeMeasure.Add(control, measure);
                measure.InvalidateView();
            }
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
            this.Select(null);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(this.DesignerBackColor), 0, 0, this.Width, this.Height);
            e.Graphics.FillRectangle(new SolidBrush(this.BackColor), new RectangleF(this.containerControl.Location, this.containerControl.Size));

            base.OnPaint(e);

            if (DrawWindowDecorations)
                drawWindowDecorations(e.Graphics);
            
            
            foreach (var ctl in selectedControls)
            {
                // Disegna il rettangolo di selezione
                selectionResizeHandles[ctl].Paint(e.Graphics);

                // Disegna l'handle per spostare i Container
                if (ctl is PlayerControls.Container)
                    dragContainerHandles[ctl].Paint(e.Graphics);
            }

            if (DebugShowRuler && resizingControl != null)
            {
                foreach (var ctl in selectedControls)
                    resizeMeasure[ctl].Paint(e.Graphics);
            }

            if (draggingControl != null && showDraggingBitmap)
            {
                e.Graphics.DrawImageUnscaled(this.draggingBitmap, this.draggingPosition.X - (int)this.draggingOffset.X, this.draggingPosition.Y - (int)this.draggingOffset.Y);
            }

            if (selectingWithMouse)
            {
                drawSelectionRectangle(e.Graphics);
            }

            if (DebugShowPaints)
            {
                var b = new SolidBrush(Color.FromArgb(60, rand.Next(255), rand.Next(255), rand.Next(255)));
                e.Graphics.FillRectangle(b, e.ClipRectangle);
            }
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

        private void drawSelectionRectangle(Graphics g)
        {
            Pen selectionPen = new Pen(Color.Black);
            selectionPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            g.DrawRectangle(
                    selectionPen,
                    Math.Min(selectionStartPoint.X, selectionEndPoint.X),
                    Math.Min(selectionStartPoint.Y, selectionEndPoint.Y),
                    Math.Abs(selectionStartPoint.X - selectionEndPoint.X),
                    Math.Abs(selectionStartPoint.Y - selectionEndPoint.Y));
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
                    this.Select(info.Item3);
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
                    this.draggingControlOriginalContainer.Controls.Add(draggingControl);
                    this.Select(draggingControl);
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

                    dropInfo.Item3.Controls.AddFirst(c);
                    var location = this.PointToClient(new Point(e.X, e.Y));
                    // Non posizioniamo sotto al mouse le coordinate (0,0) del controllo: lo spostiamo un pochino in alto e a sinistra.
                    const float mouseOffsetX = 7, mouseOffsetY = 7;
                    c.Location = new PointF(Math.Max(0, location.X - dropInfo.Item1 - mouseOffsetX), Math.Max(0, location.Y - dropInfo.Item2 - mouseOffsetY));

                    if (DesignerControlsTreeChanged != null) DesignerControlsTreeChanged(this, new EventArgs());
                    this.Select(c);

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
                    this.Select(null);
                }

                var dropInfo = ControlDropAllowed(this.PointToClient(new Point(e.X, e.Y)), false);
                if (dropInfo != null)
                {
                    var newParent = dropInfo.Item3;
                    if (newParent != c)
                    {
                        newParent.Controls.AddFirst(c);
                    }
                    var location = this.PointToClient(new Point(e.X, e.Y));
                    c.Location = new PointF(location.X - this.draggingOffset.X - dropInfo.Item1, location.Y - this.draggingOffset.Y - dropInfo.Item2);
                    this.draggingControl = null;
                    if (DesignerControlsTreeChanged != null) DesignerControlsTreeChanged(this, new EventArgs());
                    this.Select(c);
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

        private DataObject GetDataObject(string format, ReadOnlyCollection<PlayerControl> controls)
        {
            var box = new Collection<SerializationHelper.SerializablePlayerControl>();
            foreach (var c in controls)
            {
                var resources = new Dictionary<string, System.IO.MemoryStream>();
                var doc = new System.Xml.XmlDocument();
                doc.AppendChild(c.GetXmlElement(doc, resources));

                var data = new SerializationHelper.SerializablePlayerControl();
                data.XmlDocument = doc;
                data.Resources = resources;

                box.Add(data);
            }

            return new DataObject(CLIPBOARD_PLAYERCONTROLS_FORMAT, box);
        }

        public bool CanPasteFromClipboard()
        {
            return (Clipboard.ContainsData(CLIPBOARD_PLAYERCONTROLS_FORMAT));
        }

        public void CopyControlsToClipboard(ReadOnlyCollection<PlayerControl> c)
        {
            Clipboard.SetDataObject(GetDataObject(CLIPBOARD_PLAYERCONTROLS_FORMAT, c), true);
        }

        public void PasteControlsFromClipboard(Container where)
        {
            if (CanPasteFromClipboard())
            {
                var box = Clipboard.GetDataObject().GetData(CLIPBOARD_PLAYERCONTROLS_FORMAT) as Collection<SerializationHelper.SerializablePlayerControl>;
                if (box == null)
                    return;

                var added = new Collection<PlayerControl>();
                foreach (var clipb in box)
                {
                    System.Xml.XmlDocument copy_xml = clipb.XmlDocument;
                    Dictionary<string, System.IO.MemoryStream> copy_resources = clipb.Resources;

                    var controlElement = copy_xml.ChildNodes[1];

                    PlayerControls.PlayerControl copy = SerializationHelper.GetPlayerControlInstanceFromTagName(controlElement.Name);
                    copy.ParentView = this;
                    copy.FromXmlElement((System.Xml.XmlElement)controlElement, copy_resources);

                    copy.Parent = where;
                    copy.Location = new PointF(copy.Location.X + 15, copy.Location.Y + 15);

                    added.Add(copy);
                }

                this.SelectMultiple(added);
                if (DesignerControlsTreeChanged != null) DesignerControlsTreeChanged(this, new EventArgs());
            }
        }

        public void CutControlsToClipboard(ReadOnlyCollection<PlayerControl> c)
        {
            CopyControlsToClipboard(c);

            foreach (var ctl in c)
            {
                ctl.Parent = null;
                this.Deselect(ctl);
            }

            if (DesignerControlsTreeChanged != null) DesignerControlsTreeChanged(this, new EventArgs());
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Gestione handle di spostamento Container
            foreach (var ctl in selectedControls)
            {
                if (ctl != this.containerControl)
                {
                    var rect = dragContainerHandles[ctl].GetHandleRectangle();
                    if (rect.Contains(e.Location))
                    {
                        var absLoc = ctl.GetAbsoluteLocation();

                        this.dragStarting = true;
                        this.dragStartPosition = e.Location;
                        this.draggingOffset.X = e.X - absLoc.X;
                        this.draggingOffset.Y = e.Y - absLoc.Y;
                        break;
                    }
                }
            }

            // Gestione resize handle (se non sta partendo il drag)
            if (!dragStarting)
            {
                foreach (var ctl in selectedControls)
                {
                    var resizeHandles = selectionResizeHandles[ctl];
                    Direction resizeDir = resizeHandles.WhatResizeHandle(e.Location);
                    if (resizeDir != Direction.None)
                    {
                        if (ctl != this.containerControl ||
                            ((resizeDir & Direction.Left) != Direction.Left // containerControl non può ridimensionarsi a sx
                            && (resizeDir & Direction.Up) != Direction.Up)) // containerControl non può ridimensionarsi in alto
                        {
                            this.resizingControl = ctl;
                            this.resizingDirection = resizeDir;
                        }
                    }
                }
            }

            // Gestione selezione e dragging (se non è stato cliccato un resize handle e se non sta partendo un drag)
            if (resizingControl == null && !dragStarting)
            {
                if (DrawWindowDecorations && getWindowDecorationsPath().IsVisible(e.Location))
                {
                    // E' stata cliccata la decorazione della finestra
                    if (ModifierKeys == Keys.Control)
                        this.ToggleSelection(this.containerControl);
                    else 
                        this.Select(this.containerControl);
                }
                else
                {
                    bool startDrag = false;

                    var hitInfo = RecursiveHitTest(e.X, e.Y);
                    if (hitInfo != null && hitInfo.Item3 != this.containerControl)
                    {
                        PlayerControl ctl = hitInfo.Item3;
                        if (ModifierKeys == Keys.Control)
                            this.ToggleSelection(ctl);
                        else
                            this.Select(ctl);

                        startDrag = true;
                    }
                    else if (hitInfo != null && hitInfo.Item3 == this.containerControl)
                    {
                        if (ModifierKeys == Keys.Control)
                            this.ToggleSelection(this.containerControl);
                        else
                            this.Select(this.containerControl);
                    }

                    if (hitInfo != null && hitInfo.Item3 is PlayerControls.Container)
                    {
                        // Fa partire la selezione col mouse
                        this.selectingWithMouse = true;
                        this.selectionStartPoint = e.Location;
                        this.selectionEndPoint = e.Location;
                        this.selectionStartContainer = (Container)hitInfo.Item3;
                        this.Invalidate();
                    }
                    else
                    {
                        if (startDrag)
                        {
                            StartingControlDrag(e.Location, e.X - hitInfo.Item1, e.Y - hitInfo.Item2);
                        }
                    }
                    
                }
            }
        }

        private void StartingControlDrag(Point startPos, float offsetX, float offsetY)
        {
            this.dragStarting = true;
            this.dragStartPosition = startPos;
            this.draggingOffset.X = offsetX;
            this.draggingOffset.Y = offsetY;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (this.dragStarting && this.draggingControl == null && this.selectedControls.Count > 0)
            {
                // Prima di far partire il drag controlliamo se il pulsante premuto è il sinistro e se abbiamo superato il delta di distanza
                var delta = SystemInformation.DragSize;
                if (e.Button == System.Windows.Forms.MouseButtons.Left
                    && (Math.Abs(this.dragStartPosition.X - e.X) >= delta.Width || Math.Abs(this.dragStartPosition.Y - e.Y) >= delta.Height))
                {
                    this.dragStarting = false;

                    // FIXME Fare il drag di tutti i controlli selezionati invece che solo dell'ultimo!
                    var lastctl = this.selectedControls.Last();
                    this.draggingControl = lastctl;
                    this.draggingControlOriginalContainer = draggingControl.Parent;
                    this.draggingBitmap = lastctl.ToBitmap();
                    this.draggingControl.Parent.Controls.Remove(this.draggingControl);
                    this.Select(null);

                    //this.DoDragDrop(GetDataObject(CLIPBOARD_PLAYERCONTROL_FORMAT, this.draggingControl), DragDropEffects.Move | DragDropEffects.Scroll);
                    this.DoDragDrop(new DataObject(typeof(PlayerControl).FullName, this.draggingControl), DragDropEffects.Move | DragDropEffects.Scroll);

                }
            }

            if (selectingWithMouse)
            {
                this.selectionEndPoint = e.Location;
                this.Invalidate();
            }

            if (draggingControl == null && resizingControl == null && selectingWithMouse == false)
            {
                // Non stiamo né draggando né ridimensionando.

                // Controlliamo se siamo sopra a un drag handle di un Container
                bool trovato = false;
                foreach (var ctl in selectedControls)
                {
                    if (ctl != this.containerControl)
                    {
                        var rect = dragContainerHandles[ctl].GetHandleRectangle();
                        if (rect.Contains(e.Location))
                        {
                            trovato = true;
                            this.Cursor = Cursors.SizeAll;
                            break;
                        }
                    }
                }

                if (!trovato)
                {
                    // Controlliamo se siamo sopra a un resize handle
                    bool actionSet = false;
                    foreach (var ctl in selectedControls)
                    {
                        var topctn = ctl == this.containerControl;
                        Direction resizeDir = selectionResizeHandles[ctl].WhatResizeHandle(e.Location);
                        actionSet = true;
                        if ((resizeDir == Direction.Left && !topctn) || resizeDir == Direction.Right)
                            this.Cursor = Cursors.SizeWE;
                        else if ((resizeDir == Direction.Up && !topctn) || resizeDir == Direction.Down)
                            this.Cursor = Cursors.SizeNS;
                        else if ((resizeDir == (Direction.Up | Direction.Right) && !topctn) || (resizeDir == (Direction.Down | Direction.Left) && !topctn))
                            this.Cursor = Cursors.SizeNESW;
                        else if ((resizeDir == (Direction.Up | Direction.Left) && !topctn) || resizeDir == (Direction.Down | Direction.Right))
                            this.Cursor = Cursors.SizeNWSE;
                        else
                            actionSet = false;

                        if (actionSet)
                            break;
                    }

                    if (!actionSet)
                        this.Cursor = Cursors.Default;
                }
            }

            if (resizingControl != null)
            {
                const int minWidth = 11, minHeight = 11;
                var resizingCtrlPos = resizingControl.GetAbsoluteLocation();

                if ((resizingDirection & Direction.Down) == Direction.Down)
                {
                    float heightIncr = (e.Y - resizingCtrlPos.Y) - resizingControl.Size.Height;
                    selectedControls.ForEach(c => c.Size = new SizeF(c.Size.Width, Math.Max(c.Size.Height + heightIncr, minHeight)));
                }
                if ((resizingDirection & Direction.Right) == Direction.Right)
                {
                    float widthIncr = (e.X - resizingCtrlPos.X) - resizingControl.Size.Width;
                    selectedControls.ForEach(c => c.Size = new SizeF(Math.Max(c.Size.Width + widthIncr, minWidth), c.Size.Height));
                }
                if ((resizingDirection & Direction.Up) == Direction.Up)
                {
                    float height = resizingControl.Size.Height + (resizingCtrlPos.Y - e.Y);
                    if (height >= minHeight)
                    {
                        float heightIncr = height - resizingControl.Size.Height;
                        float topIncr = e.Y - resizingCtrlPos.Y;
                        selectedControls.ForEach(c => { c.Top += topIncr; c.Size = new SizeF(c.Size.Width, c.Size.Height + heightIncr); });
                    }
                }
                if ((resizingDirection & Direction.Left) == Direction.Left)
                {
                    float width = resizingControl.Size.Width + (resizingCtrlPos.X - e.X);
                    if (width >= minWidth)
                    {
                        float widthIncr = width - resizingControl.Size.Width;
                        float leftIncr = e.X - resizingCtrlPos.X;
                        selectedControls.ForEach(c => { c.Left += leftIncr; c.Size = new SizeF(c.Size.Width + widthIncr, c.Size.Height); });
                    }
                }

                resizingControl.OnResize(new EventArgs());
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            this.dragStarting = false;

            if (this.selectingWithMouse)
            {
                this.selectingWithMouse = false;

                if (this.selectionStartPoint != this.selectionEndPoint)
                {
                    // Cerchiamo i controlli che cadono nel rettangolo di selezione
                    RectangleF selRect = new RectangleF(
                            Math.Min(selectionStartPoint.X, selectionEndPoint.X),
                            Math.Min(selectionStartPoint.Y, selectionEndPoint.Y),
                            Math.Abs(selectionStartPoint.X - selectionEndPoint.X),
                            Math.Abs(selectionStartPoint.Y - selectionEndPoint.Y));

                    bool foundSome = false;
                    foreach (var ctl in this.selectionStartContainer.Controls)
                    {
                        RectangleF ctlRect = new RectangleF(ctl.GetAbsoluteLocation(), ctl.Size);
                        if (selRect.IntersectsWith(ctlRect))
                        {
                            if (ModifierKeys == Keys.Control)
                            {
                                foundSome |= this.ToggleSelection(ctl);
                            }
                            else
                            {
                                this.AddToSelection(ctl);
                                foundSome = true;
                            }
                        }
                    }

                    // Decidiamo se deselezionare il contenitore oppure no
                    // in base al fatto che sia stato selezionato almeno un figlio
                    // (n.b. questa logica potrebbe benissimo essere migliorata)
                    if (foundSome)
                        this.Deselect(this.selectionStartContainer);
                }

                this.Invalidate();
            }

            if (resizingControl != null)
            {
                resizingControl = null;
                
                foreach (var ctl in selectedControls)
                    selectedControl_MetaControlsNeedRepaint(ctl, new EventArgs());

                if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyData == Keys.Delete)
            {
                foreach (var ctl in selectedControls)
                {
                    if (ctl != this.ContainerControl)
                        ctl.Parent = null;
                }

                this.Select(null);
                if (DesignerControlsTreeChanged != null) DesignerControlsTreeChanged(this, new EventArgs());
            }
            else if (e.KeyData == Keys.Down)
            {
                foreach (var ctl in selectedControls)
                {
                    if (ctl != this.ContainerControl)
                        ctl.Top += 1;
                }
                if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
            }
            else if (e.KeyData == Keys.Left)
            {
                foreach (var ctl in selectedControls)
                {
                    if (ctl != this.ContainerControl)
                        ctl.Left -= 1;
                }
                if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
            }
            else if (e.KeyData == Keys.Right)
            {
                foreach (var ctl in selectedControls)
                {
                    if (ctl != this.ContainerControl)
                        ctl.Left += 1;
                }
                if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
            }
            else if (e.KeyData == Keys.Up)
            {
                foreach (var ctl in selectedControls)
                {
                    if (ctl != this.ContainerControl)
                        ctl.Top -= 1;
                }
                if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
            }
            else if (e.KeyData == (Keys.Up | Keys.Shift))
            {
                foreach (var ctl in selectedControls)
                {
                    if (ctl != this.ContainerControl)
                        ctl.Size = new SizeF(ctl.Size.Width, ctl.Size.Height - 1);
                }
                if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
            }
            else if (e.KeyData == (Keys.Down | Keys.Shift))
            {
                foreach (var ctl in selectedControls)
                {
                    if (ctl != this.ContainerControl)
                        ctl.Size = new SizeF(ctl.Size.Width, ctl.Size.Height + 1);
                }
                if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
            }
            else if (e.KeyData == (Keys.Left | Keys.Shift))
            {
                foreach (var ctl in selectedControls)
                {
                    if (ctl != this.ContainerControl)
                        ctl.Size = new SizeF(ctl.Size.Width - 1, ctl.Size.Height);
                }
                if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
            }
            else if (e.KeyData == (Keys.Right | Keys.Shift))
            {
                foreach (var ctl in selectedControls)
                {
                    if (ctl != this.ContainerControl)
                        ctl.Size = new SizeF(ctl.Size.Width + 1, ctl.Size.Height);
                }
                if (SelectedObjectPropertyChanged != null) SelectedObjectPropertyChanged(this, new EventArgs());
            }
        }

    }
}
