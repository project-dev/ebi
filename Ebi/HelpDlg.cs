using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ebi
{
    public partial class HelpDlg : Form
    {
        public HelpDlg()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void HelpDlg_Load(object sender, EventArgs e)
        {
            var path = Application.ExecutablePath;

            helpView.Url = new Uri("/help/index.html");
        }
    }
}
