using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExtensionMethods;

namespace SkinnableUI.MetaControls
{
    class MetaDragContainer : MetaControl
    {
        SkinnableControls.SkinnableControl control;
        bool isWindow;

        public MetaDragContainer(SkinnableView parentView) : base(parentView)
        {
           
        }

        public SkinnableControls.SkinnableControl Control
        {
            get { return this.control; }
            set { this.control = value; }
        }

        public bool IsWindow
        {
            get { return this.isWindow; }
            set { this.isWindow = value; }
        }

        public override void InvalidateView()
        {
            if (control == null) return;
            parentView.Invalidate(); // FIXME
        }

        public RectangleF GetHandleRectangle()
        {
            var loc = control.GetAbsoluteLocation();
            return new RectangleF(loc.X + 12, loc.Y - 8, 16, 16);
        }

        public override void Paint(Graphics g)
        {
            if (!isWindow)
            {
                var rect = GetHandleRectangle();
                var img = new Bitmap(SkinnableUI.Properties.Resources.move);
                g.DrawImage(img, rect.Location);
            }
        }

    }
}
