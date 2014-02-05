﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace ExtensionMethods
{
    static class PlayerExtensionMethods
    {
        /// <summary>
        /// Arrotonda un RectangleF per eccesso (X e Y vengono arrotondate per difetto, Width e Height per eccesso).
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static Rectangle RoundUp(this RectangleF rect)
        {
            return new Rectangle((int)Math.Floor(rect.X), (int)Math.Floor(rect.Y), (int)Math.Ceiling(rect.Width), (int)Math.Ceiling(rect.Height));
        }

        public static RectangleF Expand(this RectangleF rect, float expand)
        {
            return new RectangleF(rect.X - expand, rect.Y - expand, rect.Width + expand * 2, rect.Height + expand * 2);
        }

        public static SizeF Expand(this SizeF size, float expand)
        {
            return new SizeF(size.Width + expand, size.Height + expand);
        }

        public static void ForEach<T>(this System.Collections.ObjectModel.Collection<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }
    }
}
