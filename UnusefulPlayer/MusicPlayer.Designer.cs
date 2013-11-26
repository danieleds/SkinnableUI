namespace UnusefulPlayer
{
    partial class MusicPlayer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.playerView1 = new UnusefulPlayer.PlayerView();
            this.SuspendLayout();
            // 
            // playerView1
            // 
            this.playerView1.AllowDrop = true;
            this.playerView1.BlockInputEvents = false;
            this.playerView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.playerView1.Location = new System.Drawing.Point(0, 0);
            this.playerView1.Name = "playerView1";
            this.playerView1.Size = new System.Drawing.Size(624, 297);
            this.playerView1.TabIndex = 0;
            this.playerView1.DragDrop += new System.Windows.Forms.DragEventHandler(this.playerView1_DragDrop);
            this.playerView1.DragEnter += new System.Windows.Forms.DragEventHandler(this.playerView1_DragEnter);
            // 
            // MusicPlayer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 297);
            this.Controls.Add(this.playerView1);
            this.Name = "MusicPlayer";
            this.Text = "MusicPlayer";
            this.Load += new System.EventHandler(this.MusicPlayer_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private PlayerView playerView1;
    }
}