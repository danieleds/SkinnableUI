using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinnableUI.MetaControls
{
    abstract class MetaControl
    {
        protected readonly SkinnableView parentView;

        public MetaControl(SkinnableView parentView)
        {
            this.parentView = parentView;
        }

        public abstract void InvalidateView();
        public abstract void Paint(Graphics g);
    }
}
