using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using static PictureToPC.Networking.Messages;

namespace PictureToPC.Networking
{
    internal class Discovery
    {
        public static Socket socket;
        public static Connection conn;
        public static TextBox txtLog;

        public static void Start(Connection _conn, TextBox text, string _ip = "224.69.69.69", int _port = 42069)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, _port);

            socket.Bind(ipep);

            IPAddress ip = IPAddress.Parse(_ip);

            socket.SetSocketOption(SocketOptionLevel.IP,SocketOptionName.AddMembership, new MulticastOption(ip, IPAddress.Any));

            conn = _conn;
            txtLog = text;
        }

        public static void Recive()
        {
            while (true)
            {
                
                byte[] b = new byte[1024];
                EndPoint endPoint = new IPEndPoint(0,0);
                socket.ReceiveFrom(b, 0, 1024, SocketFlags.None, ref endPoint);

                Connect msg = JsonConvert.DeserializeObject<Connect>(Encoding.ASCII.GetString(b, 0, b.Length));

                msg.ip = endPoint.ToString().Split(':')[0];


                if (txtLog.Text == msg.code)
                {
                    conn.Loop(new IPEndPoint(IPAddress.Parse(msg.ip), msg.port));
                }
            }
        }
    }
}
