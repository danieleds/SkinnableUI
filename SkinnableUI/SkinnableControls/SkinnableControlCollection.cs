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
using System.Collections.ObjectModel;

namespace SkinnableUI.SkinnableControls
{
    public partial class Container : SkinnableControl
    {
        public partial class SkinnableControlCollection : Collection<SkinnableControl>
        {
            private int ignoreInsertEvent = 0;
            private int ignoreRemoveEvent = 0;

            private Container owner = null;
            public Container Owner { get { return this.owner; } }

            public SkinnableControlCollection(Container owner)
                : base()
            {
                this.owner = owner;
            }

            public void AddFirst(SkinnableControl item)
            {
                this.Insert(0, item);
            }

            public void MoveToFirst(SkinnableControl item)
            {
                this.ignoreRemoveEvent++;
                this.Remove(item);
                this.ignoreInsertEvent++;
                this.Insert(0, item);
            }

            public void MoveToLast(SkinnableControl item)
            {
                this.ignoreRemoveEvent++;
                this.Remove(item);
                this.ignoreInsertEvent++;
                this.Add(item);
            }

            protected override void InsertItem(int index, SkinnableControl item)
            {
                base.InsertItem(index, item);

                if (ignoreInsertEvent == 0)
                    owner.OnControlAdded(item);
                else
                    ignoreInsertEvent--;
            }

            protected override void RemoveItem(int index)
            {
                SkinnableControl c = this[index];
                base.RemoveItem(index);

                if (ignoreRemoveEvent == 0)
                    owner.OnControlRemoved(c);
                else
                    ignoreRemoveEvent--;
            }

            protected override void ClearItems()
            {
                throw new NotSupportedException();
                //base.ClearItems();
            }

            protected override void SetItem(int index, SkinnableControl item)
            {
                throw new NotSupportedException();
                //base.SetItem(index, item);
            }
        }
    }
}
