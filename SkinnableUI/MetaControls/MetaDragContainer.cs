/*
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
