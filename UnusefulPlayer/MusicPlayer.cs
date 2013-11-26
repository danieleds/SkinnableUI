﻿using System;
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

namespace UnusefulPlayer
{
    public partial class MusicPlayer : Form
    {

        Dictionary<PlayerControls.PlayerControl.SemanticType, List<PlayerControls.PlayerControl>> controls = new Dictionary<PlayerControls.PlayerControl.SemanticType, List<PlayerControls.PlayerControl>>();
        List<PlayerControls.Button> play = new List<PlayerControls.Button>();
        List<PlayerControls.TrackBar> songProgress = new List<PlayerControls.TrackBar>();

        Mp3FileReader mp3Reader;
        WaveOut waveOut;

        Timer tmr = new Timer { Interval = 100 };

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
        }

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
            /*if (e.Data.GetDataPresent(DataFormats.FileDrop))
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
            }*/
        }

        private void MusicPlayer_Load(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                playerView1.LoadSkin(fd.FileName);
                playerView1.Width -= 1;
                playerView1.Width += 1;
                AttachEvents();


                mp3Reader = new Mp3FileReader(@"aaa.mp3");
                waveOut.Init(mp3Reader);

                songProgress.ForEach(item => item.Maximum = (int)mp3Reader.TotalTime.TotalMilliseconds);

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

                if (item.Semantic == PlayerControls.PlayerControl.SemanticType.Play)
                {
                    item.Click += play_Click;
                }
                else if (item.Semantic == PlayerControls.PlayerControl.SemanticType.Pause)
                {
                    item.Click += pause_Click;
                }
                else if (item.Semantic == PlayerControls.PlayerControl.SemanticType.PlayPause)
                {
                    if (typeof(PlayerControls.ToggleButton).IsAssignableFrom(item.GetType()))
                    {
                        var t = (PlayerControls.ToggleButton)item;
                        t.CheckedChanged += playPause_CheckedChanged;
                    }
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

            play = GetControls<PlayerControls.Button>(PlayerControls.PlayerControl.SemanticType.Play);
            songProgress = GetControls<PlayerControls.TrackBar>(PlayerControls.PlayerControl.SemanticType.SongProgress);
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
            this.Text = tb.Value.ToString();
            mp3Reader.Skip(-mp3Reader.CurrentTime.Milliseconds);
            mp3Reader.Skip(tb.Value);
        }

        void play_Click(object sender, EventArgs e)
        {
            waveOut.Play();
        }

        private void playerView1_DragEnter(object sender, DragEventArgs e)
        {
            /*if (e.Data.GetDataPresent(DataFormats.FileDrop))
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
            }*/
        }

        private void playerView1_DragOver(object sender, DragEventArgs e)
        {

        }
    }
}
