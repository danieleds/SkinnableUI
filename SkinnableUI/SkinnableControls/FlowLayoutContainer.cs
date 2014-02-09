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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections.ObjectModel;

namespace SkinnableUI.SkinnableControls
{
    public class FlowLayoutContainer : Container
    {
        public FlowLayoutContainer(SemanticType c) : base(c)
        {
            this.ControlAdded += FlowLayoutContainer_ControlAdded;
            this.ControlRemoved += FlowLayoutContainer_ControlRemoved;

            this.SetLayout();
        }

        public override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.SetLayout();
        }

        void FlowLayoutContainer_ControlAdded(object sender, SkinnableControlEventArgs e)
        {
            SnapTopToGrid(e.Control);
            AttachUpdateEvents(e.Control);
            this.SetLayout();
        }

        void FlowLayoutContainer_ControlRemoved(object sender, SkinnableControlEventArgs e)
        {
            DetachUpdateEvents(e.Control);
            this.SetLayout();
        }

        void AttachUpdateEvents(SkinnableControl c)
        {
            DetachUpdateEvents(c);
            c.Resize += Control_Resize;
            c.Move += Control_Move;
        }

        void DetachUpdateEvents(SkinnableControl c)
        {
            c.Resize -= Control_Resize;
            c.Move -= Control_Move;
        }

        void Control_Move(object sender, EventArgs e)
        {
            SnapTopToGrid((SkinnableControl)sender);

            this.SetLayout();
        }

        void SnapTopToGrid(SkinnableControl control)
        {
            float minHeightDifference = float.MaxValue;
            SkinnableControl minHeightDifferenceCtl = control;

            foreach (var ctl in this.controls)
            {
                if (ctl == control) continue;
                float topdiff = control.Top - ctl.Top;
                if (topdiff >= 0 && topdiff < minHeightDifference)
                {
                    minHeightDifference = topdiff;
                    minHeightDifferenceCtl = ctl;
                }
            }

            DetachUpdateEvents(control);
            control.Top = minHeightDifferenceCtl.Top;
            AttachUpdateEvents(control);
        }

        void Control_Resize(object sender, EventArgs e)
        {
            this.SetLayout();
        }

        void SetLayout()
        {
            const float hpadding = 5, vpadding = 5;
            float curLeft = 0;
            float curTop = 0;
            float curRowMaxHeight = 0;

            foreach (var ctl in this.controls
                .OrderBy(c => c.Top)
                .ThenBy(c => c.Left))
            {
                if (curLeft + ctl.Size.Width > this.Size.Width)
                {
                    curTop += curRowMaxHeight + vpadding;
                    curRowMaxHeight = 0;
                    curLeft = 0;
                }

                DetachUpdateEvents(ctl);

                ctl.Top = curTop;
                ctl.Left = curLeft;

                AttachUpdateEvents(ctl);

                curLeft += ctl.Size.Width + hpadding;
                curRowMaxHeight = Math.Max(curRowMaxHeight, ctl.Size.Height);
            }
        }

    }
}
