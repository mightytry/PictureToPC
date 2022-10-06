using Emgu.CV;
using Forms;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace PictureToPC
{
    internal static class Networking
    {

    }
    internal class Client
    {
        public Socket Socket;

        public Client()
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Connect(string ip, int port)
        {
            try 
            {
                Socket.Connect(new IPEndPoint(IPAddress.Parse(ip), port)); 
            }
            catch (SocketException e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public void Send(byte[] data)
        {
            Socket.SendAsync(data, SocketFlags.Partial);
        }
    }

    internal class Server
    {
        private readonly TcpListener listener;
        private TcpClient client;
        private NetworkStream stream;
        private readonly int port;
        private readonly IPAddress[] addressList;
        private bool connected;
        private byte[] buffer;
        private readonly Form1 form;
        private Client sendToClient;
        private Action<int> OnDataReceved;
        public Server(int port, Form1 form, Action<int> OnDataReceved)
        {
            this.OnDataReceved = OnDataReceved;
            this.port = port;
            addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            listener = new TcpListener(localaddr: addressList[^1], port);
            listener.Start();
            form.SetLabel5(listener.LocalEndpoint.ToString().Split(':')[0]);
            this.form = form;
        }
        public void Send(string message)
        {
            if (connected)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
        }
        public string? Receive()
        {
            int bytesRead;
            if (connected)
            {
                try { bytesRead = stream.Read(buffer, 0, 32768); }
                catch (Exception)
                {
                    Close();
                    return null;
                }

                if (sendToClient != null)
                {
                    sendToClient.Send(buffer);
                }
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
            return null;
        }
        public byte[]? Receive(int size)
        {
            int bytesRead = 0;
            int old = 0;
            byte[] data = new byte[size];
            if (connected)
            {
                while (bytesRead != size)
                {
                    try
                    { 
                        bytesRead += stream.Read(data, bytesRead, size - bytesRead);
                    }
                    catch (Exception)
                    {
                        Close();
                        return null;
                    }
                    OnDataReceved((int)((float)bytesRead/size*100));
                    if (sendToClient != null)
                    {
                        sendToClient.Send(data[old..bytesRead]);
                    }
                    old = bytesRead;
                }
                return data;
            }
            return null;
        }
        public bool AddSendToClient(string ip, int port)
        {
            sendToClient = new Client();
            sendToClient.Connect(ip, port);
            if (!sendToClient.Socket.Connected)
            {
                sendToClient = null;
                return false;
            }
            return true;
        }
        public void Close()
        {

            if (listener != null)
            {
                listener.Stop();
            }

            if (stream != null)
            {
                stream.Close();
            }

            if (client != null)
            {
                client.Close();
            }

            connected = false;

        }

        public void Loop()
        {
            try
            {
                client = listener.AcceptTcpClient();
            }
            catch (Exception)
            {
                return;
            }
            stream = client.GetStream();
            connected = true;
            buffer = new byte[32768];

            while (connected)
            {
                Send("ready");
                string? pictureData = Receive();
                if (pictureData == null)
                {
                    Loop();
                    return;
                }

                if (pictureData.Length == 0)
                {
                    Loop();
                    return;
                }
                
                int s = int.Parse(pictureData);

                Console.WriteLine(s);

                Send("ready");
                byte[]? pictureBytes = Receive(s);
                if (pictureBytes == null)
                {
                    Loop();
                    return;
                }

                Bitmap im = new Bitmap(new MemoryStream(pictureBytes));

                im.RotateFlip(RotateFlipType.Rotate90FlipNone);


                //Mat mat = im.ToMat();

                //Mat dst = new(im.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);

                //CvInvoke.CvtColor(mat, dst, Emgu.CV.CvEnum.ColorConversion.Rgba2Bgr);

                //Image img = dst.ToBitmap();

                form.SetImg(im);

            }
        }
    }
}
