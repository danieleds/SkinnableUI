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
