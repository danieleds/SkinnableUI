using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;

namespace PlayerUI.PlayerControls
{
    public class Button : PlayerControl
    {

        public Button(SemanticType c) : base(c)
        {
            this.Size = new SizeF(75, 30);
        }

        private bool pressed;
        private bool over;

        protected NinePatch backgroundNormal9P;
        [DefaultValue(null)]
        public Bitmap BackgroundNormal9P
        {
            get { return backgroundNormal9P != null ? backgroundNormal9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundNormal9P = null;
                else
                    this.backgroundNormal9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        protected NinePatch backgroundOver9P;
        [DefaultValue(null)]
        public Bitmap BackgroundOver9P
        {
            get { return backgroundOver9P != null ? backgroundOver9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundOver9P = null;
                else
                    this.backgroundOver9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        protected NinePatch backgroundPressed9P;
        [DefaultValue(null)]
        public Bitmap BackgroundPressed9P
        {
            get { return backgroundPressed9P != null ? backgroundPressed9P.Image : null; }
            set
            {
                if (value == null)
                    this.backgroundPressed9P = null;
                else
                    this.backgroundPressed9P = new NinePatch(value);
                this.Invalidate();
            }
        }

        private string text;
        public string Text { get { return text; } set { text = value; this.Invalidate(); } }

        protected override void OnPaint(System.Drawing.Graphics g)
        {
            var strSize = g.MeasureString(this.Text, this.Font);
            var contentBox = new RectangleF(0, 0, this.Size.Width, this.Size.Height);
            
            if (pressed)
            {
                if (backgroundPressed9P != null)
                {
                    var patch = backgroundPressed9P;
                    contentBox = patch.GetContentBox(this.Size);
                    patch.Paint(g, this.Size);
                }
                else
                    drawDefaultButton(g);
            }
            else if(over)
            {
                if (backgroundOver9P != null)
                {
                    var patch = anim != null && anim.IsRunning() ? anim.GetCurrentFrame() : backgroundOver9P;
                    contentBox = patch.GetContentBox(this.Size);
                    patch.Paint(g, this.Size);
                }
                else
                    drawDefaultButton(g);
            }
            else
            {
                if (backgroundNormal9P != null)
                {
                    var patch = anim != null && anim.IsRunning() ? anim.GetCurrentFrame() : backgroundNormal9P;
                    contentBox = patch.GetContentBox(this.Size);
                    patch.Paint(g, this.Size);
                }
                else
                    drawDefaultButton(g);
            }

            g.SetClip(contentBox, System.Drawing.Drawing2D.CombineMode.Intersect);
            g.DrawString(this.Text, this.Font, new SolidBrush(this.ForeColor), contentBox.Width / 2 - strSize.Width / 2 + contentBox.Left, contentBox.Height / 2 - strSize.Height / 2 + contentBox.Top);
        }

        private void drawDefaultButton(Graphics g)
        {
            g.FillRectangle(SystemBrushes.ButtonFace, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
            g.DrawRectangle(SystemPens.ButtonShadow, 0, 0, this.Size.Width - 1, this.Size.Height - 1);
        }

        public override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.pressed = true;
                this.Invalidate();
            }
        }

        public override void OnMouseUp(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.pressed = false;
                this.Invalidate();
            }
        }

        Animator.Animation anim;
        public override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.over = true;

            if (this.backgroundOver9P != null && this.backgroundNormal9P != null)
                AnimateTo(ref anim, this.backgroundOver9P, this.backgroundNormal9P, this.GetAnimator(), 50, 400);
            
            this.Invalidate();
        }

        /// <summary>
        /// Esegue un'animazione salvando l'oggetto in animationPlaceholder.
        /// Una chiamata successiva di questo metodo sullo stesso animationPlaceholder
        /// eseguirà la nuova animazione a partire dall'istante in cui era arrivata quella vecchia.
        /// </summary>
        /// <param name="animationPlaceholder"></param>
        /// <param name="to"></param>
        /// <param name="defaultFrom"></param>
        /// <param name="animator"></param>
        /// <param name="interval"></param>
        /// <param name="duration"></param>
        private void AnimateTo(ref Animator.Animation animationPlaceholder, NinePatch to, NinePatch defaultFrom, Animator animator, int interval, int duration)
        {
            float p;
            NinePatch startFrame;
            if (animationPlaceholder == null)
            {
                p = 1;
                startFrame = defaultFrom;
            }
            else
            {
                p = animationPlaceholder.Stop();
                startFrame = animationPlaceholder.GetCurrentFrame();
            }

            animationPlaceholder = animator.Attach(interval, (int)(duration * p), startFrame, to, this.Invalidate);
            animationPlaceholder.SetDetachOnFinish(true, animator);
            animationPlaceholder.Start();
        }

        public override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.over = false;

            if (this.backgroundNormal9P != null && this.backgroundOver9P != null)
                AnimateTo(ref anim, this.backgroundNormal9P, this.backgroundOver9P, this.GetAnimator(), 50, 400);

            this.Invalidate();
        }

        public override System.Xml.XmlElement GetXmlElement(System.Xml.XmlDocument document, Dictionary<string, System.IO.MemoryStream> resources)
        {
            var node = base.GetXmlElement(document, resources);
            node.SetAttribute("text", this.Text);

            SerializationHelper.SetNinePatch(this.backgroundNormal9P, "backgroundNormal9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundOver9P, "backgroundOver9P", resources, node);
            SerializationHelper.SetNinePatch(this.backgroundPressed9P, "backgroundPressed9P", resources, node);

            return node;
        }

        public override void FromXmlElement(System.Xml.XmlElement element, Dictionary<string, System.IO.MemoryStream> resources)
        {
            base.FromXmlElement(element, resources);
            SerializationHelper.LoadString(element, "text", s => this.Text = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundNormal9P", resources, s => this.BackgroundNormal9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundOver9P", resources, s => this.BackgroundOver9P = s);
            SerializationHelper.LoadBitmapFromResources(element, "backgroundPressed9P", resources, s => this.BackgroundPressed9P = s);
        }

    }
}
