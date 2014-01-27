using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.PlayerControls
{
    public class PlayerControlEventArgs : EventArgs
    {
        public PlayerControl Control;
        
        public PlayerControlEventArgs(PlayerControl control) : base()
        {
            this.Control = control;
        }
    }
}
