using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkinnableUI;

namespace SkinDesigner
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
            this.ClientSize = playerView1.Size;
            // Ora che il form ha una dimensione, genera l'evento Resize
            playerView1.Width = playerView1.Width - 1;
            playerView1.Width = playerView1.Width + 1;
            playerView1.Dock = DockStyle.Fill;
            //playerView1.Resize += (sender2, e2) => this.ClientSize = playerView1.Size;
            this.ClientSize = playerView1.Size;
        }
    }
}
