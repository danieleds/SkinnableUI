using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinDesigner
{
    /// <summary>
    /// Pannello Windows Forms con alcune correzioni
    /// </summary>
    class CustomPanel : System.Windows.Forms.Panel
    {
        protected override System.Drawing.Point ScrollToControl(System.Windows.Forms.Control activeControl)
        {
            // Returning the current location prevents the panel from
            // scrolling to the active control when the panel loses and regains focus
            return this.DisplayRectangle.Location;
        }
    }
}
