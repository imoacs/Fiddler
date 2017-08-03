namespace Fiddler
{
    using System;
    using System.Runtime.CompilerServices;

    public class ProgressCallbackEventArgs : EventArgs
    {
        private readonly int _PercentDone;
        private readonly string _sProgressText;

        public ProgressCallbackEventArgs(float flCompletionRatio, string sProgressText)
        {
            this._sProgressText = sProgressText ?? string.Empty;
            this._PercentDone = (int) Math.Truncate((double) (100f * Math.Max(0f, Math.Min(1f, flCompletionRatio))));
        }

        public bool Cancel { get; set; }

        public int PercentComplete
        {
            get
            {
                return this._PercentDone;
            }
        }

        public string ProgressText
        {
            get
            {
                return this._sProgressText;
            }
        }
    }
}

