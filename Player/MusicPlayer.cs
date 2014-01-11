using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio;
using NAudio.Wave;
using PlayerControls = UnusefulPlayer.PlayerControls;

namespace Player
{
    public partial class MusicPlayer : Form
    {
        Dictionary<PlayerControls.PlayerControl.SemanticType, List<PlayerControls.PlayerControl>> controls = new Dictionary<PlayerControls.PlayerControl.SemanticType, List<PlayerControls.PlayerControl>>();
        List<PlayerControls.Button> play = new List<PlayerControls.Button>();
        List<PlayerControls.Button> pause = new List<PlayerControls.Button>();
        List<PlayerControls.ToggleButton> playPause = new List<PlayerControls.ToggleButton>();
        List<PlayerControls.Button> stop = new List<PlayerControls.Button>();
        List<PlayerControls.TrackBar> songProgress = new List<PlayerControls.TrackBar>();

        List<PlayerControls.Label> title = new List<PlayerControls.Label>();
        List<PlayerControls.Label> artist = new List<PlayerControls.Label>();
        List<PlayerControls.Label> album = new List<PlayerControls.Label>();
        List<PlayerControls.Label> year = new List<PlayerControls.Label>();

        List<PlayerControls.Label> currentTime = new List<PlayerControls.Label>();
        List<PlayerControls.Label> totalTime = new List<PlayerControls.Label>();
        List<PlayerControls.Label> remainingTime = new List<PlayerControls.Label>();

        List<PlayerControls.ListView> playlist = new List<PlayerControls.ListView>();

        Mp3FileReader mp3Reader;
        WaveOut waveOut;

        Timer tmr = new Timer { Interval = 100 };

        List<String> songPaths = new List<string>();
        int currentSong = -1;

        public MusicPlayer()
        {
            InitializeComponent();

            foreach (PlayerControls.PlayerControl.SemanticType c in Enum.GetValues(typeof(PlayerControls.PlayerControl.SemanticType)))
                this.controls.Add(c, new List<PlayerControls.PlayerControl>());
            
            waveOut = new WaveOut();

            tmr.Tick += tmr_Tick;
        }

        void tmr_Tick(object sender, EventArgs e)
        {
            songProgress.ForEach(item => item.Value = (int)mp3Reader.CurrentTime.TotalMilliseconds);
            currentTime.ForEach(item => item.Text = FormatTime(mp3Reader.CurrentTime, mp3Reader.TotalTime));
            remainingTime.ForEach(item => item.Text = FormatTime(mp3Reader.TotalTime - mp3Reader.CurrentTime, mp3Reader.TotalTime));
        }

        string FormatTime(TimeSpan time, TimeSpan totalTime)
        {
            if (totalTime.Hours != 0)
                return time.ToString(@"h\:mm\:ss");
            else
                return time.ToString(@"mm\:ss");
        }

        private void playerView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    songPaths.Add(file);
                    var filename = System.IO.Path.GetFileName(file);

                    TagLib.File f = null;
                    try
                    {
                        f = TagLib.File.Create(file);
                    }
                    catch (TagLib.UnsupportedFormatException)
                    {

                    }

