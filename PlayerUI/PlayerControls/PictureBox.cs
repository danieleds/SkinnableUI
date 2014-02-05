using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;

namespace PlayerUI.PlayerControls
{
    public class PictureBox : PlayerControl
    {

        public PictureBox(SemanticType c) : base(c)
        {
            this.Size = new SizeF(50, 50);
            this.TabStop = false;
        }

        private Image image;
        public Image Image { get { return image; } set { image = value; this.Invalidate(); } }

        protected override void OnPaint(System.Drawing.Graphics g)
        {
            if (image != null)
            {
                g.DrawImage(image, new RectangleF(0, 0, this.Size.Width, this.Size.Height));
            }
            
            if (this.ParentView != null && this.ParentView.DesignSkinMode)
            {
                Pen p = new Pen(Color.Gray);
                p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                g.DrawRectangle(p, 0, 0, this.Size.Width-1, this.Size.Height-1);
            }
            
        }

        public override System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument document, Dictionary<string, System.IO.MemoryStream> resources)
        {
            var node = base.GetXmlElement(document, resources);
            SerializationHelper.SetImage(this.image, "image", resources, node);

            return node;
        }

        public override void FromXmlElement(System.Xml.XmlElement element, Dictionary<string, System.IO.MemoryStream> resources)
        {
            base.FromXmlElement(element, resources);
            SerializationHelper.LoadBitmapFromResources(element, "image", resources, s => this.Image = s);
        }

    }
}
