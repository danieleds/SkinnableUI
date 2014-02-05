using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.MetaControls
{
    abstract class MetaControl
    {
        protected readonly PlayerView parentView;

        public MetaControl(PlayerView parentView)
        {
            this.parentView = parentView;
        }

        public abstract void InvalidateView();
        public abstract void Paint(Graphics g);
    }
}
