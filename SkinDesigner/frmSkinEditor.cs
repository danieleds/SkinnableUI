using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PlayerControls = PlayerUI.PlayerControls;

namespace SkinDesigner
{
    public partial class frmSkinEditor : Form
    {
        PlayerUI.PlayerViewDesigner playerView;

        string _filename;
        string filename
        {
            get { return _filename; }
            set
            {
                _filename = value;
                if (_filename == null)
                    lblFilename.Text = "New skin";
                else
                    lblFilename.Text = System.IO.Path.GetFileName(value);
            }
        }

        public frmSkinEditor()
        {
            InitializeComponent();

            saveDialog.Filter = "Skin|*.skn";
            openDialog.Filter = "Skin|*.skn";
            filename = null;
        }

        SaveFileDialog saveDialog = new SaveFileDialog();
        OpenFileDialog openDialog = new OpenFileDialog();

        Point defaultContainerLocation = new Point(30, 50);

        private void Form1_Load(object sender, EventArgs e)
        {
            toolStrip1.Renderer = new CustomToolStripRender();

            // Riempiamo la listview con tutti i controlli disponibili
            foreach (PlayerControls.PlayerControl.SemanticType c in Enum.GetValues(typeof(PlayerControls.PlayerControl.SemanticType)))
            {
                PlayerControls.PlayerControl.SemanticTypeMeta info = PlayerControls.PlayerControl.GetPlayerControlInstanceInfo(c);
                ListViewItem item = new ListViewItem(info.Title);
                item.Tag = c; // Salviamo il tipo del controllo (Play, Pause) nel campo Tag (ci servirà per il drag n' drop)
                listView1.Items.Add(item);
            }
            
            listView1.ItemDrag += listView1_ItemDrag;
            panelSurface.Resize += panelSurface_Resize;

            InitializePlayerView();

            listView1_Resize(listView1, new EventArgs());
            cmbControls.DisplayMember = "DisplayName";
        }

        private void InitializePlayerView()
        {
            if (panelSurface.Controls.Contains(playerView))
                panelSurface.Controls.Remove(playerView);

            cmbControls.Items.Clear();
            propertyGrid1.SelectedObject = null;

            playerView = new PlayerUI.PlayerViewDesigner() {
                AllowDrop = true,
                DesignerBackColor = Color.FromArgb(240, 240, 240),
                BlockInputEvents = true,
                DockContainerControl = false,
                DebugShowPaints = btnShowPaints.Checked,
                DebugShowRuler = rulerToolStripButton.Checked,
                DrawWindowDecorations = true,
                Location = new Point(0, 0)
            };

            panelSurface.Controls.Add(playerView);

            AdjustPlayerViewSizeForScrollbars();

            playerView.ContainerControl.Location = defaultContainerLocation;
            playerView.ContainerControl.Size = new Size(400, 320);
            playerView.ContainerControl.Resize += ContainerControl_Resize;

            playerView.SelectionChanged += playerView_SelectionChanged;
            playerView.SelectedObjectPropertyChanged += playerView_SelectedObjectPropertyChanged;
            playerView.DesignerControlsTreeChanged += playerView_DesignerControlsTreeChanged;
            playerView.KeyDown += playerView_KeyDown;

            playerView_DesignerControlsTreeChanged(playerView, new EventArgs());
        }

        void ContainerControl_Resize(object sender, EventArgs e)
        {
            AdjustPlayerViewSizeForScrollbars();
        }

        void panelSurface_Resize(object sender, EventArgs e)
        {
            AdjustPlayerViewSizeForScrollbars();
        }

        /// <summary>
        /// Modifica la dimensione del PlayerView per far comparire nel panelSurface le scrollbar
        /// al momento giusto.
        /// </summary>
        void AdjustPlayerViewSizeForScrollbars()
        {
            if (this.playerView != null)
            {
                var containerControlRightmostPx = 20 + playerView.ContainerControl.Location.X + playerView.ContainerControl.Size.Width;
                var containerControlDownmostPx = 20 + playerView.ContainerControl.Location.Y + playerView.ContainerControl.Size.Height;

                if (containerControlRightmostPx <= panelSurface.Size.Width)
                {
                    playerView.Anchor |= AnchorStyles.Right;
                    playerView.Width = panelSurface.Size.Width;
                }
                else
                {
                    playerView.Anchor &= AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
                    playerView.Width = (int)Math.Max(panelSurface.Size.Width, containerControlRightmostPx);
                }

                if (containerControlDownmostPx <= panelSurface.Size.Height)
                {
                    playerView.Anchor |= AnchorStyles.Bottom;
                    playerView.Height = panelSurface.Size.Height;
                }
                else
                {
                    playerView.Anchor &= AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
                    playerView.Height = (int)Math.Max(panelSurface.Size.Height, containerControlDownmostPx);
                }

                // Aggiorna le scrollbar
                panelSurface.PerformLayout();
            }
        }

        void playerView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                copyToolStripButton_Click(this, new EventArgs());
            }
            else if (e.Control && e.KeyCode == Keys.X)
            {
                cutToolStripButton_Click(this, new EventArgs());
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                pasteToolStripButton_Click(this, new EventArgs());
            }
        }

        void playerView_DesignerControlsTreeChanged(object sender, EventArgs e)
        {
            cmbControls.Items.Clear();

            if (playerView.ContainerControl != null)
            {
                cmbControls.Items.AddRange(playerView.ContainerControl.GetAllChildren().ToArray());
            }
        }

        void playerView_SelectedObjectPropertyChanged(object sender, EventArgs e)
        {
            propertyGrid1.Refresh();
        }

        void playerView_SelectionChanged(object sender, EventArgs e)
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
            if (AskUserToSaveChanges())
            {
                if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        playerView.LoadSkin(openDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    filename = openDialog.FileName;

                    playerView.ContainerControl.Location = defaultContainerLocation;
                    AdjustPlayerViewSizeForScrollbars();
                }
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
            if(filename == null && (this.playerView.ContainerControl == null || this.playerView.ContainerControl.Controls.Count == 0))
                return true;

            var answ = MessageBox.Show("Do you want to save the current changes?", "Skin editor", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
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
            if (playerView.SelectedControl != null)
            {
                playerView.CopyControlToClipboard(playerView.SelectedControl);
            }
        }

        private void pasteToolStripButton_Click(object sender, EventArgs e)
        {
            PlayerControls.Container where;
            if (playerView.SelectedControl == null)
            {
                where = playerView.ContainerControl;
            }
            else
            {
                if (playerView.SelectedControl is PlayerControls.Container)
                {
                    where = (PlayerControls.Container)playerView.SelectedControl;
                }
                else
                {
                    where = playerView.SelectedControl.Parent;
                }
            }

            playerView.PasteControlFromClipboard(where);
        }

        private void cutToolStripButton_Click(object sender, EventArgs e)
        {
            if (playerView.SelectedControl != null)
            {
                playerView.CutControlToClipboard(playerView.SelectedControl);
            }
        }

        private void btnBringForward_Click(object sender, EventArgs e)
        {
            if (playerView.SelectedControl != null && playerView.SelectedControl.Parent != null)
            {
                PlayerControls.PlayerControl ctl = playerView.SelectedControl;
                ctl.Parent.BringToFront(ctl);
            }
        }

        private void btnBringBackward_Click(object sender, EventArgs e)
        {
            if (playerView.SelectedControl != null && playerView.SelectedControl.Parent != null)
            {
                PlayerControls.PlayerControl ctl = playerView.SelectedControl;
                ctl.Parent.SendToBack(ctl);
            }
        }

    }
}
