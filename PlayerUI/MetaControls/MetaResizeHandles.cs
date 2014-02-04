using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExtensionMethods;

namespace PlayerUI.MetaControls
{
    class MetaResizeHandles : MetaControl
    {
        // dimensioni resize handles
        const int HANDLE_W = 6, HANDLE_H = 6;

        PlayerControls.PlayerControl control;
        bool isWindow;

        struct ControlRectangleInfo
        {
            public SizeF size;
            public PointF abslocation;
        }
        ControlRectangleInfo selectedControlOldRect = new ControlRectangleInfo();

        public MetaResizeHandles(PlayerView parentView) : base(parentView)
        {

        }

        public PlayerControls.PlayerControl Control
        {
            get { return this.control; }
            set { this.control = value; }
        }

        public bool IsWindow
        {
            get { return this.isWindow; }
            set { this.isWindow = value; }
        }

        public float ClipRectanglePadding { get; set; }

        public override void InvalidateView()
        {
            if (control == null) return;

            if (isWindow)
            {
                parentView.Invalidate();
                return;
            }

            Rectangle clip;

            // Repaint della vecchia posizione/dimensione del controllo (non necessariamente il solito controllo attualmente selezionato)
            clip = getMetaControlsOuterRectangle(selectedControlOldRect.abslocation, selectedControlOldRect.size).RoundUp();
            parentView.Invalidate(clip);

            // Repaint della nuova posizione/dimensione del controllo
            var absloc = control.GetAbsoluteLocation();
            clip = getMetaControlsOuterRectangle(absloc, control.Size).RoundUp();
            parentView.Invalidate(clip);

            selectedControlOldRect.abslocation = absloc;
            selectedControlOldRect.size = control.Size;

        }

        public override void Paint(Graphics g)
        {
            drawSelectionMetacontrols(g);
        }

        private void drawSelectionMetacontrols(Graphics g)
        {
            if (control != null)
            {
                var selectedControlPos = control.GetAbsoluteLocation();

                RectangleF[] resizeHandles = getResizeHandlesRectangles(selectedControlPos, control.Size);

                // Linee tratteggiate
                Pen selectionPen = new Pen(Color.Black);
                selectionPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                g.DrawRectangle(selectionPen, selectedControlPos.X - (HANDLE_W / 2), selectedControlPos.Y - (HANDLE_H / 2), control.Size.Width + HANDLE_W - 1, control.Size.Height + HANDLE_H - 1);

                // Handles
                RectangleF[] handlesToDraw = resizeHandles;
                if (isWindow)
                    handlesToDraw = new RectangleF[] { resizeHandles[4], resizeHandles[6], resizeHandles[7] };

                foreach (RectangleF handle in handlesToDraw)
                {
                    g.FillRectangle(Brushes.White, handle);
                    g.DrawRectangle(Pens.Black, handle.X, handle.Y, handle.Width, handle.Height);
                }
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
            ).Expand(ClipRectanglePadding);
        }

        /// <summary>
        /// Dato un punto, determina quale resize handle è disponibile su quel punto.
        /// </summary>
        /// <param name="p">Punto in coordinate relative alla View</param>
        /// <returns></returns>
        public Direction WhatResizeHandle(PointF p)
        {
            PlayerControls.PlayerControl c = this.control;
            if (c == null) return Direction.None;

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
    }
}
