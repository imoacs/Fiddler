namespace Fiddler
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    public class UIARRuleEditor : Form
    {
        private ResponderRule _oOwningRule;
        private Button btnSaveEdits;
        private IContainer components;
        private Label lblAutoRespondHeader;
        private Panel panel1;
        private TabControl tabsResponseEditors;
        private byte[] tmpRB;
        private HTTPResponseHeaders tmpRH;

        public UIARRuleEditor(ResponderRule oRR)
        {
            this.InitializeComponent();
            this._oOwningRule = oRR;
            base.Icon = FiddlerApplication.UI.Icon;
            FiddlerApplication.oInspectors.AddResponseInspectorsToTabControl(this.tabsResponseEditors);
            this.tmpRH = (HTTPResponseHeaders) this._oOwningRule._oResponseHeaders.Clone();
            this.tmpRB = (byte[]) this._oOwningRule._arrResponseBodyBytes.Clone();
            this.Text = "Fiddler - AutoResponse for " + this._oOwningRule.sMatch;
            FiddlerApplication.UI.actActivateTabByTitle("Headers", this.tabsResponseEditors);
            this.actUpdateEditor();
        }

        private void actCommitEdits()
        {
            if (this._oOwningRule != null)
            {
                this.actUpdateFields(this.tabsResponseEditors.SelectedTab.Tag as IResponseInspector2);
                this._oOwningRule._arrResponseBodyBytes = (byte[]) this.tmpRB.Clone();
                this._oOwningRule._oResponseHeaders = (HTTPResponseHeaders) this.tmpRH.Clone();
                FiddlerApplication.oAutoResponder.IsRuleListDirty = true;
            }
        }

        private void actUpdateEditor()
        {
            IResponseInspector2 tag = this.tabsResponseEditors.SelectedTab.Tag as IResponseInspector2;
            tag.headers = this.tmpRH;
            tag.body = this.tmpRB;
            tag.bReadOnly = false;
        }

        private void actUpdateFields(IResponseInspector2 oRI)
        {
            if ((oRI != null) && oRI.bDirty)
            {
                if (oRI.headers != null)
                {
                    this.tmpRH = oRI.headers;
                }
                if (oRI.body != null)
                {
                    this.tmpRB = oRI.body;
                }
                if (this.tmpRH.Exists("Content-Length"))
                {
                    this.tmpRH["Content-Length"] = this.tmpRB.LongLength.ToString();
                }
            }
        }

        private void btnSaveEdits_Click(object sender, EventArgs e)
        {
            this.actCommitEdits();
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
            this.panel1 = new Panel();
            this.lblAutoRespondHeader = new Label();
            this.btnSaveEdits = new Button();
            this.tabsResponseEditors = new TabControl();
            this.panel1.SuspendLayout();
            base.SuspendLayout();
            this.panel1.Controls.Add(this.lblAutoRespondHeader);
            this.panel1.Controls.Add(this.btnSaveEdits);
            this.panel1.Dock = DockStyle.Top;
            this.panel1.Location = new Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new Size(0x2b1, 0x2d);
            this.panel1.TabIndex = 0;
            this.lblAutoRespondHeader.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top;
            this.lblAutoRespondHeader.BackColor = Color.Gray;
            this.lblAutoRespondHeader.Font = new Font("Tahoma", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.lblAutoRespondHeader.ForeColor = Color.White;
            this.lblAutoRespondHeader.Location = new Point(3, 6);
            this.lblAutoRespondHeader.Name = "lblAutoRespondHeader";
            this.lblAutoRespondHeader.Padding = new Padding(4);
            this.lblAutoRespondHeader.Size = new Size(0x25a, 0x22);
            this.lblAutoRespondHeader.TabIndex = 6;
            this.lblAutoRespondHeader.Text = "Use the editors below to adjust the response sent for this rule. Click Save to commit your changes back to the rule.";
            this.lblAutoRespondHeader.TextAlign = ContentAlignment.MiddleLeft;
            this.btnSaveEdits.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            this.btnSaveEdits.Location = new Point(0x263, 6);
            this.btnSaveEdits.Name = "btnSaveEdits";
            this.btnSaveEdits.Size = new Size(0x4b, 0x21);
            this.btnSaveEdits.TabIndex = 0;
            this.btnSaveEdits.Text = "Save";
            this.btnSaveEdits.UseVisualStyleBackColor = true;
            this.btnSaveEdits.Click += new EventHandler(this.btnSaveEdits_Click);
            this.tabsResponseEditors.Dock = DockStyle.Fill;
            this.tabsResponseEditors.Location = new Point(0, 0x2d);
            this.tabsResponseEditors.Multiline = true;
            this.tabsResponseEditors.Name = "tabsResponseEditors";
            this.tabsResponseEditors.SelectedIndex = 0;
            this.tabsResponseEditors.Size = new Size(0x2b1, 0x14e);
            this.tabsResponseEditors.SizeMode = TabSizeMode.FillToRight;
            this.tabsResponseEditors.TabIndex = 1;
            this.tabsResponseEditors.SelectedIndexChanged += new EventHandler(this.tabsResponseEditors_SelectedIndexChanged);
            this.tabsResponseEditors.Deselecting += new TabControlCancelEventHandler(this.tabsResponseEditors_Deselecting);
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.ClientSize = new Size(0x2b1, 0x17b);
            base.Controls.Add(this.tabsResponseEditors);
            base.Controls.Add(this.panel1);
            base.Name = "UIARRuleEditor";
            this.Text = "UIARRuleEditor";
            base.FormClosing += new FormClosingEventHandler(this.UIARRuleEditor_FormClosing);
            this.panel1.ResumeLayout(false);
            base.ResumeLayout(false);
        }

        private void tabsResponseEditors_Deselecting(object sender, TabControlCancelEventArgs e)
        {
            this.actUpdateFields(e.TabPage.Tag as IResponseInspector2);
        }

        private void tabsResponseEditors_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!FiddlerApplication.isClosing)
            {
                this.actUpdateEditor();
            }
        }

        private void UIARRuleEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (FiddlerApplication.isClosing || (e.CloseReason != CloseReason.UserClosing))
            {
                this._oOwningRule._oEditor = null;
                base.Dispose();
            }
            else
            {
                IResponseInspector2 tag = this.tabsResponseEditors.SelectedTab.Tag as IResponseInspector2;
                if ((tag == null) || !tag.bDirty)
                {
                    this._oOwningRule._oEditor = null;
                    base.Dispose();
                }
                else
                {
                    DialogResult result = MessageBox.Show("You made changes to the current response. Do you want to commit these changes?", "Save Changes?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (DialogResult.Cancel == result)
                    {
                        e.Cancel = true;
                    }
                    else
                    {
                        if (DialogResult.Yes == result)
                        {
                            this.actCommitEdits();
                        }
                        this._oOwningRule._oEditor = null;
                        base.Dispose();
                    }
                }
            }
        }
    }
}

