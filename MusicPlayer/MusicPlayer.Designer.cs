using SkinnableUI;
namespace Player
{
    partial class MusicPlayer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.playerView1 = new SkinnableUI.SkinnableView();
            this.panelLoadSkin = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.panelLoadSkin.SuspendLayout();
            this.SuspendLayout();
            // 
            // playerView1
            // 
            this.playerView1.AllowDrop = true;
            this.playerView1.BlockInputEvents = false;
            this.playerView1.DockContainerControl = true;
            this.playerView1.Location = new System.Drawing.Point(0, 0);
            this.playerView1.Name = "playerView1";
            this.playerView1.Size = new System.Drawing.Size(100, 76);
            this.playerView1.TabIndex = 0;
            this.playerView1.DragDrop += new System.Windows.Forms.DragEventHandler(this.playerView1_DragDrop);
            this.playerView1.DragEnter += new System.Windows.Forms.DragEventHandler(this.playerView1_DragEnter);
            // 
            // panelLoadSkin
            // 
            this.panelLoadSkin.BackColor = System.Drawing.Color.RoyalBlue;
            this.panelLoadSkin.Controls.Add(this.label1);
            this.panelLoadSkin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLoadSkin.Location = new System.Drawing.Point(0, 0);
            this.panelLoadSkin.Name = "panelLoadSkin";
            this.panelLoadSkin.Size = new System.Drawing.Size(544, 282);
            this.panelLoadSkin.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 27.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(544, 282);
            this.label1.TabIndex = 0;
            this.label1.Text = "Load your skin!";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // MusicPlayer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(544, 282);
            this.Controls.Add(this.panelLoadSkin);
            this.Controls.Add(this.playerView1);
            this.Name = "MusicPlayer";
            this.Text = "MusicPlayer";
            this.Shown += new System.EventHandler(this.MusicPlayer_Shown);
            this.panelLoadSkin.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private SkinnableView playerView1;
        private System.Windows.Forms.Panel panelLoadSkin;
        private System.Windows.Forms.Label label1;
    }
}