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

        void FlowLayoutContainer_ControlAdded(object sender, PlayerControlEventArgs e)
        {
            SnapTopToGrid(e.Control);
            AttachUpdateEvents(e.Control);
            this.SetLayout();
        }

        void FlowLayoutContainer_ControlRemoved(object sender, PlayerControlEventArgs e)
        {
            DetachUpdateEvents(e.Control);
            this.SetLayout();
        }

        void AttachUpdateEvents(PlayerControl c)
        {
            DetachUpdateEvents(c);
            c.Resize += Control_Resize;
            c.Move += Control_Move;
        }

        void DetachUpdateEvents(PlayerControl c)
        {
            c.Resize -= Control_Resize;
            c.Move -= Control_Move;
        }

        void Control_Move(object sender, EventArgs e)
        {
            SnapTopToGrid((PlayerControl)sender);

            this.SetLayout();
        }

        void SnapTopToGrid(PlayerControl control)
        {
            float minHeightDifference = float.MaxValue;
            PlayerControl minHeightDifferenceCtl = control;

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
