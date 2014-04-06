using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ffxivlib;
using System.Diagnostics;

namespace Chocorep2
{
    public partial class ChoiceProcForm : Form
    {
        public ChoiceProcForm()
        {
            InitializeComponent();
        }
        public int SelectedFFXIVProcessID
        {
            get;
            set;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void ChoiceProcForm_Load(object sender, EventArgs e)
        {
            DataTable table = new DataTable();
            table.Columns.Add("ProcessID", typeof(int));
            table.Columns.Add("CharName", typeof(string));
            foreach (Process proc in Process.GetProcessesByName("ffxiv"))
            {
                try
                {
                    FFXIVLIB lib = new FFXIVLIB(proc.Id);
                    Entity me = lib.GetEntityInfo(0);
                    if (me != null)
                    {
                        table.Rows.Add(proc.Id, me.Name);
                    }
                    else
                    {
                        table.Rows.Add(proc.Id, proc.Id.ToString());
                    }
                }
                catch
                {
                }
            }
            ProcComboBox.DataSource = table;
            ProcComboBox.DisplayMember = "CharName";
            ProcComboBox.ValueMember = "ProcessID";
            ProcComboBox.SelectedIndex = -1;
        }

        private void ProcComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ProcComboBox.SelectedValue is int)
            {
                SelectedFFXIVProcessID = (int)ProcComboBox.SelectedValue;
                OKButton.Enabled = true;
            }
        }
    }
}
