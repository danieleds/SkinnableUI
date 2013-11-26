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
    public partial class frmSkinEditor : Form
    {
        public frmSkinEditor()
        {
            InitializeComponent();

            saveDialog.Filter = "Skin|*.skn";
            openDialog.Filter = "Skin|*.skn";
        }

        SaveFileDialog saveDialog = new SaveFileDialog();
        OpenFileDialog openDialog = new OpenFileDialog();

        private void Form1_Load(object sender, EventArgs e)
        {
            // Riempiamo la listview con tutti i controlli disponibili
            foreach (PlayerControls.PlayerControl.SemanticType c in Enum.GetValues(typeof(PlayerControls.PlayerControl.SemanticType)))
            {
                PlayerControls.PlayerControl.SemanticTypeMeta info = PlayerControls.PlayerControl.GetPlayerControlInstanceInfo(c);
                ListViewItem item = new ListViewItem(info.Title);
                item.Tag = c; // Salviamo il tipo del controllo (Play, Pause) nel campo Tag (ci servirà per il drag n' drop)
                listView1.Items.Add(item);
            }
            
            listView1.ItemDrag += listView1_ItemDrag;

            playerView1.ContainerControl.Size = playerView1.Size;
            
            playerView1.SelectionChanged += playerView1_SelectionChanged;
            playerView1.SelectedObjectPropertyChanged += playerView1_SelectedObjectPropertyChanged;
            playerView1.DesignerControlsTreeChanged += playerView1_DesignerControlsTreeChanged;

            playerView1.BlockInputEvents = true;

            listView1_Resize(listView1, new EventArgs());
            cmbControls.DisplayMember = "DisplayName";
        }

        void playerView1_DesignerControlsTreeChanged(object sender, EventArgs e)
        {
            cmbControls.Items.Clear();

            if (playerView1.ContainerControl != null)
            {
                cmbControls.Items.AddRange(playerView1.ContainerControl.GetAllChildren().ToArray());
            }
        }

        void playerView1_SelectedObjectPropertyChanged(object sender, EventArgs e)
        {
            propertyGrid1.Refresh();
        }

        void playerView1_SelectionChanged(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = playerView1.SelectedControl;
            cmbControls.SelectedItem = playerView1.SelectedControl;
        }

        void listView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            ListViewItem item = (ListViewItem)e.Item;
            PlayerControls.PlayerControl.SemanticType ctype = (PlayerControls.PlayerControl.SemanticType)item.Tag;
            listView1.DoDragDrop(ctype, DragDropEffects.Copy);
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                this.playerView1.SaveSkin(saveDialog.FileName);
        }

        private void listView1_Resize(object sender, EventArgs e)
        {
            listView1.Columns[0].Width = listView1.Width - 3;
        }

        private void cmbControls_SelectedIndexChanged(object sender, EventArgs e)
        {
            playerView1.SelectedControl = (PlayerControls.PlayerControl)cmbControls.SelectedItem;
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                playerView1.LoadSkin(openDialog.FileName);
            }
        }

        private void btnShowPaints_CheckStateChanged(object sender, EventArgs e)
        {
            playerView1.DebugShowPaints = btnShowPaints.Checked;
            playerView1.Invalidate();
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            var f = new frmSkinPreview(playerView1.GetSkin());
            f.ShowDialog();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            var f = new MusicPlayer();
            f.Show();
        }

    }
}
