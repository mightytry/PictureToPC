using Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PictureToPC.Networking
{
    internal class Connection
    {
        private TcpClient client;
        private NetworkStream stream;
        public bool connected;
        public string connCode;
        private byte[] buffer;
        private readonly Form1 form;
        private Action<int> OnDataReceved;
        public Connection(Form1 form, Action<int> OnDataReceved)
        {
            this.OnDataReceved = OnDataReceved;
            this.form = form;
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
                    OnDataReceved((int)((float)bytesRead / size * 100));
                    old = bytesRead;
                }
                return data;
            }
            return null;
        }
        public void Close()
        {
            if (stream != null)
            {
                stream.Close();
            }

            if (client != null)
            {
                client.Close();
            }
            try { form.Invoke(new Action(() => form.checkBox.Checked = false)); }
            catch { }

            connected = false;

        }

        public void Loop(IPEndPoint endPoint)
        {
            try { client = new TcpClient(endPoint.Address.ToString(), endPoint.Port); }
            catch { return; }
            

            stream = client.GetStream();
            connected = true;
            form.Invoke(new Action(() => form.checkBox.Checked = true));
            buffer = new byte[32768];

            while (connected)
            {

                string? pictureData = Receive();
                if (pictureData == null)
                {
                    Close();
                    return;
                }

                if (pictureData.Length == 0)
                {
                    Close();
                    return;
                }

                int s = int.Parse(pictureData);

                if (s == -1)
                {
                    continue;
                }

                byte[]? pictureBytes = Receive(s);
                if (pictureBytes == null)
                {
                    Close();
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
