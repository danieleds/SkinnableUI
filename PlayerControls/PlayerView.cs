using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UnusefulPlayer.PlayerControls;
using System.Drawing;

namespace UnusefulPlayer
{
    /* Contenitore di oggetti PlayerControl */
    public class PlayerView : UserControl
    {
        /// <summary>
        /// L'ordine degli elementi in questa lista rappresenta il loro z-order.
        /// Gli elementi in testa sono sopra rispetto a quelli in coda.
        /// </summary>
        protected Container containerControl;
        public Animator animator = new Animator();

        [Obsolete("Questa proprietà ha un comportamento nullo. Usare ContainerControl per impostare un contenitore di primo livello.")]
        new public ControlCollection Controls { get; private set; }

        public bool BlockInputEvents { get; set; }

        public PlayerView()
        {
            BlockInputEvents = false;

            var cc = new Container(PlayerControl.SemanticType.Container);
            cc.Top = 0;
            cc.Left = 0;
            cc.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top;
            this.ContainerControl = cc;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        public bool DesignSkinMode { get { return this is PlayerViewDesigner; } }

        public Container ContainerControl
        {
            get { return this.containerControl; }
            set
            {
                this.containerControl = value;
                if (this.containerControl != null)
                    containerControl.ParentView = this;
            }
        }

        public Skin GetSkin()
        {
            var resources = new Dictionary<string, System.IO.MemoryStream>();

            var doc = new System.Xml.XmlDocument();
            var declar = doc.CreateXmlDeclaration("1.0", System.Text.Encoding.UTF8.WebName, null);
            doc.AppendChild(declar);

            var root = doc.CreateElement("Theme");
            root.SetAttribute("version", "1.0");
            root.AppendChild(this.ContainerControl.GetXmlElement(doc, resources));

            doc.AppendChild(root);

            return new Skin(doc, resources);
        }

        public void SaveSkin(string fileName)
        {
            SerializationHelper.SaveSkinPackage(fileName, this.GetSkin());
        }

        public virtual void SetSkin(Skin skin)
        {
            var doc = skin.Xml;
            var resources = skin.Resources;

            var theme = doc.GetElementsByTagName("Theme")[0];

            this.ContainerControl = new Container(PlayerControl.SemanticType.Container);
            this.ContainerControl.ParentView = this;
            this.ContainerControl.FromXmlElement((System.Xml.XmlElement)theme.ChildNodes[0], resources);
        }

        public void LoadSkin(string fileName)
        {
            this.SetSkin(SerializationHelper.OpenSkinPackage(fileName));
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    return true;
                case Keys.Shift | Keys.Right:
                case Keys.Shift | Keys.Left:
                case Keys.Shift | Keys.Up:
                case Keys.Shift | Keys.Down:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.ContainerControl != null)
            {
                if (this.Width != 0 && this.Height != 0) // Per evitare bug su minimize della form
                {
                    this.ContainerControl.Left = 0;
                    this.ContainerControl.Top = 0;
                    this.ContainerControl.Size = new SizeF(this.Width, this.Height);
                }
            }
        }

#region Events

        protected override void OnPaint(PaintEventArgs e)
        {
            this.containerControl.InternalPaint(e.Graphics);
            base.OnPaint(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!BlockInputEvents)
            {
                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(this.containerControl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(this.containerControl.Top, 0, MidpointRounding.ToEven), e.Delta);
                this.containerControl.OnMouseDown(e2);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!BlockInputEvents)
            {
                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(this.containerControl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(this.containerControl.Top, 0, MidpointRounding.ToEven), e.Delta);
                this.containerControl.OnMouseMove(e2);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!BlockInputEvents)
            {
                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(this.containerControl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(this.containerControl.Top, 0, MidpointRounding.ToEven), e.Delta);
                this.containerControl.OnMouseUp(e2);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if(!BlockInputEvents)
            {
                MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, e.X - (int)Math.Round(this.containerControl.Left, 0, MidpointRounding.ToEven), e.Y - (int)Math.Round(this.containerControl.Top, 0, MidpointRounding.ToEven), e.Delta);
                this.containerControl.OnMouseWheel(e2);
            }
        }

#endregion

    }
}
