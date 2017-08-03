namespace Fiddler
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Net.NetworkInformation;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Web;
    using System.Windows.Forms;

    internal static class FiddlerToolbar
    {
        private static bool _bCapturing;
        private static IntPtr _hInvertedWindow;
        private static int _iFilterPID;
        private static RECT _rectInvertedWindow;
        private static object oShowHideLock = new object();
        private static ToolStripButton tsbRemoveEncodings = null;
        private static ToolStripButton tsbStreaming = null;
        private static ToolStripButton tsbWindowName = null;
        private static ToolStripLabel tslNetStat = null;
        private static ToolStripLabel tslProcessFilter = null;
        private static ToolStrip tstripMain = null;

        static FiddlerToolbar()
        {
            FiddlerApplication.Prefs.AddWatcher("fiddler.ui", new EventHandler<PrefChangeEventArgs>(FiddlerToolbar.HandlePrefChanged));
        }

        private static void _DoHide()
        {
            if (tstripMain != null)
            {
                lock (oShowHideLock)
                {
                    tsbWindowName = null;
                    tslNetStat = null;
                    tsbRemoveEncodings = null;
                    tsbStreaming = null;
                    tstripMain.Dispose();
                    tstripMain = null;
                    FiddlerApplication._iShowOnlyPID = 0;
                }
            }
        }

        private static void _DoShow()
        {
            if (tstripMain == null)
            {
                object obj2;
                Monitor.Enter(obj2 = oShowHideLock);
                try
                {
                    tstripMain = new ToolStrip();
                    if (CONFIG.flFontSize >= 10f)
                    {
                        tstripMain.Font = new Font(tstripMain.Font.FontFamily, 10f);
                    }
                    tstripMain.SuspendLayout();
                    tstripMain.ImageList = FiddlerApplication.UI.imglToolbar;
                    tstripMain.GripStyle = ToolStripGripStyle.Hidden;
                    tstripMain.RenderMode = ToolStripRenderMode.System;
                    tstripMain.Renderer = new TSRenderWithoutBorder();
                    tstripMain.AllowItemReorder = true;
                    tstripMain.ShowItemToolTips = true;
                    if (CONFIG.bIsViewOnly)
                    {
                        tstripMain.RenderMode = ToolStripRenderMode.Professional;
                        tsbWindowName = new ToolStripButton("FiddlerViewer");
                        tsbWindowName.Font = new Font(tsbWindowName.Font, FontStyle.Bold);
                        tsbWindowName.Padding = new Padding(5, 0, 15, 0);
                        tsbWindowName.Click += delegate (object sender, EventArgs e) {
                            tsbWindowName.Text = frmPrompt.GetUserString("FiddlerViewer Name", "Enter a new title for this viewer:", tsbWindowName.Text);
                        };
                        tstripMain.Items.Add(tsbWindowName);
                    }
                    ToolStripButton button = new ToolStripButton("") {
                        ToolTipText = "Add a comment to the selected sessions.",
                        ImageKey = "comment"
                    };
                    button.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.UI.actCommentSelectedSessions();
                    };
                    tstripMain.Items.Add(button);
                    button = new ToolStripButton("Reissue") {
                        ToolTipText = "Reissue the selected requests.\nHold CTRL to reissue unconditionally.\nHold SHIFT to reissue multiple times.",
                        ImageKey = "refresh"
                    };
                    button.Click += delegate (object sender, EventArgs e) {
                        if (Utilities.GetAsyncKeyState(0x11) < 0)
                        {
                            FiddlerApplication.UI.actUnconditionallyReissueSelected();
                        }
                        else
                        {
                            FiddlerApplication.UI.actReissueSelected();
                        }
                    };
                    tstripMain.Items.Add(button);
                    ToolStripDropDownButton button2 = new ToolStripDropDownButton("") {
                        ToolTipText = "Remove sessions from the session list.",
                        ImageKey = "remove"
                    };
                    ((ToolStripDropDownMenu) button2.DropDown).ShowCheckMargin = false;
                    ((ToolStripDropDownMenu) button2.DropDown).ShowImageMargin = true;
                    ((ToolStripDropDownMenu) button2.DropDown).ImageList = FiddlerApplication.UI.imglToolbar;
                    ToolStripMenuItem item = new ToolStripMenuItem("Remove all") {
                        DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                        ImageKey = "redbang"
                    };
                    item.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.UI.actRemoveAllSessions();
                    };
                    button2.DropDownItems.Add(item);
                    item = new ToolStripMenuItem("Images") {
                        ImageKey = "image",
                        DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
                    };
                    item.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.UI.actSelectSessionsWithResponseHeaderValue("Content-Type", "image/");
                        FiddlerApplication.UI.actRemoveSelectedSessions();
                        FiddlerApplication.UI.sbpInfo.Text = "Removed all images.";
                    };
                    button2.DropDownItems.Add(item);
                    item = new ToolStripMenuItem("CONNECTs") {
                        ImageKey = "lock",
                        DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
                    };
                    item.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.UI.actSelectSessionsMatchingCriteria(oSess => oSess.HTTPMethodIs("CONNECT"));
                        FiddlerApplication.UI.actRemoveSelectedSessions();
                        FiddlerApplication.UI.sbpInfo.Text = "Removed CONNECT tunnels";
                    };
                    button2.DropDownItems.Add(item);
                    item = new ToolStripMenuItem("Non-200s") {
                        ImageKey = "info",
                        DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
                    };
                    item.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.UI.actSelectSessionsMatchingCriteria(oSess => (oSess.state >= SessionStates.Done) && (200 != oSess.responseCode));
                        FiddlerApplication.UI.actRemoveSelectedSessions();
                        FiddlerApplication.UI.sbpInfo.Text = "Removed all but HTTP/200 responses.";
                    };
                    button2.DropDownItems.Add(item);
                    item = new ToolStripMenuItem("Non-Browser") {
                        ImageKey = "builder",
                        DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
                    };
                    item.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.UI.actSelectSessionsMatchingCriteria(delegate (Session oSess) {
                            string str = oSess.oFlags["x-ProcessInfo"];
                            if (!string.IsNullOrEmpty(str))
                            {
                                bool flag = ((str.StartsWith("ie", StringComparison.OrdinalIgnoreCase) || str.StartsWith("firefox", StringComparison.OrdinalIgnoreCase)) || (str.StartsWith("chrome", StringComparison.OrdinalIgnoreCase) || str.StartsWith("opera", StringComparison.OrdinalIgnoreCase))) || str.StartsWith("safari", StringComparison.OrdinalIgnoreCase);
                                return !flag;
                            }
                            return true;
                        });
                        FiddlerApplication.UI.actRemoveSelectedSessions();
                        FiddlerApplication.UI.sbpInfo.Text = "Removed all but browser traffic.";
                    };
                    button2.DropDownItems.Add(item);
                    item = new ToolStripMenuItem("Un-Marked") {
                        ImageKey = "mark",
                        DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
                    };
                    item.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.UI.TrimSessionList(0);
                        FiddlerApplication.UI.sbpInfo.Text = "Removed all unmarked sessions.";
                    };
                    button2.DropDownItems.Add(item);
                    tstripMain.Items.Add(button2);
                    button = new ToolStripButton("Resume All") {
                        ToolTipText = "Resume all sessions that are currently stopped at breakpoints.",
                        ImageKey = "resume"
                    };
                    button.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.UI.actResumeAllSessions();
                    };
                    tstripMain.Items.Add(button);
                    ToolStripSeparator separator = new ToolStripSeparator();
                    tstripMain.Items.Add(separator);
                    tsbStreaming = new ToolStripButton("Streaming");
                    tsbStreaming.ToolTipText = "When Streaming Mode is enabled, all breakpoints are skipped and all HTTP responses are streamed.";
                    tsbStreaming.ImageKey = "streaming";
                    tsbStreaming.Checked = !FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.rules.bufferresponses", true);
                    tsbStreaming.CheckOnClick = true;
                    tsbStreaming.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.Prefs.SetBoolPref("fiddler.ui.rules.bufferresponses", !(sender as ToolStripButton).Checked);
                    };
                    tstripMain.Items.Add(tsbStreaming);
                    tsbRemoveEncodings = new ToolStripButton("AutoDecode");
                    tsbRemoveEncodings.ToolTipText = "When enabled all traffic is decompressed for easy viewing.";
                    tsbRemoveEncodings.ImageKey = "decoder";
                    tsbRemoveEncodings.CheckOnClick = true;
                    tsbRemoveEncodings.Checked = FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.rules.removeencoding", false);
                    tsbRemoveEncodings.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.Prefs.SetBoolPref("fiddler.ui.rules.removeencoding", tsbRemoveEncodings.Checked);
                    };
                    tstripMain.Items.Add(tsbRemoveEncodings);
                    separator = new ToolStripSeparator();
                    tstripMain.Items.Add(separator);
                    if (CONFIG.bMapSocketToProcess && !CONFIG.bIsViewOnly)
                    {
                        tslProcessFilter = new ToolStripLabel("Process Filter");
                        tslProcessFilter.Font = new Font(tslProcessFilter.Font.FontFamily, tstripMain.Font.SizeInPoints, FontStyle.Italic);
                        tslProcessFilter.BackColor = Color.Transparent;
                        tslProcessFilter.MouseDown += delegate (object sender, MouseEventArgs e) {
                            if (e.Button == MouseButtons.Right)
                            {
                                _bCapturing = false;
                                tstripMain.Cursor = Cursors.Default;
                                ClearProcessFilter();
                            }
                            else
                            {
                                tstripMain.Capture = true;
                                _bCapturing = true;
                                FiddlerApplication._iShowOnlyPID = _iFilterPID = 0;
                                tstripMain.Cursor = Cursors.Cross;
                                _hInvertedWindow = IntPtr.Zero;
                                tslProcessFilter.Font = new Font(tslProcessFilter.Font, FontStyle.Italic);
                                tslProcessFilter.Text = "pick target...";
                            }
                        };
                        tstripMain.MouseMove += delegate (object sender, MouseEventArgs e) {
                            if (_bCapturing)
                            {
                                ShowHoveredApplication();
                            }
                        };
                        tslProcessFilter.MouseUp += delegate (object sender, MouseEventArgs e) {
                            EndMouseCapture();
                        };
                        tstripMain.MouseUp += delegate (object sender, MouseEventArgs e) {
                            EndMouseCapture();
                        };
                        tslProcessFilter.ImageKey = "crosshair";
                        tslProcessFilter.ToolTipText = "Drag this icon to a window to show traffic from only that process.\nRight-click to cancel the filter.";
                        tstripMain.Items.Add(tslProcessFilter);
                    }
                    button = new ToolStripButton("Find") {
                        ToolTipText = "Find sessions containing specified content.",
                        ImageKey = "find"
                    };
                    button.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.UI.actDoFind();
                    };
                    tstripMain.Items.Add(button);
                    button = new ToolStripButton("Save") {
                        ToolTipText = "Save all sessions in a .SAZ file.",
                        ImageKey = "save"
                    };
                    button.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.UI.actSaveAllSessions();
                    };
                    tstripMain.Items.Add(button);
                    separator = new ToolStripSeparator();
                    tstripMain.Items.Add(separator);
                    button = new ToolStripButton {
                        ToolTipText = "Add a screenshot to the capture\nHold SHIFT to delay 5 seconds.",
                        ImageKey = "camera"
                    };
                    button.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.UI.actCaptureScreenshot((Utilities.GetAsyncKeyState(0x10) < 0) || (Utilities.GetAsyncKeyState(0x11) < 0));
                    };
                    tstripMain.Items.Add(button);
                    ToolStripButton button3 = new ToolStripButton("Launch IE") {
                        ToolTipText = "Launch IE to the selected URL, or about:blank."
                    };
                    button3.Click += new EventHandler(FiddlerToolbar.tsbLaunchIE_Click);
                    button3.ImageKey = "ie";
                    tstripMain.Items.Add(button3);
                    if (!CONFIG.bIsViewOnly)
                    {
                        button = new ToolStripButton("Clear Cache") {
                            ToolTipText = "Clear the WinINET cache. Hold CTRL to also delete persistent cookies.",
                            ImageKey = "clearcache"
                        };
                        button.Click += delegate (object sender, EventArgs e) {
                            FiddlerApplication.UI.actClearWinINETCache();
                            if ((Utilities.GetAsyncKeyState(0x10) < 0) || (Utilities.GetAsyncKeyState(0x11) < 0))
                            {
                                FiddlerApplication.UI.actClearWinINETCookies();
                            }
                        };
                        tstripMain.Items.Add(button);
                    }
                    button = new ToolStripButton("Encoder") {
                        ToolTipText = "Create a new instance of the text encoder/decoder.",
                        ImageKey = "tools"
                    };
                    button.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.UI.actShowEncodingTools();
                    };
                    tstripMain.Items.Add(button);
                    separator = new ToolStripSeparator();
                    tstripMain.Items.Add(separator);
                    button = new ToolStripButton("Tearoff") {
                        ToolTipText = "Open Details View in a floating window.",
                        ImageKey = "tearoff"
                    };
                    button.Click += delegate (object sender, EventArgs e) {
                        FiddlerApplication.UI.actTearoffInspectors();
                    };
                    tstripMain.Items.Add(button);
                    separator = new ToolStripSeparator();
                    tstripMain.Items.Add(separator);
                    ToolStripTextBox tstbMSDN = new ToolStripTextBox("MSDNSearch");
                    Utilities.SetCueText(tstbMSDN.Control, "MSDN Search...");
                    tstbMSDN.AcceptsReturn = true;
                    tstbMSDN.KeyUp += delegate (object sender, KeyEventArgs e) {
                        if (e.KeyCode == Keys.Return)
                        {
                            e.SuppressKeyPress = true;
                            e.Handled = true;
                            Utilities.LaunchHyperlink("http://social.msdn.microsoft.com/Search/en-US/?Refinement=59&Query=" + HttpUtility.UrlEncode(tstbMSDN.Text, Encoding.UTF8));
                        }
                    };
                    tstbMSDN.KeyDown += delegate (object sender, KeyEventArgs e) {
                        if (e.KeyCode == Keys.Return)
                        {
                            e.SuppressKeyPress = true;
                        }
                        else if ((e.KeyCode == Keys.A) && e.Control)
                        {
                            tstbMSDN.SelectAll();
                            e.SuppressKeyPress = true;
                            e.Handled = true;
                        }
                    };
                    tstripMain.Items.Add(tstbMSDN);
                    button = new ToolStripButton("Help") {
                        ToolTipText = "Show Fiddler's online help.",
                        ImageKey = "help"
                    };
                    button.Click += delegate (object sender, EventArgs e) {
                        Utilities.LaunchHyperlink(CONFIG.GetUrl("HelpContents") + Application.ProductVersion);
                    };
                    tstripMain.Items.Add(button);
                    button = new ToolStripButton("") {
                        ToolTipText = "Close this toolbar. Click View / Show Toolbar to get it back.",
                        ImageKey = "close",
                        Alignment = ToolStripItemAlignment.Right
                    };
                    button.Click += delegate (object sender, EventArgs e) {
                        Hide();
                    };
                    tstripMain.Items.Add(button);
                    tslNetStat = new ToolStripLabel("Detecting");
                    tslNetStat.Alignment = ToolStripItemAlignment.Right;
                    tslNetStat.BackColor = Color.Transparent;
                    tslNetStat.ImageKey = "notconnected";
                    tslNetStat.ToolTipText = "Detecting network status...";
                    tslNetStat.MouseDown += delegate (object sender, MouseEventArgs e) {
                        if (e.Clicks > 1)
                        {
                            Utilities.RunExecutable("control.exe", "netconnections");
                        }
                    };
                    tstripMain.Items.Add(tslNetStat);
                    UpdateNetworkStatus(NetworkInterface.GetIsNetworkAvailable());
                    tstripMain.ResumeLayout();
                    FiddlerApplication.UI.Controls.Add(tstripMain);
                }
                catch (Exception exception)
                {
                    FiddlerApplication.ReportException(exception, "Toolbar failed");
                }
                finally
                {
                    Monitor.Exit(obj2);
                }
            }
        }

        internal static void ClearProcessFilter()
        {
            FiddlerApplication._iShowOnlyPID = _iFilterPID = 0;
            if (IsShowing())
            {
                tslProcessFilter.Text = "Process Filter";
                tslProcessFilter.Font = new Font(tslProcessFilter.Font, FontStyle.Italic);
            }
        }

        internal static void DisplayIfNeeded()
        {
            if (FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.toolbar.visible", true))
            {
                _DoShow();
            }
        }

        private static void EndMouseCapture()
        {
            if (_bCapturing)
            {
                _bCapturing = false;
                RevertWindowMarking();
                tstripMain.Cursor = Cursors.Default;
                tslProcessFilter.Font = new Font(tslProcessFilter.Font, FontStyle.Regular);
                FiddlerApplication._iShowOnlyPID = _iFilterPID;
                if (_iFilterPID == 0)
                {
                    tslProcessFilter.Text = "Process Filter";
                    tslProcessFilter.Font = new Font(tslProcessFilter.Font, FontStyle.Italic);
                }
                else
                {
                    string text = tslProcessFilter.Text;
                    bool flag = ((text.StartsWith("ie", StringComparison.OrdinalIgnoreCase) || text.StartsWith("firefox", StringComparison.OrdinalIgnoreCase)) || (text.StartsWith("chrome", StringComparison.OrdinalIgnoreCase) || text.StartsWith("opera", StringComparison.OrdinalIgnoreCase))) || text.StartsWith("safari", StringComparison.OrdinalIgnoreCase);
                    if (((CONFIG.iShowProcessFilter == ProcessFilterCategories.HideAll) || (flag && (CONFIG.iShowProcessFilter == ProcessFilterCategories.NonBrowsers))) || (!flag && (CONFIG.iShowProcessFilter == ProcessFilterCategories.Browsers)))
                    {
                        FiddlerApplication.UI._SetProcessFilter(ProcessFilterCategories.All);
                    }
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hwnd);
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
        [DllImport("user32.dll", SetLastError=true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        private static void HandlePrefChanged(object sender, PrefChangeEventArgs oPref)
        {
            if (oPref.PrefName == "fiddler.ui.toolbar.visible")
            {
                if (oPref.ValueBool)
                {
                    FiddlerApplication.UIInvoke(new MethodInvoker(FiddlerToolbar._DoShow));
                }
                else
                {
                    FiddlerApplication.UIInvoke(new MethodInvoker(FiddlerToolbar._DoHide));
                }
            }
            else if (oPref.PrefName == "fiddler.ui.rules.removeencoding")
            {
                if (tsbRemoveEncodings != null)
                {
                    bool flag2 = oPref.ValueString == bool.TrueString;
                    tsbRemoveEncodings.Checked = flag2;
                }
            }
            else if ((oPref.PrefName == "fiddler.ui.rules.bufferresponses") && (tsbStreaming != null))
            {
                bool flag3 = !oPref.ValueBool;
                tsbStreaming.Checked = flag3;
            }
        }

        internal static void Hide()
        {
            FiddlerApplication.Prefs.SetBoolPref("fiddler.ui.toolbar.visible", false);
        }

        [DllImport("user32.dll")]
        private static extern bool InvertRect(IntPtr hDC, [In] ref RECT lprc);
        internal static bool IsShowing()
        {
            return (null != tstripMain);
        }

        private static string ProcessIDToProcessName(int iPid)
        {
            try
            {
                return Process.GetProcessById(iPid).ProcessName.ToLower();
            }
            catch
            {
                return string.Empty;
            }
        }

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        private static void RevertWindowMarking()
        {
            if (IntPtr.Zero != _hInvertedWindow)
            {
                IntPtr windowDC = GetWindowDC(_hInvertedWindow);
                InvertRect(windowDC, ref _rectInvertedWindow);
                ReleaseDC(_hInvertedWindow, windowDC);
                _hInvertedWindow = IntPtr.Zero;
            }
        }

        internal static void SetViewerTitle(string sTitle)
        {
            if ((tstripMain != null) && (tsbWindowName != null))
            {
                tsbWindowName.Text = sTitle;
            }
        }

        internal static void Show()
        {
            FiddlerApplication.Prefs.SetBoolPref("fiddler.ui.toolbar.visible", true);
        }

        private static void ShowHoveredApplication()
        {
            try
            {
                IntPtr hWnd = WindowFromPoint(Cursor.Position);
                int lpdwProcessId = 0;
                GetWindowThreadProcessId(hWnd, out lpdwProcessId);
                if (hWnd != _hInvertedWindow)
                {
                    RevertWindowMarking();
                    if (Process.GetCurrentProcess().Id == lpdwProcessId)
                    {
                        tslProcessFilter.Text = string.Format("pick target...", ProcessIDToProcessName(lpdwProcessId), lpdwProcessId);
                        _iFilterPID = 0;
                    }
                    else
                    {
                        tslProcessFilter.Text = string.Format("{0}:{1}", ProcessIDToProcessName(lpdwProcessId), lpdwProcessId);
                        _iFilterPID = lpdwProcessId;
                        GetWindowRect(hWnd, ref _rectInvertedWindow);
                        _rectInvertedWindow.Right -= _rectInvertedWindow.Left;
                        _rectInvertedWindow.Bottom -= _rectInvertedWindow.Top;
                        _rectInvertedWindow.Left = _rectInvertedWindow.Top = 0;
                        IntPtr windowDC = GetWindowDC(hWnd);
                        InvertRect(windowDC, ref _rectInvertedWindow);
                        ReleaseDC(hWnd, windowDC);
                        _hInvertedWindow = hWnd;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private static void tsbLaunchIE_Click(object sender, EventArgs e)
        {
            if (FiddlerApplication.UI.lvSessions.SelectedItems.Count == 1)
            {
                Utilities.RunExecutable("iexplore.exe", FiddlerApplication.UI.GetFirstSelectedSession().fullUrl);
            }
            else
            {
                Utilities.RunExecutable("iexplore.exe", "about:blank");
            }
        }

        internal static void UpdateNetworkStatus(bool bAvailable)
        {
            if (tslNetStat != null)
            {
                if (bAvailable)
                {
                    tslNetStat.ToolTipText = "Network connection active";
                    tslNetStat.Text = "Online";
                    tslNetStat.ImageKey = "connected";
                }
                else
                {
                    tslNetStat.ToolTipText = "Network connection not active";
                    tslNetStat.Text = "Offline";
                    tslNetStat.ImageKey = "notconnected";
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        private class TSRenderWithoutBorder : ToolStripSystemRenderer
        {
            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
            }
        }
    }
}

