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
        PlayerViewDesigner playerView;
        string filename = null;

        PlayerControls.PlayerControl copiedControl = null;

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

            InitializePlayerView();

            listView1_Resize(listView1, new EventArgs());
            cmbControls.DisplayMember = "DisplayName";
        }

        private void InitializePlayerView()
        {
            if (panel2.Controls.Contains(playerView))
                panel2.Controls.Remove(playerView);

            cmbControls.Items.Clear();
            propertyGrid1.SelectedObject = null;

            playerView = new PlayerViewDesigner() {
                AllowDrop = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                BlockInputEvents = true,
                DebugShowPaints = btnShowPaints.Checked,
                DebugShowRuler = rulerToolStripButton.Checked,
                Location = new Point(3, 3),
                Size = new Size(panel2.Size.Width - 8, panel2.Size.Height - 8)
            };

            panel2.Controls.Add(playerView);

            playerView.ContainerControl.Size = playerView.Size;

            playerView.SelectionChanged += playerView1_SelectionChanged;
            playerView.SelectedObjectPropertyChanged += playerView1_SelectedObjectPropertyChanged;
            playerView.DesignerControlsTreeChanged += playerView1_DesignerControlsTreeChanged;
        }

        void playerView1_DesignerControlsTreeChanged(object sender, EventArgs e)
        {
            cmbControls.Items.Clear();

            if (playerView.ContainerControl != null)
            {
                cmbControls.Items.AddRange(playerView.ContainerControl.GetAllChildren().ToArray());
            }
        }

        void playerView1_SelectedObjectPropertyChanged(object sender, EventArgs e)
        {
            propertyGrid1.Refresh();
        }

        void playerView1_SelectionChanged(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = playerView.SelectedControl;
            cmbControls.SelectedItem = playerView.SelectedControl;
        }

        void listView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            ListViewItem item = (ListViewItem)e.Item;
            PlayerControls.PlayerControl.SemanticType ctype = (PlayerControls.PlayerControl.SemanticType)item.Tag;
            listView1.DoDragDrop(ctype, DragDropEffects.Copy);
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            if (filename == null)
                saveAsToolStripButton_Click(sender, e);
            else
                this.playerView.SaveSkin(filename);
        }

        private void listView1_Resize(object sender, EventArgs e)
        {
            listView1.Columns[0].Width = listView1.Width - 3;
        }

        private void cmbControls_SelectedIndexChanged(object sender, EventArgs e)
        {
            playerView.SelectedControl = (PlayerControls.PlayerControl)cmbControls.SelectedItem;
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                playerView.LoadSkin(openDialog.FileName);
                filename = openDialog.FileName;
            }
        }

        private void btnShowPaints_CheckStateChanged(object sender, EventArgs e)
        {
            playerView.DebugShowPaints = btnShowPaints.Checked;
            playerView.Invalidate();
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            var f = new frmSkinPreview(playerView.GetSkin());
            f.ShowDialog();
        }

        private bool AskUserToSaveChanges()
        {
            var answ = MessageBox.Show("Save changes?", "Skin editor", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (answ == System.Windows.Forms.DialogResult.Yes)
            {
                if (filename == null)
                {
                    if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        this.playerView.SaveSkin(saveDialog.FileName);
                        this.filename = saveDialog.FileName;
                        return true;
                    }
                    else return false;
                }
                else
                {
                    this.playerView.SaveSkin(filename);
                    return true;
                }

            }
            else if (answ == System.Windows.Forms.DialogResult.No)
                return true;
            else
                return false;
        }

        private void newToolStripButton_Click(object sender, EventArgs e)
        {
            if (AskUserToSaveChanges())
            {
                filename = null;
                InitializePlayerView();
            }
        }

        private void saveAsToolStripButton_Click(object sender, EventArgs e)
        {
            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.playerView.SaveSkin(saveDialog.FileName);
                filename = saveDialog.FileName;
            }
        }

        private void rulerToolStripButton_CheckStateChanged(object sender, EventArgs e)
        {
            playerView.DebugShowRuler = rulerToolStripButton.Checked;
        }

        private void copyToolStripButton_Click(object sender, EventArgs e)
        {
            PlayerControls.PlayerControl copy = (PlayerControls.PlayerControl)Activator.CreateInstance(
                playerView.SelectedControl.GetType(),
                new object[] { playerView.SelectedControl.Semantic });
            copy.ApplyProperties(playerView.SelectedControl);
            copy.Parent = null;

            copiedControl = copy;
        }

        private void pasteToolStripButton_Click(object sender, EventArgs e)
        {
            if (copiedControl != null)
            {
                PlayerControls.PlayerControl copy = (PlayerControls.PlayerControl)Activator.CreateInstance(
                    copiedControl.GetType(),
                    new object[] { copiedControl.Semantic });
                copy.ApplyProperties(copiedControl);
                copy.Parent = playerView.ContainerControl;
            }
        }

    }
}
