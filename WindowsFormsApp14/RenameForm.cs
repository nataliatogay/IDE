using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp14
{
    public partial class RenameForm : Form
    {
        public string FileName { get; private set; }
        public RenameForm(string name)
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(name))
            {
                this.textBox1.Text = name;
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            FileName = textBox1.Text;
            DialogResult = DialogResult.OK;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
