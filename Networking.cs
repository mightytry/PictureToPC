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
        public Server(int port, Form1 form)
        {
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
                try { bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize); }
                catch (Exception)
                {
                    Close();
                    return null;
                }

                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
            return null;
        }
        public byte[]? Receive(int size)
        {
            int bytesRead = 0;
            byte[] data = new byte[size];
            if (connected)
            {
                while (bytesRead != size)
                {
                    try
                    { bytesRead += stream.Read(data, bytesRead, size - bytesRead); }
                    catch (Exception)
                    {
                        Close();
                        return null;
                    }
                }
                return data;
            }
            return null;
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
            buffer = new byte[client.ReceiveBufferSize];

            while (connected)
            {
                Send("ready");
                string? pictureData = Receive();
                if (pictureData == null)
                {
                    return;
                }

                string[] pdl = pictureData.Split(',');

                int s = int.Parse(pdl[0]);
                int height = int.Parse(pdl[1]);
                int width = int.Parse(pdl[2]);

                Console.WriteLine(s + ", " + height + ", " + width);

                Send("ready");
                byte[]? pictureBytes = Receive(s);
                if (pictureBytes == null)
                {
                    return;
                }

                Bitmap im = new(width / 4, height, width,
                    PixelFormat.Format32bppArgb,
                    Marshal.UnsafeAddrOfPinnedArrayElement(pictureBytes, 0));

                im.RotateFlip(RotateFlipType.Rotate90FlipNone);


                Mat mat = im.ToMat();

                Mat dst = new(im.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);

                CvInvoke.CvtColor(mat, dst, Emgu.CV.CvEnum.ColorConversion.Rgba2Bgr);

                Image img = dst.ToBitmap();

                form.SetImg(img);

            }
        }
    }
}
