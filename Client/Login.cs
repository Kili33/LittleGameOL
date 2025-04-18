using System;
using System.Windows.Forms;

namespace Client
{
    public partial class Login : Form
    {
        public string Name;
        public string Ip;

        public Login()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Ip = textBox1.Text;
            Name = textBox2.Text;
            this.Close();
        }

        private void textBox2_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1_Click(null, null);
            }
        }
    }
}