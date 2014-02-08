using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinnableUI
{
    public class Animation : IDisposable
    {
        int toNextFrame; // Istanti rimanenti per arrivare al prossimo frame
        int frameInterval; // Numero di istanti tra un frame e l'altro
        int numberOfFrames; // Numero totale di frame che compongono l'animazione
        int frameCount; // Contatore frame corrente

        NinePatch from, to, curFrame;
        RectangleF srcRect, destRect;
        Action invalidate;

        PointF topleft = new PointF();
        PointF top1left1 = new PointF(1, 1);

        bool running;

        public delegate void FinishEventHandler(object sender, EventArgs e);
        public event FinishEventHandler Finish;

        Animator detachOnFinish = null; // Se != null, quando l'animazione finisce allora questo Animator deve fare il detach.

        public void SetDetachOnFinish(bool detachOnFinish, Animator animator)
        {
            this.detachOnFinish = detachOnFinish ? animator : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frameInterval">Parametro >= 0.
        /// Se 0, l'animazione va alla stessa frequenza del timer.
        /// Se 1, l'animazione va alla metà della frequenza del timer, ecc.</param>
        public Animation(int frameInterval, int numberOfFrames, NinePatch from, NinePatch to, Action invalidate)
        {
            if (numberOfFrames <= 0) throw new ArgumentException("numberOfFrames should be > 0");
            this.frameInterval = frameInterval;
            this.numberOfFrames = numberOfFrames;
            this.from = from;
            this.to = to;
            this.invalidate = invalidate;

            this.curFrame = new NinePatch(from.Image.Clone(new RectangleF(topleft, from.Image.Size), System.Drawing.Imaging.PixelFormat.Format32bppArgb));

            srcRect = new RectangleF(1, 1, from.Image.Size.Width - 2, from.Image.Size.Height - 2);
            destRect = new RectangleF(1, 1, to.Image.Size.Width - 2, to.Image.Size.Height - 2);

            Reset();
        }

        public void Reset()
        {
            toNextFrame = 0;
            frameCount = 0;
            running = false;
        }

        public void Start()
        {
            Start(0);
        }

        public void Start(float startPoint)
        {
            if (startPoint < 0 || startPoint > 1) throw new ArgumentException("startPoint should be between 0 and 1");
            frameCount = (int)(startPoint * numberOfFrames);

            running = true;
            DoAnimation();
        }

        /// <summary>
        /// Ferma l'animazione e restituisce la percentuale di completamento.
        /// </summary>
        /// <returns></returns>
        public float Stop()
        {
            running = false;
            return (float)frameCount / (float)numberOfFrames;
        }

        public bool IsRunning()
        {
            return running;
        }

        public void DoAnimation()
        {
            if (running)
            {
                if (this.toNextFrame == 0)
                {
                    frameCount++;
                    FadingEasingFunction((float)frameCount / (float)numberOfFrames);
                    invalidate();
                }


                if (toNextFrame == 0)
                    toNextFrame = frameInterval;
                else
                    toNextFrame -= 1;

                if (frameCount == numberOfFrames)
                {
                    Stop();

                    if (detachOnFinish != null)
                        detachOnFinish.Detach(this);

                    if (Finish != null) Finish(this, new EventArgs());
                }
            }
        }

        public NinePatch GetCurrentFrame()
        {
            return curFrame;
        }

        ColorMatrix fading_colorMatrix = new ColorMatrix();
        ImageAttributes fading_imageAtt = new ImageAttributes();
        private void FadingEasingFunction(float percent)
        {
            using (var g = Graphics.FromImage(curFrame.Image))
            {
                g.DrawImage(from.Image, destRect, srcRect, GraphicsUnit.Pixel);

                fading_colorMatrix.Matrix33 = percent;

                fading_imageAtt.SetColorMatrix(
                   fading_colorMatrix,
                   ColorMatrixFlag.Default,
                   ColorAdjustType.Bitmap);

                var destPoints = new PointF[] {
                            top1left1,
                            new PointF(to.Image.Size.Width - 1, 1),
                            new PointF(1, to.Image.Size.Height - 1)
                        };
                g.DrawImage(to.Image, destPoints, destRect, GraphicsUnit.Pixel, fading_imageAtt);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                fading_imageAtt.Dispose();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
