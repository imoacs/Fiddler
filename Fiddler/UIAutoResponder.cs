namespace Fiddler
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;

    internal class UIAutoResponder : UserControl
    {
        private Button btnRespondAdd;
        private Button btnRespondImport;
        private Button btnSaveRule;
        internal CheckBox cbAutoRespond;
        internal CheckBox cbRespondPassthrough;
        internal CheckBox cbRespondUseLatency;
        internal ComboBox cbxRuleAction;
        internal ComboBox cbxRuleURI;
        private ColumnHeader colRespondLatency;
        private ColumnHeader colRespondMatch;
        private ColumnHeader colRespondWith;
        private IContainer components;
        private GroupBox gbResponderEditor;
        private Label lblAutoRespondHeader;
        private Label lblMultipleMatch;
        private Label lblResponderTips;
        private LinkLabel lnkAutoRespondHelp;
        internal ListView lvRespondRules;
        private ToolStripMenuItem miAREdit;
        private ToolStripMenuItem miAutoResponderGenerate;
        private ToolStripMenuItem miDemoteRule;
        private ToolStripMenuItem miEditARFileWith;
        private ToolStripMenuItem miExportRules;
        private ToolStripMenuItem miPromoteRule;
        private ToolStripMenuItem miRemoveRule;
        private ToolStripMenuItem miRespondCloneRule;
        private ToolStripMenuItem miRespondSetLatency;
        private ToolStripMenuItem miRuleOpenURL;
        private ContextMenuStrip mnuContextAutoResponder;
        private Panel pnlAutoResponders;
        private Panel pnlResponderActions;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripSeparator toolStripMenuItem3;

        internal UIAutoResponder()
        {
            this.InitializeComponent();
            this.lvRespondRules.Font = new Font(this.lvRespondRules.Font.FontFamily, CONFIG.flFontSize);
            Utilities.SetCueText(this.cbxRuleURI, "Request URL Pattern");
            Utilities.SetCueText(this.cbxRuleAction, "Local file to return or *Action to execute");
        }

        private void _SelectMostRecentLVRespondRule()
        {
            if (this.lvRespondRules.Items.Count > -1)
            {
                this.lvRespondRules.SelectedItems.Clear();
                this.lvRespondRules.Items[this.lvRespondRules.Items.Count - 1].Selected = true;
            }
        }

        private void actDemoteRule()
        {
            if (this.lvRespondRules.SelectedItems.Count == 1)
            {
                ListViewItem item = this.lvRespondRules.SelectedItems[0];
                if (FiddlerApplication._AutoResponder.DemoteRule((ResponderRule) item.Tag))
                {
                    int index = item.Index + 1;
                    item.Remove();
                    this.lvRespondRules.Items.Insert(index, item);
                    item.Focused = true;
                }
            }
        }

        private void actLaunchEditor(ListViewItem lvi)
        {
            ResponderRule tag = lvi.Tag as ResponderRule;
            if (tag.HasImportedResponse)
            {
                if ((tag._oEditor == null) || tag._oEditor.IsDisposed)
                {
                    tag._oEditor = new UIARRuleEditor(tag);
                    tag._oEditor.Show();
                }
                else
                {
                    tag._oEditor.BringToFront();
                }
            }
        }

        private void actPromoteRule()
        {
            if (this.lvRespondRules.SelectedItems.Count == 1)
            {
                ListViewItem item = this.lvRespondRules.SelectedItems[0];
                if (FiddlerApplication._AutoResponder.PromoteRule((ResponderRule) item.Tag))
                {
                    int index = item.Index - 1;
                    item.Remove();
                    this.lvRespondRules.Items.Insert(index, item);
                    item.Focused = true;
                }
            }
        }

        private void actRemoveSelectedRules()
        {
            ListView.SelectedListViewItemCollection selectedItems = this.lvRespondRules.SelectedItems;
            if (selectedItems.Count >= 1)
            {
                this.lvRespondRules.BeginUpdate();
                foreach (ListViewItem item in selectedItems)
                {
                    FiddlerApplication._AutoResponder.RemoveRule((ResponderRule) item.Tag);
                }
                this.lvRespondRules.EndUpdate();
                this.cbxRuleURI.Text = this.cbxRuleAction.Text = string.Empty;
                if (this.lvRespondRules.FocusedItem != null)
                {
                    this.lvRespondRules.FocusedItem.Selected = true;
                }
            }
        }

        private void btnRespondAdd_Click(object sender, EventArgs e)
        {
            string str;
            if (FiddlerApplication.UI.lvSessions.SelectedItems.Count == 1)
            {
                str = "EXACT:" + FiddlerApplication.UI.GetFirstSelectedSession().fullUrl;
            }
            else
            {
                str = "StringToMatch[" + ((this.lvRespondRules.Items.Count + 1)).ToString() + "]";
            }
            this.lvRespondRules.SelectedItems.Clear();
            ResponderRule rule = FiddlerApplication._AutoResponder.AddRule(str, string.Empty, true);
            if (rule != null)
            {
                rule.ViewItem.EnsureVisible();
                rule.ViewItem.Selected = true;
            }
            this.cbxRuleURI.Focus();
        }

        private void btnRespondImport_Click(object sender, EventArgs e)
        {
            string fileName;
            OpenFileDialog dialog = new OpenFileDialog {
                DefaultExt = "saz",
                RestoreDirectory = true,
                InitialDirectory = CONFIG.GetPath("Captures"),
                Title = "Import file for replay",
                Filter = "SAZ / Rules (*.saz;*.farx)|*.saz;*.farx|ZIP Files (*.zip)|*.zip"
            };
            if (DialogResult.OK == dialog.ShowDialog(this))
            {
                fileName = dialog.FileName;
            }
            else
            {
                dialog.Dispose();
                return;
            }
            dialog.Dispose();
            if (fileName.EndsWith(".farx", StringComparison.OrdinalIgnoreCase))
            {
                FiddlerApplication._AutoResponder.ImportFARX(fileName);
            }
            else
            {
                FiddlerApplication._AutoResponder.ImportSAZ(fileName);
            }
        }

        private void btnSaveRule_Click(object sender, EventArgs e)
        {
            if (this.lvRespondRules.SelectedItems.Count >= 1)
            {
                foreach (ListViewItem item in this.lvRespondRules.SelectedItems)
                {
                    ResponderRule tag = (ResponderRule) item.Tag;
                    if (this.cbxRuleURI.Visible)
                    {
                        tag.sMatch = this.cbxRuleURI.Text;
                        item.Text = tag.sMatch;
                    }
                    if (tag.sAction != this.cbxRuleAction.Text)
                    {
                        tag._arrResponseBodyBytes = null;
                        tag._oResponseHeaders = null;
                        tag.sAction = this.cbxRuleAction.Text;
                        item.SubItems[1].Text = tag.sAction;
                    }
                    FiddlerApplication._AutoResponder.IsRuleListDirty = true;
                }
            }
        }

        private void cbAutoRespond_CheckedChanged(object sender, EventArgs e)
        {
            FiddlerApplication._AutoResponder.IsEnabled = this.cbAutoRespond.Checked;
            if (this.cbAutoRespond.Checked && CONFIG.IsMicrosoftMachine)
            {
                FiddlerApplication.logSelfHostOnePerSession(30);
            }
            this.pnlAutoResponders.Enabled = this.cbAutoRespond.Checked;
            FiddlerApplication.UI.pageResponder.ImageIndex = this.cbAutoRespond.Checked ? 0x18 : 0x17;
        }

        private void cbRespondPassthrough_CheckedChanged(object sender, EventArgs e)
        {
            FiddlerApplication._AutoResponder.PermitFallthrough = this.cbRespondPassthrough.Checked;
        }

        private void cbRespondUseLatency_CheckedChanged(object sender, EventArgs e)
        {
            FiddlerApplication._AutoResponder.UseLatency = this.cbRespondUseLatency.Checked;
            this.lvRespondRules.Columns[2].Width = FiddlerApplication._AutoResponder.UseLatency ? 60 : 0;
        }

        private void cbxRuleAction_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.A))
            {
                this.cbxRuleAction.SelectAll();
                e.SuppressKeyPress = true;
            }
            else if ((e.KeyCode == Keys.Return) && !this.cbxRuleAction.DroppedDown)
            {
                base.ActiveControl = base.GetNextControl(base.ActiveControl, true);
                e.SuppressKeyPress = true;
            }
        }

        private void cbxRuleAction_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (this.cbxRuleAction.SelectedIndex == (this.cbxRuleAction.Items.Count - 1))
            {
                bool flag = false;
                this.cbxRuleAction.Text = string.Empty;
                OpenFileDialog dialog = new OpenFileDialog {
                    DefaultExt = "dat",
                    Title = "Choose response file",
                    Filter = "All Files (*.*)|*.*|Response Files (*.dat)|*.dat"
                };
                if (DialogResult.OK == dialog.ShowDialog(this))
                {
                    this.cbxRuleAction.Items.Insert(0, dialog.FileName);
                    flag = true;
                }
                if (this.cbxRuleAction.Items.Count > 0)
                {
                    this.cbxRuleAction.SelectedIndex = 0;
                }
                dialog.Dispose();
                if (flag)
                {
                    this.btnSaveRule_Click(null, null);
                }
            }
        }

        private void cbxRuleURI_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.A))
            {
                this.cbxRuleURI.SelectAll();
                e.SuppressKeyPress = true;
            }
            if (e.KeyCode == Keys.Return)
            {
                base.ActiveControl = base.GetNextControl(base.ActiveControl, true);
                e.SuppressKeyPress = true;
            }
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
            this.components = new Container();
            this.lblResponderTips = new Label();
            this.lnkAutoRespondHelp = new LinkLabel();
            this.cbAutoRespond = new CheckBox();
            this.pnlAutoResponders = new Panel();
            this.lvRespondRules = new ListView();
            this.colRespondMatch = new ColumnHeader();
            this.colRespondWith = new ColumnHeader();
            this.colRespondLatency = new ColumnHeader();
            this.mnuContextAutoResponder = new ContextMenuStrip(this.components);
            this.miRemoveRule = new ToolStripMenuItem();
            this.miPromoteRule = new ToolStripMenuItem();
            this.miDemoteRule = new ToolStripMenuItem();
            this.miRespondSetLatency = new ToolStripMenuItem();
            this.miRespondCloneRule = new ToolStripMenuItem();
            this.miAutoResponderGenerate = new ToolStripMenuItem();
            this.toolStripMenuItem3 = new ToolStripSeparator();
            this.miRuleOpenURL = new ToolStripMenuItem();
            this.toolStripMenuItem1 = new ToolStripSeparator();
            this.miExportRules = new ToolStripMenuItem();
            this.gbResponderEditor = new GroupBox();
            this.cbxRuleURI = new ComboBox();
            this.lblMultipleMatch = new Label();
            this.cbxRuleAction = new ComboBox();
            this.btnSaveRule = new Button();
            this.pnlResponderActions = new Panel();
            this.btnRespondAdd = new Button();
            this.btnRespondImport = new Button();
            this.cbRespondUseLatency = new CheckBox();
            this.cbRespondPassthrough = new CheckBox();
            this.lblAutoRespondHeader = new Label();
            this.miEditARFileWith = new ToolStripMenuItem();
            this.toolStripMenuItem2 = new ToolStripSeparator();
            this.miAREdit = new ToolStripMenuItem();
            this.pnlAutoResponders.SuspendLayout();
            this.mnuContextAutoResponder.SuspendLayout();
            this.gbResponderEditor.SuspendLayout();
            this.pnlResponderActions.SuspendLayout();
            base.SuspendLayout();
            this.lblResponderTips.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            this.lblResponderTips.ForeColor = SystemColors.ControlDarkDark;
            this.lblResponderTips.Location = new Point(0x16b, 0x27);
            this.lblResponderTips.Name = "lblResponderTips";
            this.lblResponderTips.Size = new Size(0xd8, 0x10);
            this.lblResponderTips.TabIndex = 8;
            this.lblResponderTips.Text = "Use the + and - keys to reorder rules.";
            this.lblResponderTips.TextAlign = ContentAlignment.MiddleRight;
            this.lnkAutoRespondHelp.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            this.lnkAutoRespondHelp.AutoSize = true;
            this.lnkAutoRespondHelp.LinkBehavior = LinkBehavior.HoverUnderline;
            this.lnkAutoRespondHelp.Location = new Point(0x222, 11);
            this.lnkAutoRespondHelp.Name = "lnkAutoRespondHelp";
            this.lnkAutoRespondHelp.Size = new Size(0x1c, 13);
            this.lnkAutoRespondHelp.TabIndex = 9;
            this.lnkAutoRespondHelp.TabStop = true;
            this.lnkAutoRespondHelp.Text = "Help";
            this.lnkAutoRespondHelp.LinkClicked += new LinkLabelLinkClickedEventHandler(this.lnkAutoRespondHelp_LinkClicked);
            this.cbAutoRespond.Location = new Point(7, 0x27);
            this.cbAutoRespond.Name = "cbAutoRespond";
            this.cbAutoRespond.Size = new Size(0xec, 0x12);
            this.cbAutoRespond.TabIndex = 6;
            this.cbAutoRespond.Text = "Enable automatic responses";
            this.cbAutoRespond.UseVisualStyleBackColor = false;
            this.cbAutoRespond.CheckedChanged += new EventHandler(this.cbAutoRespond_CheckedChanged);
            this.pnlAutoResponders.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.pnlAutoResponders.AutoScroll = true;
            this.pnlAutoResponders.Controls.Add(this.lvRespondRules);
            this.pnlAutoResponders.Controls.Add(this.gbResponderEditor);
            this.pnlAutoResponders.Controls.Add(this.pnlResponderActions);
            this.pnlAutoResponders.Enabled = false;
            this.pnlAutoResponders.Location = new Point(3, 60);
            this.pnlAutoResponders.Name = "pnlAutoResponders";
            this.pnlAutoResponders.Size = new Size(0x240, 0x141);
            this.pnlAutoResponders.TabIndex = 7;
            this.lvRespondRules.AllowDrop = true;
            this.lvRespondRules.CheckBoxes = true;
            this.lvRespondRules.Columns.AddRange(new ColumnHeader[] { this.colRespondMatch, this.colRespondWith, this.colRespondLatency });
            this.lvRespondRules.ContextMenuStrip = this.mnuContextAutoResponder;
            this.lvRespondRules.Dock = DockStyle.Fill;
            this.lvRespondRules.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.lvRespondRules.FullRowSelect = true;
            this.lvRespondRules.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            this.lvRespondRules.HideSelection = false;
            this.lvRespondRules.Location = new Point(0, 0x18);
            this.lvRespondRules.Name = "lvRespondRules";
            this.lvRespondRules.Size = new Size(0x240, 0xda);
            this.lvRespondRules.TabIndex = 1;
            this.lvRespondRules.UseCompatibleStateImageBehavior = false;
            this.lvRespondRules.View = View.Details;
            this.lvRespondRules.ItemChecked += new ItemCheckedEventHandler(this.lvRespondRules_ItemChecked);
            this.lvRespondRules.SelectedIndexChanged += new EventHandler(this.lvRespondRules_SelectedIndexChanged);
            this.lvRespondRules.DragDrop += new DragEventHandler(this.lvRespondRules_DragDrop);
            this.lvRespondRules.DragOver += new DragEventHandler(this.lvRespondRules_DragOver);
            this.lvRespondRules.KeyDown += new KeyEventHandler(this.lvRespondRules_KeyDown);
            this.colRespondMatch.Text = "If URI matches...";
            this.colRespondMatch.Width = 250;
            this.colRespondWith.Text = "then respond with...";
            this.colRespondWith.Width = 250;
            this.colRespondLatency.Text = "Latency";
            this.colRespondLatency.Width = 0;
            this.mnuContextAutoResponder.Items.AddRange(new ToolStripItem[] { this.miRemoveRule, this.miPromoteRule, this.miDemoteRule, this.miRespondSetLatency, this.miRespondCloneRule, this.toolStripMenuItem2, this.miAREdit, this.miAutoResponderGenerate, this.miEditARFileWith, this.toolStripMenuItem3, this.miRuleOpenURL, this.toolStripMenuItem1, this.miExportRules });
            this.mnuContextAutoResponder.Name = "mnuContextAutoResponder";
            this.mnuContextAutoResponder.Size = new Size(0x99, 0x108);
            this.mnuContextAutoResponder.Opening += new CancelEventHandler(this.mnuContextAutoResponder_Opening);
            this.miRemoveRule.Name = "miRemoveRule";
            this.miRemoveRule.Size = new Size(0x98, 0x16);
            this.miRemoveRule.Text = "&Remove";
            this.miRemoveRule.Click += new EventHandler(this.miRemoveRule_Click);
            this.miPromoteRule.Name = "miPromoteRule";
            this.miPromoteRule.Size = new Size(0x98, 0x16);
            this.miPromoteRule.Text = "&Promote Rule";
            this.miPromoteRule.Click += new EventHandler(this.miPromoteRule_Click);
            this.miDemoteRule.Name = "miDemoteRule";
            this.miDemoteRule.Size = new Size(0x98, 0x16);
            this.miDemoteRule.Text = "&Demote Rule";
            this.miDemoteRule.Click += new EventHandler(this.miDemoteRule_Click);
            this.miRespondSetLatency.Name = "miRespondSetLatency";
            this.miRespondSetLatency.Size = new Size(0x98, 0x16);
            this.miRespondSetLatency.Text = "Set &Latency...";
            this.miRespondSetLatency.Click += new EventHandler(this.miRespondSetLatency_Click);
            this.miRespondCloneRule.Name = "miRespondCloneRule";
            this.miRespondCloneRule.Size = new Size(0x98, 0x16);
            this.miRespondCloneRule.Text = "&Clone Rule...";
            this.miRespondCloneRule.Click += new EventHandler(this.miRespondCloneRule_Click);
            this.miAutoResponderGenerate.Name = "miAutoResponderGenerate";
            this.miAutoResponderGenerate.Size = new Size(0x98, 0x16);
            this.miAutoResponderGenerate.Text = "&Generate File";
            this.miAutoResponderGenerate.Click += new EventHandler(this.miAutoResponderGenerate_Click);
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new Size(0x95, 6);
            this.miRuleOpenURL.Name = "miRuleOpenURL";
            this.miRuleOpenURL.Size = new Size(0x98, 0x16);
            this.miRuleOpenURL.Text = "&Open URL";
            this.miRuleOpenURL.Click += new EventHandler(this.miRuleOpenURL_Click);
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new Size(0x95, 6);
            this.miExportRules.Name = "miExportRules";
            this.miExportRules.Size = new Size(0x98, 0x16);
            this.miExportRules.Text = "E&xport";
            this.miExportRules.Click += new EventHandler(this.miExportRules_Click);
            this.gbResponderEditor.BackColor = SystemColors.Control;
            this.gbResponderEditor.Controls.Add(this.cbxRuleURI);
            this.gbResponderEditor.Controls.Add(this.lblMultipleMatch);
            this.gbResponderEditor.Controls.Add(this.cbxRuleAction);
            this.gbResponderEditor.Controls.Add(this.btnSaveRule);
            this.gbResponderEditor.Dock = DockStyle.Bottom;
            this.gbResponderEditor.Enabled = false;
            this.gbResponderEditor.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.gbResponderEditor.Location = new Point(0, 0xf2);
            this.gbResponderEditor.Name = "gbResponderEditor";
            this.gbResponderEditor.Size = new Size(0x240, 0x4f);
            this.gbResponderEditor.TabIndex = 2;
            this.gbResponderEditor.TabStop = false;
            this.gbResponderEditor.Text = "Rule Editor";
            this.cbxRuleURI.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.cbxRuleURI.FormattingEnabled = true;
            this.cbxRuleURI.Items.AddRange(new object[] { @"regex:(?insx).*\.jpg$ #Match strings ending with JPG", @"regex:(?insx).*\.(gif|png|jpg)$ #Match strings ending with img types", @"regex:(?insx)^https://.*\.gif$ #Match HTTPS-delivered GIFs" });
            this.cbxRuleURI.Location = new Point(8, 0x10);
            this.cbxRuleURI.Name = "cbxRuleURI";
            this.cbxRuleURI.Size = new Size(500, 0x15);
            this.cbxRuleURI.TabIndex = 0;
            this.cbxRuleURI.KeyDown += new KeyEventHandler(this.cbxRuleURI_KeyDown);
            this.lblMultipleMatch.AutoSize = true;
            this.lblMultipleMatch.Font = new Font("Tahoma", 8.25f, FontStyle.Italic, GraphicsUnit.Point, 0);
            this.lblMultipleMatch.Location = new Point(10, 20);
            this.lblMultipleMatch.Name = "lblMultipleMatch";
            this.lblMultipleMatch.Size = new Size(0xdf, 13);
            this.lblMultipleMatch.TabIndex = 3;
            this.lblMultipleMatch.Text = "Update all selected matches to respond with:";
            this.cbxRuleAction.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.cbxRuleAction.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            this.cbxRuleAction.AutoCompleteSource = AutoCompleteSource.FileSystem;
            this.cbxRuleAction.Font = new Font("Tahoma", 8.25f);
            this.cbxRuleAction.ItemHeight = 13;
            this.cbxRuleAction.Location = new Point(8, 0x2b);
            this.cbxRuleAction.MaxDropDownItems = 20;
            this.cbxRuleAction.Name = "cbxRuleAction";
            this.cbxRuleAction.Size = new Size(500, 0x15);
            this.cbxRuleAction.TabIndex = 1;
            this.cbxRuleAction.SelectionChangeCommitted += new EventHandler(this.cbxRuleAction_SelectionChangeCommitted);
            this.cbxRuleAction.KeyDown += new KeyEventHandler(this.cbxRuleAction_KeyDown);
            this.btnSaveRule.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            this.btnSaveRule.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.btnSaveRule.Location = new Point(0x200, 20);
            this.btnSaveRule.Name = "btnSaveRule";
            this.btnSaveRule.Size = new Size(0x3a, 0x2a);
            this.btnSaveRule.TabIndex = 2;
            this.btnSaveRule.Text = "Save";
            this.btnSaveRule.Click += new EventHandler(this.btnSaveRule_Click);
            this.pnlResponderActions.Controls.Add(this.btnRespondAdd);
            this.pnlResponderActions.Controls.Add(this.btnRespondImport);
            this.pnlResponderActions.Controls.Add(this.cbRespondUseLatency);
            this.pnlResponderActions.Controls.Add(this.cbRespondPassthrough);
            this.pnlResponderActions.Dock = DockStyle.Top;
            this.pnlResponderActions.Location = new Point(0, 0);
            this.pnlResponderActions.Name = "pnlResponderActions";
            this.pnlResponderActions.Size = new Size(0x240, 0x18);
            this.pnlResponderActions.TabIndex = 0;
            this.btnRespondAdd.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.btnRespondAdd.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.btnRespondAdd.Location = new Point(0x200, 0);
            this.btnRespondAdd.Name = "btnRespondAdd";
            this.btnRespondAdd.Size = new Size(0x40, 0x16);
            this.btnRespondAdd.TabIndex = 3;
            this.btnRespondAdd.Text = "Add...";
            this.btnRespondAdd.Click += new EventHandler(this.btnRespondAdd_Click);
            this.btnRespondImport.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            this.btnRespondImport.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.btnRespondImport.Location = new Point(0x1ac, 0);
            this.btnRespondImport.Name = "btnRespondImport";
            this.btnRespondImport.Size = new Size(80, 0x16);
            this.btnRespondImport.TabIndex = 2;
            this.btnRespondImport.Text = "Import...";
            this.btnRespondImport.Click += new EventHandler(this.btnRespondImport_Click);
            this.cbRespondUseLatency.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.cbRespondUseLatency.Location = new Point(0xcd, 0);
            this.cbRespondUseLatency.Name = "cbRespondUseLatency";
            this.cbRespondUseLatency.Size = new Size(0x6a, 0x15);
            this.cbRespondUseLatency.TabIndex = 1;
            this.cbRespondUseLatency.Text = "Enable Latency";
            this.cbRespondUseLatency.CheckedChanged += new EventHandler(this.cbRespondUseLatency_CheckedChanged);
            this.cbRespondPassthrough.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.cbRespondPassthrough.Location = new Point(4, 0);
            this.cbRespondPassthrough.Name = "cbRespondPassthrough";
            this.cbRespondPassthrough.Size = new Size(0xc3, 0x15);
            this.cbRespondPassthrough.TabIndex = 0;
            this.cbRespondPassthrough.Text = "Unmatched requests passthrough";
            this.cbRespondPassthrough.CheckedChanged += new EventHandler(this.cbRespondPassthrough_CheckedChanged);
            this.lblAutoRespondHeader.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.lblAutoRespondHeader.BackColor = Color.Gray;
            this.lblAutoRespondHeader.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.lblAutoRespondHeader.ForeColor = Color.White;
            this.lblAutoRespondHeader.Location = new Point(3, 0);
            this.lblAutoRespondHeader.Name = "lblAutoRespondHeader";
            this.lblAutoRespondHeader.Padding = new Padding(4);
            this.lblAutoRespondHeader.Size = new Size(0x219, 0x22);
            this.lblAutoRespondHeader.TabIndex = 5;
            this.lblAutoRespondHeader.Text = "Fiddler can return previously generated responses instead of connecting to the server.";
            this.lblAutoRespondHeader.TextAlign = ContentAlignment.MiddleLeft;
            this.miEditARFileWith.Name = "miEditARFileWith";
            this.miEditARFileWith.Size = new Size(0x98, 0x16);
            this.miEditARFileWith.Text = "Edit &File With...";
            this.miEditARFileWith.Click += new EventHandler(this.miEditARFileWith_Click);
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new Size(0x95, 6);
            this.miAREdit.Name = "miAREdit";
            this.miAREdit.Size = new Size(0x98, 0x16);
            this.miAREdit.Text = "&Edit Response";
            this.miAREdit.Click += new EventHandler(this.miAREdit_Click);
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.Controls.Add(this.cbAutoRespond);
            base.Controls.Add(this.lblResponderTips);
            base.Controls.Add(this.lnkAutoRespondHelp);
            base.Controls.Add(this.pnlAutoResponders);
            base.Controls.Add(this.lblAutoRespondHeader);
            this.Font = new Font("Tahoma", 8.25f);
            base.Name = "UIAutoResponder";
            base.Size = new Size(0x246, 0x180);
            this.pnlAutoResponders.ResumeLayout(false);
            this.mnuContextAutoResponder.ResumeLayout(false);
            this.gbResponderEditor.ResumeLayout(false);
            this.gbResponderEditor.PerformLayout();
            this.pnlResponderActions.ResumeLayout(false);
            base.ResumeLayout(false);
            base.PerformLayout();
        }

        private void lnkAutoRespondHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Utilities.LaunchHyperlink(CONFIG.GetUrl("AutoResponderHelp"));
        }

        private void lvRespondRules_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                this.lvRespondRules.BeginUpdate();
                string[] data = (string[]) e.Data.GetData("FileDrop", false);
                foreach (string str in data)
                {
                    if (Directory.Exists(str))
                    {
                        FiddlerApplication._AutoResponder.CreateRulesForFolder(str);
                    }
                    if (str.EndsWith(".saz", StringComparison.OrdinalIgnoreCase))
                    {
                        FiddlerApplication._AutoResponder.ImportSAZ(str);
                    }
                    else
                    {
                        FiddlerApplication._AutoResponder.CreateRuleForFile(str, null);
                    }
                }
                this.lvRespondRules.EndUpdate();
                this._SelectMostRecentLVRespondRule();
            }
            else
            {
                Session[] oSessions = (Session[]) e.Data.GetData("Fiddler.Session[]");
                if ((oSessions != null) && (oSessions.Length >= 1))
                {
                    FiddlerApplication._AutoResponder.ImportSessions(oSessions);
                    this._SelectMostRecentLVRespondRule();
                }
            }
        }

        private void lvRespondRules_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Fiddler.Session[]") || e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void lvRespondRules_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            ResponderRule tag = (ResponderRule) e.Item.Tag;
            if (tag != null)
            {
                tag.IsEnabled = e.Item.Checked;
            }
        }

        private void lvRespondRules_KeyDown(object sender, KeyEventArgs e)
        {
            Keys keyCode = e.KeyCode;
            if (keyCode <= Keys.Delete)
            {
                switch (keyCode)
                {
                    case Keys.Return:
                        if (this.lvRespondRules.SelectedItems.Count == 1)
                        {
                            ListViewItem lvi = this.lvRespondRules.SelectedItems[0];
                            this.actLaunchEditor(lvi);
                        }
                        break;

                    case Keys.Delete:
                        this.actRemoveSelectedRules();
                        e.SuppressKeyPress = true;
                        break;
                }
            }
            else
            {
                switch (keyCode)
                {
                    case Keys.A:
                        if (e.Control)
                        {
                            this.lvRespondRules.BeginUpdate();
                            foreach (ListViewItem item in this.lvRespondRules.Items)
                            {
                                item.Selected = true;
                            }
                            this.lvRespondRules.EndUpdate();
                        }
                        return;

                    case Keys.B:
                    case Keys.Separator:
                    case Keys.Oemcomma:
                        return;

                    case Keys.C:
                        if (e.Control && (this.lvRespondRules.SelectedItems.Count >= 1))
                        {
                            StringBuilder builder = new StringBuilder();
                            foreach (ListViewItem item2 in this.lvRespondRules.SelectedItems)
                            {
                                builder.AppendFormat("{0}\t{1}\r\n", item2.Text, item2.SubItems[1].Text);
                            }
                            Utilities.CopyToClipboard(builder.ToString());
                            e.SuppressKeyPress = true;
                            return;
                        }
                        return;

                    case Keys.Add:
                    case Keys.Oemplus:
                        this.actPromoteRule();
                        e.SuppressKeyPress = true;
                        return;

                    case Keys.Subtract:
                    case Keys.OemMinus:
                        this.actDemoteRule();
                        e.SuppressKeyPress = true;
                        return;

                    default:
                        return;
                }
            }
        }

        private void lvRespondRules_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.lvRespondRules.SelectedItems.Count == 0)
            {
                this.gbResponderEditor.Enabled = false;
            }
            else if (this.lvRespondRules.SelectedItems.Count > 1)
            {
                this.cbxRuleURI.Visible = false;
            }
            else
            {
                ListViewItem item = this.lvRespondRules.SelectedItems[0];
                ResponderRule tag = (ResponderRule) item.Tag;
                this.cbxRuleURI.Visible = true;
                this.cbxRuleURI.Text = tag.sMatch;
                this.cbxRuleAction.Text = tag.sAction;
                this.gbResponderEditor.Enabled = true;
            }
        }

        private void miAREdit_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection selectedItems = this.lvRespondRules.SelectedItems;
            if (selectedItems.Count == 1)
            {
                if (selectedItems[0] != null)
                {
                    this.actLaunchEditor(selectedItems[0]);
                }
                else
                {
                    ResponderRule tag = (ResponderRule) selectedItems[0].Tag;
                    if (!File.Exists(tag.sAction))
                    {
                        MessageBox.Show("No local file exists for this rule");
                    }
                    else
                    {
                        using (Process.Start("rundll32", "shell32.dll,OpenAs_RunDLL " + tag.sAction))
                        {
                        }
                    }
                }
            }
        }

        private void miAutoResponderGenerate_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection selectedItems = this.lvRespondRules.SelectedItems;
            if (selectedItems.Count >= 1)
            {
                this.lvRespondRules.BeginUpdate();
                foreach (ListViewItem item in selectedItems)
                {
                    ResponderRule tag = (ResponderRule) item.Tag;
                    if (tag != null)
                    {
                        tag.ConvertToFileBackedRule();
                    }
                }
                this.lvRespondRules.SelectedItems.Clear();
                this.lvRespondRules.EndUpdate();
            }
        }

        private void miDemoteRule_Click(object sender, EventArgs e)
        {
            this.actDemoteRule();
        }

        private void miEditARFileWith_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection selectedItems = this.lvRespondRules.SelectedItems;
            if (selectedItems.Count == 1)
            {
                ResponderRule tag = (ResponderRule) selectedItems[0].Tag;
                if (tag != null)
                {
                    tag.ConvertToFileBackedRule();
                }
                if (!File.Exists(tag.sAction))
                {
                    MessageBox.Show("No local file exists for this rule");
                }
                else
                {
                    using (Process.Start("rundll32", "shell32.dll,OpenAs_RunDLL " + tag.sAction))
                    {
                    }
                }
            }
        }

        private void miExportRules_Click(object sender, EventArgs e)
        {
            string str = Utilities.ObtainSaveFilename("Export rules...", "AutoResponder Ruleset|*.farx");
            if (!string.IsNullOrEmpty(str))
            {
                if (!str.EndsWith(".farx", StringComparison.OrdinalIgnoreCase))
                {
                    str = str + ".farx";
                }
                if (FiddlerApplication._AutoResponder.ExportFARX(str))
                {
                    FiddlerApplication.UI.sbpInfo.Text = "Exported AutoResponder Rules to: " + str;
                }
                else
                {
                    FiddlerApplication.UI.sbpInfo.Text = "Failed to export AutoResponder Rules.";
                }
            }
        }

        private void miPromoteRule_Click(object sender, EventArgs e)
        {
            this.actPromoteRule();
        }

        private void miRemoveRule_Click(object sender, EventArgs e)
        {
            this.actRemoveSelectedRules();
        }

        private void miRespondCloneRule_Click(object sender, EventArgs e)
        {
            if (this.lvRespondRules.SelectedItems.Count == 1)
            {
                ResponderRule tag = (ResponderRule) this.lvRespondRules.SelectedItems[0].Tag;
                byte[] arrResponseBody = null;
                HTTPResponseHeaders oRH = null;
                if (tag.HasImportedResponse)
                {
                    oRH = (HTTPResponseHeaders) tag._oResponseHeaders.Clone();
                    arrResponseBody = (byte[]) tag._arrResponseBodyBytes.Clone();
                }
                FiddlerApplication._AutoResponder.AddRule(tag.sMatch, oRH, arrResponseBody, tag.sAction, tag.iLatency, tag.IsEnabled);
            }
        }

        private void miRespondSetLatency_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection selectedItems = this.lvRespondRules.SelectedItems;
            if (selectedItems.Count >= 1)
            {
                if (!this.cbRespondUseLatency.Checked)
                {
                    this.cbRespondUseLatency.Checked = true;
                }
                int result = 0;
                string sDefault = "0";
                if (selectedItems.Count == 1)
                {
                    sDefault = ((ResponderRule) selectedItems[0].Tag).iLatency.ToString();
                }
                string s = frmPrompt.GetUserString("Adjust Latency", "Enter the exact number of milliseconds by which to delay the response, or use a leading + or - to adjust the current latency.", sDefault, true);
                if ((s != null) && int.TryParse(s, out result))
                {
                    bool flag = s.StartsWith("+") || s.StartsWith("-");
                    this.lvRespondRules.BeginUpdate();
                    foreach (ListViewItem item in selectedItems)
                    {
                        int num2;
                        if (flag)
                        {
                            num2 = Math.Max(((ResponderRule) item.Tag).iLatency + result, 0);
                        }
                        else
                        {
                            num2 = result;
                        }
                        ((ResponderRule) item.Tag).iLatency = num2;
                        item.SubItems[2].Text = num2.ToString();
                    }
                    this.lvRespondRules.EndUpdate();
                    FiddlerApplication._AutoResponder.IsRuleListDirty = true;
                }
            }
        }

        private void miRuleOpenURL_Click(object sender, EventArgs e)
        {
            if (this.lvRespondRules.SelectedItems.Count == 1)
            {
                ListViewItem item = this.lvRespondRules.SelectedItems[0];
                string sMatch = (item.Tag as ResponderRule).sMatch;
                if (sMatch.StartsWith("EXACT:", StringComparison.OrdinalIgnoreCase))
                {
                    sMatch = sMatch.Substring(6);
                }
                if ((!sMatch.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !sMatch.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) && !sMatch.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
                {
                    sMatch = "http://" + sMatch;
                }
                Utilities.LaunchHyperlink(sMatch);
            }
        }

        private void mnuContextAutoResponder_Opening(object sender, CancelEventArgs e)
        {
            this.miAutoResponderGenerate.Enabled = this.miRemoveRule.Enabled = this.miRespondSetLatency.Enabled = this.lvRespondRules.SelectedItems.Count > 0;
            this.miAREdit.Enabled = this.miEditARFileWith.Enabled = this.miRespondCloneRule.Enabled = this.miRuleOpenURL.Enabled = this.miDemoteRule.Enabled = this.miPromoteRule.Enabled = this.lvRespondRules.SelectedItems.Count == 1;
            this.miExportRules.Enabled = this.lvRespondRules.Items.Count > 0;
        }
    }
}

