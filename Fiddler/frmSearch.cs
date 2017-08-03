namespace Fiddler
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    public class frmSearch : Form
    {
        private static bool _bSearchBinaries;
        private static bool _bSearchCaseSensitive;
        private static bool _bSearchDecodeFirst;
        private static bool _bSelectMatches;
        private static int _iSearchCount;
        private static int _ixExamine;
        private static int _ixSearchIn;
        private static AutoCompleteStringCollection _SearchHistory = new AutoCompleteStringCollection();
        private static string _sLastSearch;
        private Button btnClose;
        private Button btnDoSearch;
        internal CheckBox cbSearchBinaries;
        internal CheckBox cbSearchCaseSensitive;
        internal CheckBox cbSearchDecodeFirst;
        internal CheckBox cbSearchSelectedSessions;
        internal CheckBox cbSearchSelectMatches;
        internal CheckBox cbUnmarkOld;
        private CheckBox cbUseRegEx;
        internal ComboBox cbxExamine;
        internal ComboBox cbxSearchColor;
        internal ComboBox cbxSearchIn;
        private Container components;
        private GroupBox gbSearchOptions;
        private Label lblExamine;
        private Label lblFind;
        private Label lblResultColor;
        private Label lblSearch;
        internal TextBox txtSearchFor;

        internal frmSearch()
        {
            this.InitializeComponent();
            this.cbxSearchIn.SelectedIndex = 0;
            this.cbxExamine.SelectedIndex = 0;
            this.cbUnmarkOld.Checked = CONFIG.bSearchUnmarkOldHits;
            this.cbSearchSelectMatches.Checked = _bSelectMatches;
            this.cbSearchBinaries.Checked = _bSearchBinaries;
            this.cbSearchCaseSensitive.Checked = _bSearchCaseSensitive;
            this.cbSearchDecodeFirst.Checked = _bSearchDecodeFirst;
            this.cbxExamine.SelectedIndex = _ixExamine;
            this.cbxSearchIn.SelectedIndex = _ixSearchIn;
            this.txtSearchFor.AutoCompleteMode = AutoCompleteMode.Suggest;
            this.txtSearchFor.AutoCompleteCustomSource = _SearchHistory;
            this.txtSearchFor.Text = _sLastSearch;
            this.txtSearchFor.SelectAll();
            this.cbxSearchColor.SelectedIndex = 1 + (_iSearchCount % (this.cbxSearchColor.Items.Count - 1));
        }

        private void btnDoSearch_Click(object sender, EventArgs e)
        {
            if (!this.cbUnmarkOld.Checked && (this.cbxSearchColor.SelectedIndex > 0))
            {
                _iSearchCount++;
            }
            _sLastSearch = this.txtSearchFor.Text;
            CONFIG.bSearchUnmarkOldHits = this.cbUnmarkOld.Checked;
            _bSelectMatches = this.cbSearchSelectMatches.Checked;
            _bSearchBinaries = this.cbSearchBinaries.Checked;
            _bSearchDecodeFirst = this.cbSearchDecodeFirst.Checked;
            _bSearchCaseSensitive = this.cbSearchCaseSensitive.Checked;
            _ixExamine = this.cbxExamine.SelectedIndex;
            _ixSearchIn = this.cbxSearchIn.SelectedIndex;
            _SearchHistory.Add(this.txtSearchFor.Text);
            FiddlerApplication.Prefs.SetStringPref("fiddler.find.ephemeral.lastsearch", this.txtSearchFor.Text);
        }

        private void cbUseRegEx_CheckedChanged(object sender, EventArgs e)
        {
            if (this.cbUseRegEx.Checked)
            {
                if (!this.txtSearchFor.Text.StartsWith("REGEX:", StringComparison.OrdinalIgnoreCase))
                {
                    this.txtSearchFor.Text = "REGEX:" + this.txtSearchFor.Text;
                }
            }
            else if (this.txtSearchFor.Text.StartsWith("REGEX:", StringComparison.OrdinalIgnoreCase))
            {
                this.txtSearchFor.Text = this.txtSearchFor.Text.Substring(6);
            }
        }

        private void cbxExamine_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.cbSearchDecodeFirst.Enabled = (this.cbxSearchIn.SelectedIndex != 3) && (this.cbxExamine.SelectedIndex != 1);
            if (this.cbxExamine.SelectedIndex == 1)
            {
                this.cbSearchBinaries.Enabled = this.cbSearchBinaries.Checked = false;
            }
            else
            {
                this.cbSearchBinaries.Enabled = true;
            }
        }

        private void cbxSearchColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.cbxSearchColor.SelectedIndex > 0)
            {
                this.lblResultColor.BackColor = this.cbxSearchColor.BackColor = Color.FromName(this.cbxSearchColor.Text);
            }
            else
            {
                this.lblResultColor.BackColor = this.cbxSearchColor.BackColor = Color.FromKnownColor(KnownColor.Window);
                this.cbSearchSelectMatches.Checked = true;
            }
        }

        private void cbxSearchIn_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.cbxExamine.Enabled = this.cbSearchBinaries.Enabled = this.cbxSearchIn.SelectedIndex != 3;
            this.cbSearchDecodeFirst.Enabled = (this.cbxSearchIn.SelectedIndex != 3) && (this.cbxExamine.SelectedIndex != 1);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            ComponentResourceManager manager = new ComponentResourceManager(typeof(frmSearch));
            this.btnDoSearch = new Button();
            this.cbSearchBinaries = new CheckBox();
            this.txtSearchFor = new TextBox();
            this.cbSearchCaseSensitive = new CheckBox();
            this.gbSearchOptions = new GroupBox();
            this.cbUseRegEx = new CheckBox();
            this.cbUnmarkOld = new CheckBox();
            this.cbxSearchColor = new ComboBox();
            this.cbSearchDecodeFirst = new CheckBox();
            this.lblExamine = new Label();
            this.cbxExamine = new ComboBox();
            this.lblSearch = new Label();
            this.cbxSearchIn = new ComboBox();
            this.cbSearchSelectMatches = new CheckBox();
            this.cbSearchSelectedSessions = new CheckBox();
            this.lblResultColor = new Label();
            this.lblFind = new Label();
            this.btnClose = new Button();
            this.gbSearchOptions.SuspendLayout();
            base.SuspendLayout();
            this.btnDoSearch.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.btnDoSearch.DialogResult = DialogResult.OK;
            this.btnDoSearch.Enabled = false;
            this.btnDoSearch.Location = new Point(110, 0xfb);
            this.btnDoSearch.Name = "btnDoSearch";
            this.btnDoSearch.Size = new Size(0x72, 0x1a);
            this.btnDoSearch.TabIndex = 3;
            this.btnDoSearch.Text = "&Find Sessions";
            this.btnDoSearch.Click += new EventHandler(this.btnDoSearch_Click);
            this.cbSearchBinaries.Location = new Point(10, 0x5b);
            this.cbSearchBinaries.Name = "cbSearchBinaries";
            this.cbSearchBinaries.Size = new Size(0x106, 20);
            this.cbSearchBinaries.TabIndex = 6;
            this.cbSearchBinaries.Text = "Search &binaries";
            this.txtSearchFor.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.txtSearchFor.AutoCompleteSource = AutoCompleteSource.CustomSource;
            this.txtSearchFor.Location = new Point(0x2a, 8);
            this.txtSearchFor.Name = "txtSearchFor";
            this.txtSearchFor.Size = new Size(0xf6, 0x15);
            this.txtSearchFor.TabIndex = 1;
            this.txtSearchFor.TextChanged += new EventHandler(this.txtSearchFor_TextChanged);
            this.cbSearchCaseSensitive.Location = new Point(10, 0x45);
            this.cbSearchCaseSensitive.Name = "cbSearchCaseSensitive";
            this.cbSearchCaseSensitive.Size = new Size(0x62, 20);
            this.cbSearchCaseSensitive.TabIndex = 4;
            this.cbSearchCaseSensitive.Text = "Match &case";
            this.gbSearchOptions.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.gbSearchOptions.Controls.Add(this.cbUseRegEx);
            this.gbSearchOptions.Controls.Add(this.cbUnmarkOld);
            this.gbSearchOptions.Controls.Add(this.cbxSearchColor);
            this.gbSearchOptions.Controls.Add(this.cbSearchDecodeFirst);
            this.gbSearchOptions.Controls.Add(this.lblExamine);
            this.gbSearchOptions.Controls.Add(this.cbxExamine);
            this.gbSearchOptions.Controls.Add(this.lblSearch);
            this.gbSearchOptions.Controls.Add(this.cbxSearchIn);
            this.gbSearchOptions.Controls.Add(this.cbSearchSelectMatches);
            this.gbSearchOptions.Controls.Add(this.cbSearchSelectedSessions);
            this.gbSearchOptions.Controls.Add(this.lblResultColor);
            this.gbSearchOptions.Controls.Add(this.cbSearchCaseSensitive);
            this.gbSearchOptions.Controls.Add(this.cbSearchBinaries);
            this.gbSearchOptions.Location = new Point(8, 0x22);
            this.gbSearchOptions.Name = "gbSearchOptions";
            this.gbSearchOptions.Size = new Size(0x11a, 0xd4);
            this.gbSearchOptions.TabIndex = 2;
            this.gbSearchOptions.TabStop = false;
            this.gbSearchOptions.Text = " Options ";
            this.cbUseRegEx.Location = new Point(0x6d, 0x45);
            this.cbUseRegEx.Name = "cbUseRegEx";
            this.cbUseRegEx.Size = new Size(0xa3, 20);
            this.cbUseRegEx.TabIndex = 5;
            this.cbUseRegEx.Text = "Regular E&xpression";
            this.cbUseRegEx.CheckedChanged += new EventHandler(this.cbUseRegEx_CheckedChanged);
            this.cbUnmarkOld.Location = new Point(0x98, 0x9d);
            this.cbUnmarkOld.Name = "cbUnmarkOld";
            this.cbUnmarkOld.Size = new Size(120, 20);
            this.cbUnmarkOld.TabIndex = 10;
            this.cbUnmarkOld.Text = "&Unmark old results";
            this.cbxSearchColor.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.cbxSearchColor.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbxSearchColor.Items.AddRange(new object[] { "(None)", "Yellow", "PaleGreen", "Plum", "Pink", "PaleGoldenrod", "RosyBrown", "LightSteelBlue", "LightCyan", "Tomato" });
            this.cbxSearchColor.Location = new Point(0x6a, 180);
            this.cbxSearchColor.MaxDropDownItems = 9;
            this.cbxSearchColor.Name = "cbxSearchColor";
            this.cbxSearchColor.Size = new Size(0xa6, 0x15);
            this.cbxSearchColor.TabIndex = 12;
            this.cbxSearchColor.SelectedIndexChanged += new EventHandler(this.cbxSearchColor_SelectedIndexChanged);
            this.cbSearchDecodeFirst.Location = new Point(10, 0x71);
            this.cbSearchDecodeFirst.Name = "cbSearchDecodeFirst";
            this.cbSearchDecodeFirst.Size = new Size(0x106, 20);
            this.cbSearchDecodeFirst.TabIndex = 7;
            this.cbSearchDecodeFirst.Text = "&Decode compressed content";
            this.lblExamine.Location = new Point(12, 0x2e);
            this.lblExamine.Name = "lblExamine";
            this.lblExamine.Size = new Size(0x60, 14);
            this.lblExamine.TabIndex = 2;
            this.lblExamine.Text = "&Examine:";
            this.cbxExamine.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.cbxExamine.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbxExamine.Items.AddRange(new object[] { "Headers and body", "Headers only", "Bodies only" });
            this.cbxExamine.Location = new Point(0x6c, 0x2a);
            this.cbxExamine.Name = "cbxExamine";
            this.cbxExamine.Size = new Size(0xa6, 0x15);
            this.cbxExamine.TabIndex = 3;
            this.cbxExamine.SelectedIndexChanged += new EventHandler(this.cbxExamine_SelectedIndexChanged);
            this.lblSearch.Location = new Point(12, 0x16);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new Size(0x60, 14);
            this.lblSearch.TabIndex = 0;
            this.lblSearch.Text = "&Search:";
            this.cbxSearchIn.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.cbxSearchIn.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbxSearchIn.Items.AddRange(new object[] { "Requests and responses", "Requests only", "Responses only", "URLs only" });
            this.cbxSearchIn.Location = new Point(0x6c, 0x12);
            this.cbxSearchIn.Name = "cbxSearchIn";
            this.cbxSearchIn.Size = new Size(0xa6, 0x15);
            this.cbxSearchIn.TabIndex = 1;
            this.cbxSearchIn.SelectedIndexChanged += new EventHandler(this.cbxSearchIn_SelectedIndexChanged);
            this.cbSearchSelectMatches.Location = new Point(10, 0x9d);
            this.cbSearchSelectMatches.Name = "cbSearchSelectMatches";
            this.cbSearchSelectMatches.Size = new Size(0x7e, 20);
            this.cbSearchSelectMatches.TabIndex = 9;
            this.cbSearchSelectMatches.Text = "Select &matches";
            this.cbSearchSelectedSessions.Location = new Point(10, 0x87);
            this.cbSearchSelectedSessions.Name = "cbSearchSelectedSessions";
            this.cbSearchSelectedSessions.Size = new Size(0x106, 20);
            this.cbSearchSelectedSessions.TabIndex = 8;
            this.cbSearchSelectedSessions.Text = "Search &only selected sessions";
            this.lblResultColor.Location = new Point(10, 180);
            this.lblResultColor.Name = "lblResultColor";
            this.lblResultColor.Size = new Size(0x62, 0x15);
            this.lblResultColor.TabIndex = 11;
            this.lblResultColor.Text = "&Result Highlight:";
            this.lblResultColor.TextAlign = ContentAlignment.MiddleLeft;
            this.lblFind.Location = new Point(8, 12);
            this.lblFind.Name = "lblFind";
            this.lblFind.Size = new Size(0x20, 14);
            this.lblFind.TabIndex = 0;
            this.lblFind.Text = "Fi&nd:";
            this.btnClose.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.btnClose.Location = new Point(230, 0xfb);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new Size(60, 0x1a);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "Cancel";
            base.AcceptButton = this.btnDoSearch;
            base.AutoScaleMode = AutoScaleMode.Inherit;
            base.CancelButton = this.btnClose;
            base.ClientSize = new Size(0x128, 0x119);
            base.Controls.Add(this.btnClose);
            base.Controls.Add(this.lblFind);
            base.Controls.Add(this.gbSearchOptions);
            base.Controls.Add(this.txtSearchFor);
            base.Controls.Add(this.btnDoSearch);
            this.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            base.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            base.Icon = (Icon)Properties.Resources.sbpCapture_Icon; //manager.GetObject("$this.Icon");
            base.KeyPreview = true;
            this.MaximumSize = new Size(0x400, 320);
            this.MinimumSize = new Size(0x130, 0x11a);
            base.Name = "frmSearch";
            base.SizeGripStyle = SizeGripStyle.Hide;
            base.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Find Sessions";
            this.gbSearchOptions.ResumeLayout(false);
            base.ResumeLayout(false);
            base.PerformLayout();
        }

        private void txtSearchFor_TextChanged(object sender, EventArgs e)
        {
            this.cbUseRegEx.Checked = this.txtSearchFor.Text.StartsWith("REGEX:", StringComparison.OrdinalIgnoreCase);
            this.btnDoSearch.Enabled = this.txtSearchFor.Text.Length > (this.cbUseRegEx.Checked ? 6 : 0);
        }
    }
}

