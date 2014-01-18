using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace PlayerUI.PlayerControls
{
    public partial class Container : PlayerControl
    {
        public partial class PlayerControlCollection : Collection<PlayerControl>
        {
            private int ignoreInsertEvent = 0;
            private int ignoreRemoveEvent = 0;

            private Container owner = null;
            public Container Owner { get { return this.owner; } }

            public PlayerControlCollection(Container owner)
                : base()
            {
                this.owner = owner;
            }

            public void AddFirst(PlayerControl item)
            {
                this.Insert(0, item);
            }

            public void MoveToFirst(PlayerControl item)
            {
                this.ignoreRemoveEvent++;
                this.Remove(item);
                this.ignoreInsertEvent++;
                this.Insert(0, item);
            }

            public void MoveToLast(PlayerControl item)
            {
                this.ignoreRemoveEvent++;
                this.Remove(item);
                this.ignoreInsertEvent++;
                this.Add(item);
            }

            protected override void InsertItem(int index, PlayerControl item)
            {
                base.InsertItem(index, item);

                if (ignoreInsertEvent == 0)
                    owner.OnControlAdded(item);
                else
                    ignoreInsertEvent--;
            }

            protected override void RemoveItem(int index)
            {
                PlayerControl c = this[index];
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

            protected override void SetItem(int index, PlayerControl item)
            {
                throw new NotSupportedException();
                //base.SetItem(index, item);
            }
        }
    }
}
