using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace ExtensionMethods
{
    public static class PlayerExtensionMethods
    {
        /// <summary>
        /// Arrotonda un RectangleF per eccesso (X e Y vengono arrotondate per difetto, Width e Height per eccesso).
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static Rectangle RoundUp(this RectangleF rect)
        {
            return new Rectangle((int)Math.Floor(rect.X), (int)Math.Floor(rect.Y), (int)Math.Ceiling(rect.Width), (int)Math.Ceiling(rect.Height));
        }
    }
}
