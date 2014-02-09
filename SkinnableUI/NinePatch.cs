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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;

namespace SkinnableUI
{
    public class NinePatch
    {
        private Bitmap image;
        /// <summary>
        /// Riferimento all'immagine 9-patch (comprende i pixel indicatori)
        /// </summary>
        public Bitmap Image { get { return image; } }

        // Array di (inizio[px], fine[px], lunghezza[px]=fine-inizio+1, espandibile?)
        private float totalFixedX = 0, totalFixedY = 0;
        private List<Tuple<float, float, float, bool>> xLines = new List<Tuple<float, float, float, bool>>();
        private List<Tuple<float, float, float, bool>> yLines = new List<Tuple<float, float, float, bool>>();

        private float paddingTop = 0, paddingBottom = 0, paddingLeft = 0, paddingRight = 0;

        public NinePatch(Bitmap image)
        {
            if (image.Width < 2 || image.Height < 2)
                throw new ArgumentException("Immagine troppo piccola. Dimensione minima: 2px * 2px.");

            this.image = image;

            // Conta i pixel neri in alto
            int i = 1, j;
            while (i < image.Width - 1)
            {
                Color startColor = image.GetPixel(i, 0);
                j = i;
                while (j + 1 < image.Width - 1 && (IsBlack(startColor) ? IsBlack(image.GetPixel(j + 1, 0)) : !IsBlack(image.GetPixel(j + 1, 0))))
                {
                    j++;
                }
                bool expand = IsBlack(image.GetPixel(j, 0));
                if (!expand) totalFixedX += j - i + 1;
                xLines.Add(new Tuple<float, float, float, bool>(i, j, j - i + 1, expand));
                i = j + 1;
            }

            // Conta i pixel neri a sinistra
            i = 1;
            while (i < image.Height - 1)
            {
                Color startColor = image.GetPixel(0, i);
                j = i;
                while (j + 1 < image.Height - 1 && (IsBlack(startColor) ? IsBlack(image.GetPixel(0, j + 1)) : !IsBlack(image.GetPixel(0, j + 1))))
                {
                    j++;
                }
                bool expand = IsBlack(image.GetPixel(0, j));
                if (!expand) totalFixedY += j - i + 1;
                yLines.Add(new Tuple<float, float, float, bool>(i, j, j - i + 1, expand));
                i = j + 1;
            }

            // Conta i pixel neri a destra
            i = 1; j = 0;
            while (i < image.Height - 1 && !IsBlack(image.GetPixel(image.Width - 1, i))) { i++; j++; }
            paddingTop = j;
            while (i < image.Height - 1 && IsBlack(image.GetPixel(image.Width - 1, i))) { i++; }
            j = 0;
            while (i < image.Height - 1 && !IsBlack(image.GetPixel(image.Width - 1, i))) { i++; j++; }
            paddingBottom = j;
            if (paddingBottom == 0 && paddingTop == image.Height - 2)
            {
                // Non c'era la riga nera: settiamo convenzionalmente il padding a 0
                paddingTop = 0;
                paddingBottom = 0;
            }

            // Conta i pixel neri in basso
            i = 1; j = 0;
            while (i < image.Width - 1 && !IsBlack(image.GetPixel(i, image.Height - 1))) { i++; j++; }
            paddingLeft = j;
            while (i < image.Width - 1 && IsBlack(image.GetPixel(i, image.Height - 1))) { i++; }
            j = 0;
            while (i < image.Width - 1 && !IsBlack(image.GetPixel(i, image.Height - 1))) { i++; j++; }
            paddingRight = j;
            if (paddingRight == 0 && paddingLeft == image.Width - 2)
            {
                // Non c'era la riga nera: settiamo convenzionalmente il padding a 0
                paddingLeft = 0;
                paddingRight = 0;
            }

        }

        private bool IsBlack(Color c)
        {
            return c.A == 255 && c.R == 0 && c.G == 0 && c.B == 0;
        }

        public void Paint(Graphics g, RectangleF rect)
        {
            g.TranslateTransform(rect.X, rect.Y);
            this.Paint(g, new SizeF(rect.Size.Width + 1, rect.Size.Height + 1));
            g.TranslateTransform(-rect.X, -rect.Y);
        }

        /// <summary>
        /// Disegna l'immagie scalata secondo le regole del nine-patch corrente.
        /// </summary>
        /// <param name="g">Oggetto Graphics su cui disegnare.</param>
        /// <param name="size">Dimensione con cui disegnare l'immagine.</param>
        public void Paint(Graphics g, SizeF size)
        {
            var t = g.Save();

            // Per evitare problemi sul taglio dei pixel, impostiamo PixelOffsetMode e InterpolationMode.
            //   http://stackoverflow.com/questions/14070311/why-is-graphics-drawimage-cropping-part-of-my-image
            //   http://stackoverflow.com/questions/10099687/im-experiencing-unexpected-results-from-graphics-drawimage
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None; // anche Default va bene, ma forziamo None per sicurezza
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            // Per lo stesso motivo di prima (taglio dei pixel) facciamo il clip leggermente più in alto a sx.
            g.SetClip(new RectangleF(-0.5f, -0.5f, size.Width, size.Height), System.Drawing.Drawing2D.CombineMode.Intersect);

            float y = 0;
            for (int yi = 0; yi < yLines.Count; yi++)
            {
                float x = 0;

                float curLineHeight;
                if (yLines[yi].Item4 == false)
                    curLineHeight = yLines[yi].Item3;
                else
                    // originalLineLength : totalFluid = x : newTotalFluid
                    curLineHeight = yLines[yi].Item3 * (size.Height - totalFixedY) / (image.Height - 2 - totalFixedY);

                for (int xi = 0; xi < xLines.Count; xi++)
                {
                    RectangleF destRect = new RectangleF();
                    destRect.X = x;
                    destRect.Y = y;

                    if (xLines[xi].Item4 == false)
                        destRect.Width = xLines[xi].Item3;
                    else
                        destRect.Width = xLines[xi].Item3 * (size.Width - totalFixedX) / (image.Width - 2 - totalFixedX);

                    x += destRect.Width;

                    destRect.Height = curLineHeight;

                    // Per lo stesso motivo di prima (taglio dei pixel) spostiamo i rettangoli sorgente e destinazione
                    // di -0.5px in alto e a sinistra.
                    g.DrawImage(
                        this.image,
                        new RectangleF(destRect.X - 0.5f, destRect.Y - 0.5f, destRect.Width, destRect.Height),
                        new RectangleF(xLines[xi].Item1 - 0.5f, yLines[yi].Item1 - 0.5f, xLines[xi].Item3, yLines[yi].Item3),
                        GraphicsUnit.Pixel);

                    /*g.DrawImage(this.image, destRect,
                        new RectangleF(xLines[xi].Item1, yLines[yi].Item1, xLines[xi].Item3, yLines[yi].Item3),
                        GraphicsUnit.Pixel);*/
                }

                y += curLineHeight;
            }

            g.Restore(t);
        }

        public RectangleF GetContentBox(SizeF size)
        {
            return new RectangleF(paddingLeft, paddingTop, size.Width - paddingLeft - paddingRight - 1, size.Height - paddingTop - paddingBottom - 1);
        }
    }
}
