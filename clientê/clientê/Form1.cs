using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace clientê
{
    public partial class Form1 : Form
    {
        private UdpClient udpClient;
        private const int clientPort = 11001; // Change this port for different clients
        private const int serverPort = 11000;
        private bool serverRunning = false;
        private Thread receiveThread;

        public Form1()
        {
            InitializeComponent();
            udpClient = new UdpClient(clientPort); // Bind client to its own port
        }

        private void ReceiveMessages()
        {
            try
            {
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    byte[] receivedData = udpClient.Receive(ref serverEndPoint);
                    string receivedMessage = Encoding.UTF8.GetString(receivedData);
                    Invoke(new MethodInvoker(delegate
                    {
                        if (receivedMessage == "SERVER START")
                        {
                            serverRunning = true;
                            MessageBox.Show("Server has started. You can now send messages.");
                        }
                        else if (receivedMessage == "SERVER STOP")
                        {
                            serverRunning = false;
                            MessageBox.Show("Server has stopped. You cannot send messages until the server is started again.");
                        }
                        else
                        {
                            richTextBoxMessages.AppendText("Server: " + receivedMessage + Environment.NewLine);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while receiving messages: " + ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // Send CHECK message to server to verify if it's running
            Thread checkThread = new Thread(CheckServerStatus);
            checkThread.Start();
        }

        private void CheckServerStatus()
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes("CHECK");
                udpClient.Send(data, data.Length, "127.0.0.1", serverPort);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while checking server status: " + ex.Message);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (!serverRunning)
            {
                MessageBox.Show("Cannot send message. Server is not running.");
                return;
            }

            try
            {
                string message = txtMessage.Text;
                byte[] data = Encoding.UTF8.GetBytes(message);
                udpClient.Send(data, data.Length, "127.0.0.1", serverPort);
                richTextBoxMessages.AppendText("Client: " + message + Environment.NewLine);
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while sending messages: " + ex.Message);
            }
        }
    }
}
