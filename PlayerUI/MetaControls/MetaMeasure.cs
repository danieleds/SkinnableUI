using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExtensionMethods;

namespace PlayerUI.MetaControls
{
    class MetaMeasure : MetaControl
    {
        PlayerControls.PlayerControl control;

        public MetaMeasure(PlayerView parentView) : base(parentView)
        {
           
        }

        public PlayerControls.PlayerControl Control
        {
            get { return this.control; }
            set { this.control = value; }
        }

        public Font Font { get; set; }

        public float ClipRectanglePadding { get; set; }

        public Direction MeasureDirection { get; set; }

        public override void InvalidateView()
        {
            if (control == null) return;
            parentView.Invalidate(); // FIXME
        }

        public static Direction ResizeDirectionToMeasureDirection(Direction resizeDirection)
        {
            bool drawUp = (resizeDirection & Direction.Up) != Direction.Up;
            bool drawLeft = (resizeDirection & Direction.Left) != Direction.Left;
            bool drawDown = (resizeDirection & Direction.Down) != Direction.Down;
            bool drawRight = (resizeDirection & Direction.Right) != Direction.Right;
            if (drawLeft && drawRight) drawRight = false;
            if (drawUp && drawDown) drawDown = false;

            Direction d = new Direction();
            if (drawUp) d |= Direction.Up;
            if (drawLeft) d |= Direction.Left;
            if (drawDown) d |= Direction.Down;
            if (drawRight) d |= Direction.Right;

            return d;
        }

        public override void Paint(Graphics g)
        {
            drawResizingMeasure(g);
        }

        private void drawResizingMeasure(Graphics g)
        {
            if (control != null)
            {
                bool drawUp = (MeasureDirection & Direction.Up) == Direction.Up;
                bool drawLeft = (MeasureDirection & Direction.Left) == Direction.Left;
                bool drawDown = (MeasureDirection & Direction.Down) == Direction.Down;
                bool drawRight = (MeasureDirection & Direction.Right) == Direction.Right;

                var loc = control.GetAbsoluteLocation();

                if (drawUp) drawSingleResizingMeasure(g, loc, control.Size, Direction.Up);
                if (drawLeft) drawSingleResizingMeasure(g, loc, control.Size, Direction.Left);
                if (drawDown) drawSingleResizingMeasure(g, loc, control.Size, Direction.Down);
                if (drawRight) drawSingleResizingMeasure(g, loc, control.Size, Direction.Right);
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
    }
}
