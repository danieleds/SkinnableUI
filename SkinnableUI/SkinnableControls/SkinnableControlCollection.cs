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
