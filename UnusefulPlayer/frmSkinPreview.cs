using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UnusefulPlayer
{
    public partial class frmSkinPreview : Form
    {
        public frmSkinPreview(Skin skin)
        {
            InitializeComponent();
            playerView1.SetSkin(skin);
        }

        private void frmSkinPreview_Load(object sender, EventArgs e)
        {
            // Ora che il form ha una dimensione, genera l'evento Resize
            playerView1.Width = playerView1.Width - 1;
            playerView1.Width = playerView1.Width + 1;
        }
    }
}
