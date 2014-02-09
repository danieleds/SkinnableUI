/*
 *  Copyright 2014 Daniele Di Sarli
 *
 *  This file is part of SkinnableUI.
 *
 *  SkinnableUI is free software: you can redistribute it and/or modify
 *  it under the terms of the Lesser GNU General Public License as
 *  published by the Free Software Foundation, either version 3 of the
 *  License, or (at your option) any later version.
 *
 *  SkinnableUI is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  Lesser GNU General Public License for more details.
 *
 *  You should have received a copy of the Lesser GNU General Public License
 *  along with SkinnableUI. If not, see <http://www.gnu.org/licenses/>.
 */

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
