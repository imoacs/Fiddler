namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Threading;
    using System.Windows.Forms;

    internal class QuickExec : TextBox
    {
        private int iCurrentCommand;
        private ExecuteHandler _OnExecute;
        private List<string> slCommandHistory = new List<string>();

        public event ExecuteHandler OnExecute
        {
            add
            {
                ExecuteHandler handler2;
                ExecuteHandler onExecute = this._OnExecute;
                do
                {
                    handler2 = onExecute;
                    ExecuteHandler handler3 = (ExecuteHandler) Delegate.Combine(handler2, value);
                    onExecute = Interlocked.CompareExchange<ExecuteHandler>(ref this._OnExecute, handler3, handler2);
                }
                while (onExecute != handler2);
            }
            remove
            {
                ExecuteHandler handler2;
                ExecuteHandler onExecute = this._OnExecute;
                do
                {
                    handler2 = onExecute;
                    ExecuteHandler handler3 = (ExecuteHandler) Delegate.Remove(handler2, value);
                    onExecute = Interlocked.CompareExchange<ExecuteHandler>(ref this._OnExecute, handler3, handler2);
                }
                while (onExecute != handler2);
            }
        }

        internal QuickExec()
        {
            base.AutoCompleteSource = AutoCompleteSource.CustomSource;
            base.AutoCompleteMode = AutoCompleteMode.Append;
            base.Font = new Font("Tahoma", CONFIG.flFontSize);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Up:
                    if (this.iCurrentCommand > 0)
                    {
                        this.iCurrentCommand--;
                        base.Clear();
                        if (this.iCurrentCommand < this.slCommandHistory.Count)
                        {
                            this.SelectedText = this.slCommandHistory[this.iCurrentCommand];
                        }
                    }
                    return true;

                case Keys.Down:
                    if (this.iCurrentCommand < this.slCommandHistory.Count)
                    {
                        this.iCurrentCommand++;
                        base.Clear();
                        if ((this.iCurrentCommand >= 0) && (this.iCurrentCommand < this.slCommandHistory.Count))
                        {
                            this.SelectedText = this.slCommandHistory[this.iCurrentCommand];
                        }
                    }
                    return true;

                case (Keys.Control | Keys.A):
                case (Keys.Alt | Keys.Q):
                    base.SelectAll();
                    return true;

                case Keys.Return:
                {
                    string sCommand = this.Text.Trim();
                    if (sCommand.Length < 1)
                    {
                        base.Clear();
                        return true;
                    }
                    if (this._OnExecute != null)
                    {
                        FiddlerApplication.logSelfHost(0x15);
                        if (this._OnExecute(sCommand))
                        {
                            base.AutoCompleteCustomSource.Add(sCommand);
                            this.slCommandHistory.Add(sCommand);
                            this.iCurrentCommand = this.slCommandHistory.Count;
                        }
                    }
                    base.Clear();
                    return true;
                }
                case Keys.Escape:
                    if (this.SelectedText.Length > 0)
                    {
                        this.SelectedText = string.Empty;
                    }
                    else
                    {
                        base.Clear();
                    }
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}

