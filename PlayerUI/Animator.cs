using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayerUI
{
    public class Animator : IDisposable
    {
        const int min_interval = 50;

        Timer tmr = new Timer();
        List<Animation> animations = new List<Animation>();
        List<Animation> detachQueue = new List<Animation>();

        public Animator()
        {
            tmr.Tick += tmr_Tick;
            tmr.Interval = min_interval;
        }

        void tmr_Tick(object sender, EventArgs e)
        {
            detachQueue.ForEach(item => animations.Remove(item));
            detachQueue.Clear();

            if (animations.Count == 0)
            {
                tmr.Stop();
            }
            else
            {
                foreach (var item in animations)
                {
                    if (!detachQueue.Contains(item))
                        item.DoAnimation();
                }
            }
        }

        /// <summary>
        /// Aggancia un'animazione a questo Animator. Eseguire il detach il prima possibile.
        /// </summary>
        /// <param name="interval">Numero di millisecondi ogni quanto eseguire un frame (valori più bassi indicano una frequenza maggiore).</param>
        /// <param name="duration">Durata dell'animazione (millisecondi).</param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="invalidate"></param>
        /// <returns></returns>
        public Animation Attach(int interval, int duration, NinePatch from, NinePatch to, Action invalidate)
        {
            if (interval < min_interval)
                interval = min_interval;

            if (duration < interval)
                duration = interval;

            var anim = new Animation(
                (int)Math.Round((float)interval / (float)min_interval, 0, MidpointRounding.ToEven) - 1,
                duration / interval,
                from,
                to,
                invalidate);

            animations.Add(anim);
            if(!tmr.Enabled) tmr.Start();
            return anim;
        }

        public void Detach(Animation anim)
        {
            detachQueue.Add(anim);
        }

        /// <summary>
        /// Esegue un'animazione salvando l'oggetto Animation in animationPlaceholder.
        /// Una chiamata successiva di questo metodo sullo stesso animationPlaceholder
        /// eseguirà la nuova animazione a partire dall'istante in cui era arrivata quella vecchia.
        /// </summary>
        /// <param name="animationPlaceholder">Oggetto Animation per tenere traccia dell'animazione corrente. Alla prima chiamata del metodo può essere null.</param>
        /// <param name="to">NinePatch verso cui eseguire l'animazione.</param>
        /// <param name="defaultFrom">NinePatch da cui far partire l'animazione nel caso in cui animationPlaceholder sia null.</param>
        /// <param name="interval">Numero di millisecondi ogni quanto eseguire un frame (valori più bassi indicano una frequenza maggiore).</param>
        /// <param name="duration">Durata dell'animazione (millisecondi).</param>
        /// <param name="invalidate">Metodo per richiedere l'invalidazione grafica.</param>
        public void ContinueAnimation(ref Animation animationPlaceholder, NinePatch to, NinePatch defaultFrom, int interval, int duration, Action invalidate)
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

            animationPlaceholder = this.Attach(interval, (int)(duration * p), startFrame, to, invalidate);
            animationPlaceholder.SetDetachOnFinish(true, this);
            animationPlaceholder.Start();
        }


        public void ContinueAnimationB(ref Animation animationPlaceholder, NinePatch from, NinePatch to, int interval, int duration, Action invalidate)
        {
            float p;
            if (animationPlaceholder == null)
            {
                p = 1;
            }
            else
            {
                p = animationPlaceholder.Stop();
            }

            animationPlaceholder = this.Attach(interval, (int)(duration * p), from, to, invalidate);
            animationPlaceholder.SetDetachOnFinish(true, this);
            animationPlaceholder.Start();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                tmr.Dispose();
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
