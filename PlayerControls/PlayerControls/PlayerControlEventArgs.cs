using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnusefulPlayer.PlayerControls
{
    public class PlayerControlEventArgs
    {
        public PlayerControl Control;
        
        public PlayerControlEventArgs(PlayerControl control)
        {
            this.Control = control;
        }
    }
}
