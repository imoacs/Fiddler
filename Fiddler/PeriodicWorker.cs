namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    internal class PeriodicWorker
    {
        private List<taskItem> oTaskList = new List<taskItem>();
        private Timer timerInternal;

        internal PeriodicWorker()
        {
            this.timerInternal = new Timer(new TimerCallback(this.doWork), null, 0x3e8, 0x3e8);
        }

        internal taskItem assignWork(SimpleEventHandler workFunction, uint iMS)
        {
            taskItem item = new taskItem(workFunction, iMS);
            lock (this.oTaskList)
            {
                this.oTaskList.Add(item);
            }
            return item;
        }

        private void doWork(object objState)
        {
            if (!FiddlerApplication.isClosing)
            {
                taskItem[] itemArray;
                lock (this.oTaskList)
                {
                    itemArray = new taskItem[this.oTaskList.Count];
                    this.oTaskList.CopyTo(itemArray);
                }
                foreach (taskItem item in itemArray)
                {
                    if (Environment.TickCount > (item._iLastRun + item._iPeriod))
                    {
                        item._oTask();
                        item._iLastRun = Environment.TickCount;
                    }
                }
            }
        }

        internal void revokeWork(taskItem oToRevoke)
        {
            lock (this.oTaskList)
            {
                this.oTaskList.Remove(oToRevoke);
            }
        }

        internal class taskItem
        {
            public int _iLastRun = Environment.TickCount;
            public uint _iPeriod;
            public SimpleEventHandler _oTask;

            public taskItem(SimpleEventHandler oTask, uint iPeriod)
            {
                this._iPeriod = iPeriod;
                this._oTask = oTask;
            }
        }
    }
}

