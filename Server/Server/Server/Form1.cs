using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Server
{
    public partial class Form1 : Form
    {
        private UdpClient udpServer;
        private const int serverPort = 11000;
        private List<int> clientPorts;
        private Thread receiveThread;
        private bool isRunning;

        public Form1()
        {
            InitializeComponent();
            udpServer = new UdpClient(new IPEndPoint(IPAddress.Parse("192.168.80.113"), serverPort));
            clientPorts = new List<int> { 11001, 11003 };
            txtMessage.TextChanged += TxtMessage_TextChanged;
            isRunning = false;
        }

        private void TxtMessage_TextChanged(object sender, EventArgs e)
        {
            AdjustTextBoxHeight();
        }

        private void AdjustTextBoxHeight()
        {
            int padding = 10;
            int numLines = txtMessage.GetLineFromCharIndex(txtMessage.TextLength) + 1;
            int newHeight = (numLines * txtMessage.Font.Height) + padding;
            int maxHeight = 200;

            if (newHeight > maxHeight)
            {
                newHeight = maxHeight;
            }

            txtMessage.Height = newHeight;
        }

        private void ReceiveMessages()
        {
            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                while (isRunning)
                {
                    byte[] receivedData = udpServer.Receive(ref clientEndPoint);
                    string receivedMessage = Encoding.UTF8.GetString(receivedData);

                    if (receivedMessage == "CHECK")
                    {
                        byte[] response = Encoding.UTF8.GetBytes("SERVER START");
                        udpServer.Send(response, response.Length, clientEndPoint);
                    }
                    else
                    {
                        Invoke(new MethodInvoker(delegate
                        {
                            richTextBoxMessages.AppendText($"Client ({clientEndPoint.Address}:{clientEndPoint.Port}): {receivedMessage}{Environment.NewLine}");
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                if (isRunning)
                {
                    MessageBox.Show("An error occurred while receiving messages: " + ex.Message);
                }
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (!isRunning)
            {
                MessageBox.Show("Server is not running. Please start the server first.");
                return;
            }

            try
            {
                string message = txtMessage.Text;
                byte[] data = Encoding.UTF8.GetBytes(message);

                if (chkSendToAllPorts.Checked)
                {
                    foreach (int port in clientPorts)
                    {
                        udpServer.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, port)); // Use broadcast for simplicity
                    }
                    richTextBoxMessages.AppendText("Server sent to (all): " + message + Environment.NewLine);
                }
                else
                {
                    string[] ports = txtClientPort.Text.Split('-');
                    List<int> targetPorts = new List<int>();

                    foreach (string port in ports)
                    {
                        if (int.TryParse(port, out int parsedPort))
                        {
                            targetPorts.Add(parsedPort);
                            udpServer.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, parsedPort)); // Use broadcast for simplicity
                        }
                    }

                    string portList = string.Join("-", targetPorts);
                    richTextBoxMessages.AppendText($"Server sent to ({portList}): " + message + Environment.NewLine);
                }

                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while sending messages: " + ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!isRunning)
            {
                isRunning = true;
                receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();
                richTextBoxMessages.AppendText("Server started." + Environment.NewLine);

                byte[] startData = Encoding.UTF8.GetBytes("SERVER START");
                foreach (int port in clientPorts)
                {
                    udpServer.Send(startData, startData.Length, new IPEndPoint(IPAddress.Broadcast, port));
                }

                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (isRunning)
            {
                isRunning = false;
                byte[] data = Encoding.UTF8.GetBytes("SERVER STOP");
                foreach (int port in clientPorts)
                {
                    udpServer.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, port));
                }
                receiveThread.Join();
                richTextBoxMessages.AppendText("Server stopped." + Environment.NewLine);
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
        }
    }
}
