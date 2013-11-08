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
    public partial class MusicPlayer : Form
    {
        public MusicPlayer()
        {
            InitializeComponent();
        }

        Dictionary<PlayerControls.PlayerControl.SemanticType, List<PlayerControls.PlayerControl>> controls = new Dictionary<PlayerControls.PlayerControl.SemanticType, List<PlayerControls.PlayerControl>>();

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void playerView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files[0].EndsWith(".skn"))
                {
                    try
                    {
                        playerView1.LoadSkin(files[0]);
                        playerView1.Width -= 1;
                        playerView1.Width += 1;
                        AttachEvents();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Skin is not valid");
                    }
                }
            }
        }

        private void MusicPlayer_Load(object sender, EventArgs e)
        {

        }

        private void AttachEvents()
        {
            this.controls.Clear();
            foreach (PlayerControls.PlayerControl.SemanticType c in Enum.GetValues(typeof(PlayerControls.PlayerControl.SemanticType)))
                this.controls.Add(c, new List<PlayerControls.PlayerControl>());

            var ctrls = this.playerView1.ContainerControl.GetAllChildren();
            foreach (var item in ctrls)
            {
                this.controls[item.Semantic].Add(item);

                if (item.Semantic == PlayerControls.PlayerControl.SemanticType.Play)
                {
                    item.Click += play_Click;
                }
                else if (item.Semantic == PlayerControls.PlayerControl.SemanticType.SongProgress)
                {
                    if (typeof(PlayerControls.TrackBar).IsAssignableFrom(item.GetType()))
                    {
                        var t = (PlayerControls.TrackBar)item;
                        t.UserChangedValue += songProgress_UserChangedValue;
                    }
                }
            }
        }

        void songProgress_UserChangedValue(object sender, EventArgs e)
        {
            var tb = (PlayerControls.TrackBar)sender;
            this.Text = tb.Value.ToString();
        }

        void play_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Play clicked");
        }

        private void playerView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files[0].EndsWith(".skn"))
                {
                    e.Effect = DragDropEffects.Move;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
        }

        private void playerView1_DragOver(object sender, DragEventArgs e)
        {

        }
    }
}
