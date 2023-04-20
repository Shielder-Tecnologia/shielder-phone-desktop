using System.Drawing;
using System.Windows.Forms;
using ContactPoint.Common;

namespace ContactPoint.Forms
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();

            try
            {
                pictureBoxPartnerLogo.Image = Image.FromFile("partner_logo.png");
            }
            catch
            {
                Logger.LogNotice("Unable to load partner logo.");
            }

            labelVersion.Text = GetType().Assembly.GetName().Version.ToString(4);
        }

        private void AboutForm_Load(object sender, System.EventArgs e)
        {

        }

        private void label1_Click(object sender, System.EventArgs e)
        {

        }
    }
}
