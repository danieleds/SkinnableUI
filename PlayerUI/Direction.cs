using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{

    [Flags]
    public enum Direction
    {
        None = 0,
        Left = 1 << 0,
        Up = 1 << 1,
        Right = 1 << 2,
        Down = 1 << 3
    };
    
}
