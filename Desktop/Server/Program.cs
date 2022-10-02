using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

using static Main.Messages;
using File = Main.Messages.File;
using FileInfo = Main.Messages.FileInfo;


namespace Server
{
    internal static class Program
    {
        private const string DirName = "./Data/";
        private static List<File> Files;

        private static List<File> getFilesToSend(Message<FileInfo> message)
        {
            List<File> toSend = new List<File>();

            foreach (File file in Files)
            {
                File? match = message.DATA.Files.Find(x => x.Name.Equals(file.Name) && x.Directory.Equals(file.Directory) && x.Hash.Equals(file.Hash));
                if (match == null)
                {
                    toSend.Add(file);
                }
            }

            return toSend;
        }

        private static List<File> LoadData()
        {
            var Files = new List<File>();

            var files = Directory.GetFiles(DirName, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                using (var sha512 = SHA512.Create())
                {
                    using (var stream = System.IO.File.OpenRead(file))
                    {
                        string fileName = file.Replace(DirName, "./").Replace("\\", "/");
                        Files.Add(new File(Path.GetFileName(fileName), Path.GetDirectoryName(fileName), BitConverter.ToString(sha512.ComputeHash(stream)).Replace("-", "").ToLower()));
                    }
                }
            }
            return Files;
        }

        private static void Main()
        {
            Console.WriteLine("Starting to Updateserver on IP: 81.169.188.220!");

            var Server = new TcpListener(42069); // IPAddress.Parse("81.169.188.220"), 

            Server.Start();

            Console.WriteLine("Server started!");

            Console.WriteLine("Getting Files!");

            Files = LoadData();
           
            while (true)
            {
                try
                {
                    var Client = Server.AcceptTcpClient();

                    Console.WriteLine("Client connected!");

                    new Thread(new ThreadStart(() => { newClient(Client); })).Start(); 
                }
                catch { }
            }
        }
        private static bool Send<T>(Stream stream, Message<T> message)
        {
            var json = message.ToJson();
            var data = Encoding.UTF8.GetBytes(json);

            try
            {
                stream.Write(BitConverter.GetBytes(data.Length));
                stream.Write(data, 0, data.Length);
            }
            catch { return false; }
            return true;
        }
        private static Message<T>? Recieve<T>(Stream stream)
        {
            var data = new byte[4];
            if (stream.Read(data, 0, 4) == -1) return null;
            var length = BitConverter.ToInt32(data, 0);

            data = new byte[length];
            int read = 0;

            while (read != length)
            {
                int r = stream.Read(data, read, length - read);
                if (r == -1) return null;
                read += r;

            }

            var json = Encoding.UTF8.GetString(data);
            return Message<T>.FromJson(json);
        }
        private static void newClient(TcpClient client)
        {
            try
            {
                var Stream = client.GetStream();

                var Message = Recieve<FileInfo>(Stream);

                if (Message == null) return;

                var FilesToSend = getFilesToSend(Message);

                if(!Send(Stream, new Message<FileInfo>(MessageId.FileInfo, new FileInfo(FilesToSend)))) return;

                while (true)
                {
                    var FileMessage = Recieve<File>(Stream);

                    if (FileMessage == null) return;

                    if (FileMessage.ID == MessageId.GetFile)
                    {
                        byte[] data = System.IO.File.ReadAllBytes(Path.Combine(DirName, FileMessage.DATA.Directory, FileMessage.DATA.Name));

                        if (!Send(Stream, new Message<byte[]>(MessageId.GetFile, data))) return;
                    }
                }

                Console.WriteLine("Response sent!");

                Stream.Close();

                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
    }
}