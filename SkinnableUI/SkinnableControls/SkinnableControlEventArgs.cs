using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinnableUI.SkinnableControls
{
    public class SkinnableControlEventArgs : EventArgs
    {
        public SkinnableControl Control;
        
        public SkinnableControlEventArgs(SkinnableControl control) : base()
        {
            this.Control = control;
        }
    }
}
