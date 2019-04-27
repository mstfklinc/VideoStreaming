using System;
using System.Windows.Forms;
using Streaming;

namespace VideoStreamServer
{
    public partial class Form1 : Form
    {
        private ImageStreamingServer _Server;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _Server = new ImageStreamingServer();
            _Server.Start(8080);
        }
    }
}
