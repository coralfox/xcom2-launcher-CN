using System;
using System.Windows.Forms;
using XCOM2Launcher.XCOM;

namespace XCOM2Launcher.Forms
{
    public partial class WelcomeDialog : Form
    {
        public WelcomeDialog()
        {
            InitializeComponent();
            Game = GameId.X2;
        }

        public bool UseSentry => rSentryEnabled.Checked;
        public GameId Game { get; private set; }

        private void bContinue_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void WelcomeDialog_Load(object sender, EventArgs e)
        {
            // Text += " " + Program.GetCurrentVersionString(true);
        }

        private void GameSelectionChanged(object sender, EventArgs e)
        {
            bContinue.Enabled = true;
            Game = rGameChimera.Checked ? GameId.ChimeraSquad : GameId.X2;
        }
    }
}