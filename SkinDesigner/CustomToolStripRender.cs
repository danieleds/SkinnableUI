using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SkinDesigner
{
    class CustomToolStripRender : ToolStripProfessionalRenderer 
    {
        public CustomToolStripRender() {
            this.RoundedEdges = false;
        }

        

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            //base.OnRenderToolStripBorder(e);
        }
    }
}
