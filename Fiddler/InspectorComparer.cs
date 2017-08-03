namespace Fiddler
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows.Forms;

    internal class InspectorComparer : IComparer<TabPage>
    {
        private Hashtable m_Inspectors;

        internal InspectorComparer(Hashtable owningList)
        {
            this.m_Inspectors = owningList;
        }

        public int Compare(TabPage x, TabPage y)
        {
            return (((Inspector2) this.m_Inspectors[x]).GetOrder() - ((Inspector2) this.m_Inspectors[y]).GetOrder());
        }
    }
}

