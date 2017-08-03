namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public class SessionListView : ListView
    {
        internal string[] _emptyBoundColumns = new string[0];
        private int _uiAsyncUpdateInterval;
        private Container components;
        private static Dictionary<string, BoundColumnEntry> dictBoundColumns = new Dictionary<string, BoundColumnEntry>();
        private const int LVM_SUBITEMHITTEST = 0x1039;
        public SimpleEventHandler OnSessionsAdded;
        private List<ListViewItem> qLVIsToAdd;
        private Timer timerLVIAddQueue;
        private WeakReference wrCurrentItem;
        private WeakReference wrPriorItem;

        public SessionListView()
        {
            this.InitializeComponent();
            if (!FiddlerApplication.Prefs.GetBoolPref("experiment.nodoublebuffer", false))
            {
                this.DoubleBuffered = true;
            }
            base.ListViewItemSorter = new ListViewItemComparer();
            base.ColumnClick += new ColumnClickEventHandler(this.OnColumnClick);
            base.MouseDown += new MouseEventHandler(this.SessionListView_MouseDown);
        }

        internal void ActivatePreviousItem()
        {
            if (this.wrPriorItem != null)
            {
                ListViewItem target = this.wrPriorItem.Target as ListViewItem;
                if (target != null)
                {
                    base.SelectedItems.Clear();
                    target.Selected = true;
                    target.Focused = true;
                }
            }
        }

        public bool AddBoundColumn(string sColumnTitle, int iWidth, getColumnStringDelegate delFn)
        {
            return (((sColumnTitle != null) && (delFn != null)) && this.AddBoundColumn(sColumnTitle, iWidth, null, delFn));
        }

        public bool AddBoundColumn(string sColumnTitle, int iWidth, string sSessionFlagName)
        {
            getColumnStringDelegate delFn = null;
            getColumnStringDelegate delegate3 = null;
            if ((sColumnTitle == null) || (sSessionFlagName == null))
            {
                return false;
            }
            if (sSessionFlagName.StartsWith("@request.", StringComparison.OrdinalIgnoreCase))
            {
                if (delFn == null)
                {
                    delFn = delegate (Session oSession) {
                        if (((oSession != null) && (oSession.oRequest != null)) && (oSession.oRequest.headers != null))
                        {
                            return oSession.oRequest.headers[sSessionFlagName.Substring(9)];
                        }
                        return string.Empty;
                    };
                }
                return this.AddBoundColumn(sColumnTitle, iWidth, null, delFn);
            }
            if (!sSessionFlagName.StartsWith("@response.", StringComparison.OrdinalIgnoreCase))
            {
                return this.AddBoundColumn(sColumnTitle, iWidth, sSessionFlagName, null);
            }
            if (delegate3 == null)
            {
                delegate3 = delegate (Session oSession) {
                    if (((oSession != null) && (oSession.oResponse != null)) && (oSession.oResponse.headers != null))
                    {
                        return oSession.oResponse.headers[sSessionFlagName.Substring(10)];
                    }
                    return string.Empty;
                };
            }
            return this.AddBoundColumn(sColumnTitle, iWidth, null, delegate3);
        }

        private bool AddBoundColumn(string sColumnTitle, int iWidth, string sSessionFlagName, getColumnStringDelegate delFn)
        {
            BoundColumnEntry entry;
            if (dictBoundColumns.ContainsKey(sColumnTitle))
            {
                entry = dictBoundColumns[sColumnTitle];
                entry._delFn = delFn;
                entry._sSessionFlagName = sSessionFlagName;
                return true;
            }
            base.Columns.Add(sColumnTitle, iWidth);
            int iColNum = base.Columns.Count - 1;
            if (delFn != null)
            {
                entry = new BoundColumnEntry(delFn, iColNum);
            }
            else
            {
                entry = new BoundColumnEntry(sSessionFlagName, iColNum);
            }
            dictBoundColumns.Add(sColumnTitle, entry);
            this._emptyBoundColumns = new string[dictBoundColumns.Count];
            for (int i = 0; i < this._emptyBoundColumns.Length; i++)
            {
                this._emptyBoundColumns[i] = string.Empty;
            }
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void DoSessionsAdded(bool bWasAtBottom)
        {
            if (CONFIG.bAutoScroll && (!CONFIG.bSmartScroll || bWasAtBottom))
            {
                int index = base.Items.Count - 1;
                if (index > -1)
                {
                    base.EnsureVisible(index);
                }
            }
            if (this.OnSessionsAdded != null)
            {
                this.OnSessionsAdded();
            }
        }

        public void EnsureItemIsVisible(ListViewItem oLVI)
        {
            if (oLVI.Index > -1)
            {
                oLVI.EnsureVisible();
            }
        }

        internal void FillBoundColumns(Session oSession, ListViewItem oLVI)
        {
            foreach (BoundColumnEntry entry in dictBoundColumns.Values)
            {
                if (oLVI.SubItems.Count > entry._iColNum)
                {
                    if (entry._sSessionFlagName != null)
                    {
                        oLVI.SubItems[entry._iColNum].Text = oSession[entry._sSessionFlagName];
                    }
                    else
                    {
                        oLVI.SubItems[entry._iColNum].Text = entry._delFn(oSession);
                    }
                }
            }
        }

        internal void FlushUpdates()
        {
            if ((this.qLVIsToAdd != null) && (this.qLVIsToAdd.Count > 0))
            {
                this.timerLVIAddQueue_Tick(null, null);
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll")]
        private static extern bool GetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);
        public int GetSubItemIndexFromPoint(Point ptClient)
        {
            LVHITTESTINFO lParam = new LVHITTESTINFO {
                pt_x = ptClient.X,
                pt_y = ptClient.Y
            };
            if (-1 < ((int) SendHitTestMessage(new HandleRef(this, base.Handle), 0x1039, 0, lParam)))
            {
                return lParam.iSubItem;
            }
            return -1;
        }

        private void InitializeComponent()
        {
            this.components = new Container();
        }

        public bool IsScrolledToBottom()
        {
            SCROLLINFO scrollinfo=new SCROLLINFO();
            scrollinfo = new SCROLLINFO {
                cbSize = (uint) Marshal.SizeOf(scrollinfo),
                fMask = 7
            };
            if (GetScrollInfo(base.Handle, 1, ref scrollinfo))
            {
                return (scrollinfo.nPos >= ((scrollinfo.nMax - scrollinfo.nPage) - ((long) 6L)));
            }
            return true;
        }

        private void OnColumnClick(object sender, ColumnClickEventArgs e)
        {
            ((ListViewItemComparer) base.ListViewItemSorter).Column = e.Column;
            ((ListViewItemComparer) base.ListViewItemSorter).bStringCompare = (e.Column != 0) && (e.Column != 5);
            base.BeginUpdate();
            base.Sort();
            if (base.SelectedIndices.Count > 0)
            {
                base.EnsureVisible(base.SelectedIndices[0]);
            }
            base.EndUpdate();
        }

        internal void QueueItem(ListViewItem lvi)
        {
            if (this._uiAsyncUpdateInterval > 0)
            {
                lock (this.qLVIsToAdd)
                {
                    this.qLVIsToAdd.Add(lvi);
                    this.timerLVIAddQueue.Enabled = true;
                    return;
                }
            }
            bool bWasAtBottom = false;
            if (CONFIG.bAutoScroll && CONFIG.bSmartScroll)
            {
                bWasAtBottom = this.IsScrolledToBottom();
            }
            base.Items.Add(lvi);
            this.DoSessionsAdded(bWasAtBottom);
        }

        internal void RemoveOrDequeue(ListViewItem lvi)
        {
            if (lvi.Index > -1)
            {
                lvi.Remove();
            }
            else
            {
                lvi.Text = "!!!";
                lock (this.qLVIsToAdd)
                {
                    this.qLVIsToAdd.Remove(lvi);
                }
            }
        }

        [DllImport("user32.dll", EntryPoint="SendMessage", CharSet=CharSet.Auto)]
        private static extern IntPtr SendHitTestMessage(HandleRef hWnd, int msg, int wParam, LVHITTESTINFO lParam);
        private void SessionListView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.XButton1)
            {
                this.ActivatePreviousItem();
            }
        }

        internal void StoreActiveItem()
        {
            this.wrPriorItem = this.wrCurrentItem;
            if (base.SelectedItems.Count == 1)
            {
                this.wrCurrentItem = new WeakReference(base.SelectedItems[0]);
            }
        }

        private void timerLVIAddQueue_Tick(object sender, EventArgs e)
        {
            if (!FiddlerApplication.isClosing)
            {
                ListViewItem[] itemArray;
                bool bWasAtBottom = false;
                if (CONFIG.bAutoScroll && CONFIG.bSmartScroll)
                {
                    bWasAtBottom = this.IsScrolledToBottom();
                }
                lock (this.qLVIsToAdd)
                {
                    itemArray = this.qLVIsToAdd.ToArray();
                    this.qLVIsToAdd.Clear();
                    this.timerLVIAddQueue.Enabled = false;
                }
                if (itemArray.Length > 0)
                {
                    base.Items.AddRange(itemArray);
                    itemArray = null;
                    this.DoSessionsAdded(bWasAtBottom);
                }
            }
        }

        public int TotalItemCount()
        {
            int count = base.Items.Count;
            if (this.qLVIsToAdd == null)
            {
                return count;
            }
            lock (this.qLVIsToAdd)
            {
                return (count + this.qLVIsToAdd.Count);
            }
        }

        internal int uiAsyncUpdateInterval
        {
            get
            {
                return this._uiAsyncUpdateInterval;
            }
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                if (value > 0x1388)
                {
                    value = 0x1388;
                }
                if ((value > 0) != (this._uiAsyncUpdateInterval > 0))
                {
                    if (value > 0)
                    {
                        if (this.timerLVIAddQueue == null)
                        {
                            this.timerLVIAddQueue = new Timer();
                            this.timerLVIAddQueue.Interval = value;
                            this.timerLVIAddQueue.Tick += new EventHandler(this.timerLVIAddQueue_Tick);
                            this.qLVIsToAdd = new List<ListViewItem>();
                        }
                    }
                    else
                    {
                        this.FlushUpdates();
                    }
                }
                this._uiAsyncUpdateInterval = value;
                if (value > 0)
                {
                    this.timerLVIAddQueue.Interval = value;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        private class LVHITTESTINFO
        {
            public int pt_x;
            public int pt_y;
            public int flags;
            public int iItem;
            public int iSubItem;
        }

        private enum ScrollBarDirection
        {
            SB_HORZ,
            SB_VERT,
            SB_CTL,
            SB_BOTH
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }

        [Flags]
        private enum ScrollInfoMask
        {
            SIF_ALL = 0x17,
            SIF_DISABLENOSCROLL = 8,
            SIF_PAGE = 2,
            SIF_POS = 4,
            SIF_RANGE = 1,
            SIF_TRACKPOS = 0x10
        }
    }
}

