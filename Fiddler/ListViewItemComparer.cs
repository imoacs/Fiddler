namespace Fiddler
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Windows.Forms;

    internal class ListViewItemComparer : IComparer
    {
        internal bool ascending = true;
        public bool bStringCompare = false;
        private int col = 0;

        public int Compare(object x, object y)
        {
            int num = -1;
            ListViewItem item = (ListViewItem) x;
            ListViewItem item2 = (ListViewItem) y;
            if (item.SubItems.Count <= this.col)
            {
                num = -1;
            }
            else if (item2.SubItems.Count <= this.col)
            {
                num = 1;
            }
            else if (this.bStringCompare)
            {
                num = string.Compare(item.SubItems[this.col].Text, item2.SubItems[this.col].Text, StringComparison.Ordinal);
            }
            else
            {
                try
                {
                    num = int.Parse(item.SubItems[this.col].Text, NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign) - int.Parse(item2.SubItems[this.col].Text, NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign);
                }
                catch (Exception exception)
                {
                    FiddlerApplication.DebugSpew(exception.Message + "\n" + item.SubItems[this.col].Text + "\n" + item2.SubItems[this.col].Text);
                }
            }
            if (!this.ascending)
            {
                num = -num;
            }
            return num;
        }

        public int Column
        {
            get
            {
                return this.col;
            }
            set
            {
                if (value == this.col)
                {
                    this.ascending = !this.ascending;
                }
                this.col = value;
            }
        }
    }
}