                    foreach (var playlist in this.playlist)
                    {
                        var row = new PlayerControls.ListView.ListViewRow();
                        if (f != null)
                        {
                            row.Values.Add(f.Tag.Title.Trim() == "" ? file : f.Tag.Title);
                            row.Values.Add(f.Tag.JoinedPerformers.Trim() != "" ? f.Tag.JoinedPerformers : f.Tag.JoinedAlbumArtists);
                            row.Values.Add(f.Tag.Album);
                        }
                        else
                        {
                            row.Values.Add(file);
                        }
                        playlist.Items.Add(row);
                    }
                }
            }
        }

        private void MusicPlayer_Load(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog { Filter = "Skin file|*.skn" };
            if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                playerView1.LoadSkin(fd.FileName);
                playerView1.Width -= 1;
                playerView1.Width += 1;
                AttachEvents();


                var path = @"aaa.mp3";
                mp3Reader = new Mp3FileReader(path);
                waveOut.Init(mp3Reader);
                
                songProgress.ForEach(item => item.Maximum = (int)mp3Reader.TotalTime.TotalMilliseconds);
                currentTime.ForEach(item => item.Text = FormatTime(mp3Reader.CurrentTime, mp3Reader.TotalTime));
                totalTime.ForEach(item => item.Text = FormatTime(mp3Reader.TotalTime, mp3Reader.TotalTime));
                remainingTime.ForEach(item => item.Text = FormatTime(mp3Reader.TotalTime - mp3Reader.CurrentTime, mp3Reader.TotalTime));
                playlist.ForEach(item => item.Items.Clear());

                TagLib.File f = TagLib.File.Create(path);
                title.ForEach(item => item.Text = f.Tag.Title);
                artist.ForEach(item => item.Text = f.Tag.JoinedPerformers.Trim() != "" ? f.Tag.JoinedPerformers : f.Tag.JoinedAlbumArtists);
                album.ForEach(item => item.Text = f.Tag.Album);
                year.ForEach(item => item.Text = f.Tag.Year.ToString());
                //f.Tag.Pictures[0].
                
                tmr.Start();
            }
            else
            {
                this.Close();
            }
        }

        List<T> GetControls<T>(PlayerControls.PlayerControl.SemanticType type) where T : PlayerControls.PlayerControl
        {
            var tmp = new List<PlayerControls.PlayerControl>();
            controls[type].ForEach(item => { if (item is T) tmp.Add(item); });
            return new List<T>(tmp.Cast<T>());
        }

        /*void ResetUI()
        {
            this.controls.Clear();
            play.Clear();
            songProgress.Clear();

            foreach (PlayerControls.PlayerControl.SemanticType c in Enum.GetValues(typeof(PlayerControls.PlayerControl.SemanticType)))
                this.controls.Add(c, new List<PlayerControls.PlayerControl>());
        }*/

        private void AttachEvents()
        {
            //ResetUI();

            var ctrls = this.playerView1.ContainerControl.GetAllChildren();
            foreach (var item in ctrls)
            {
                this.controls[item.Semantic].Add(item);
            }

            play = GetControls<PlayerControls.Button>(PlayerControls.PlayerControl.SemanticType.Play);
            pause = GetControls<PlayerControls.Button>(PlayerControls.PlayerControl.SemanticType.Pause);
            playPause = GetControls<PlayerControls.ToggleButton>(PlayerControls.PlayerControl.SemanticType.PlayPause);
            stop = GetControls<PlayerControls.Button>(PlayerControls.PlayerControl.SemanticType.Stop);
            songProgress = GetControls<PlayerControls.TrackBar>(PlayerControls.PlayerControl.SemanticType.SongProgress);
            title = GetControls<PlayerControls.Label>(PlayerControls.PlayerControl.SemanticType.Title);
            artist = GetControls<PlayerControls.Label>(PlayerControls.PlayerControl.SemanticType.Artist);
            album = GetControls<PlayerControls.Label>(PlayerControls.PlayerControl.SemanticType.Album);
            year = GetControls<PlayerControls.Label>(PlayerControls.PlayerControl.SemanticType.Year);
            currentTime = GetControls<PlayerControls.Label>(PlayerControls.PlayerControl.SemanticType.CurrentTime);
            totalTime = GetControls<PlayerControls.Label>(PlayerControls.PlayerControl.SemanticType.TotalTime);
            remainingTime = GetControls<PlayerControls.Label>(PlayerControls.PlayerControl.SemanticType.RemainingTime);
            playlist = GetControls<PlayerControls.ListView>(PlayerControls.PlayerControl.SemanticType.Playlist);

            play.ForEach(c => c.Click += play_Click);
            pause.ForEach(c => c.Click += pause_Click);
            playPause.ForEach(c => c.CheckedChanged += playPause_CheckedChanged);
            stop.ForEach(c => c.Click += stop_Click);
            songProgress.ForEach(c => c.UserChangedValue += songProgress_UserChangedValue);
        }

        void stop_Click(object sender, EventArgs e)
        {
            playPause.ForEach(item => item.Checked = false);
            waveOut.Stop();
            mp3Reader.Seek(0, System.IO.SeekOrigin.Begin);
        }

        void playPause_CheckedChanged(object sender, EventArgs e)
        {
            var ctl = (PlayerControls.ToggleButton)sender;
            if (ctl.Checked)
                waveOut.Play();
            else
                waveOut.Pause();
        }

        private void pause_Click(object sender, EventArgs e)
        {
            waveOut.Pause();
        }

        void songProgress_UserChangedValue(object sender, EventArgs e)
        {
            var tb = (PlayerControls.TrackBar)sender;
            mp3Reader.CurrentTime = new TimeSpan(0, 0, 0, 0, tb.Value);
        }

        void play_Click(object sender, EventArgs e)
        {
            waveOut.Play();
        }

        private void playerView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void playerView1_DragOver(object sender, DragEventArgs e)
        {

        }
    }
}
