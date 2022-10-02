using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using static Main.Messages;
using File = Main.Messages.File;
using FileInfo = Main.Messages.FileInfo;

namespace Main
{
    internal static class Program
    {
        private const string DirName = "Update";

        private static bool? _isRunningAsAdmin = null;

        private static bool _startOnFinish = false;

        private static List<File> LoadData()
        {
            var Files = new List<File>();

            var files = Directory.GetFiles("./", "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                using (var sha512 = SHA512.Create())
                {
                    using (var stream = System.IO.File.OpenRead(file))
                    {
                        string fileName = file.Replace("\\", "/");
                        Files.Add(new File(Path.GetFileName(fileName), Path.GetDirectoryName(fileName).Replace("\\", "/"), BitConverter.ToString(sha512.ComputeHash(stream)).Replace("-", "").ToLower()));
                    }
                }
            }
            return Files;
        }

        private static void Send<T>(Stream stream, Message<T> message)
        {
            var json = message.ToJson();
            var data = Encoding.UTF8.GetBytes(json);

            try {
                stream.Write(BitConverter.GetBytes(data.Length));
                stream.Write(data, 0, data.Length);
            }
            catch { Exit(); }
        }

        private static byte[] GetFileData(Stream stream, File file)
        {
            Send(stream, new Message<File>(MessageId.GetFile, file));

            Message<byte[]> message = Recieve<byte[]>(stream);

            return message.DATA;
        }
        
        private static Message<T> Recieve<T>(Stream stream)
        {
            var data = new byte[4];
            if (stream.Read(data, 0, 4) == -1) Exit();
            var length = BitConverter.ToInt32(data, 0);

            data = new byte[length];
            int read = 0;
            
            while (read != length)
            {
                int r = stream.Read(data, read, length - read);
                if (r == -1) Exit();
                read += r;
                
            }

            var json = Encoding.UTF8.GetString(data);
            return Message<T>.FromJson(json);
        }
        private static void checkIsUpdateEmpty()
        {
#if !DEBUG
            try
            {
#endif
                string[] files = Directory.GetFiles(DirName, "*", SearchOption.AllDirectories);

                if (files.Length == 0) return;

                foreach (var file in files)
                {
#if !DEBUG
                    try
                    {
#endif
                    CheckPermission();

                        string newPos = file.Replace(DirName, ".");

                        if (!Directory.Exists(Path.GetDirectoryName(newPos)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(newPos));
                        }

                        if (System.IO.File.Exists(newPos))
                        {
                            System.IO.File.Delete(newPos);
                        }


                        System.IO.File.Move(file, newPos);
#if !DEBUG
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
#endif
                }
#if !DEBUG
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
#endif
        }
        private static void CheckPermission()
        {
            if (_isRunningAsAdmin == null)
            {
                if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                {
                    _isRunningAsAdmin = false;
                }
                else
                {
                    _isRunningAsAdmin = true;
                }
            }
            else if (_isRunningAsAdmin == false)
            {
                ExecuteAsAdmin("Main.exe");
                Environment.Exit(0);
            }
        }
            

        private static void Main()
        {
#if !DEBUG
            try
            {
#endif
                CheckPermission();
                checkIsUpdateEmpty();

                try
                {
                    Process.Start("PictureToPC.exe");
                    _startOnFinish = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    _startOnFinish = true;
                }
                

                Console.WriteLine("Starting Updater");

                var Client = new TcpClient("meesstudio.ddns.net", 42069);

                Console.WriteLine("Connected to server");

                var Stream = Client.GetStream();

                var Message = new Message<FileInfo>(MessageId.FileInfo, new FileInfo(LoadData()));

                Send(Stream, Message);

                Console.WriteLine("Sent file info");


                var Response = Recieve<FileInfo>(Stream).DATA;

                if (Response.Files.Count == 0) Exit();

                CheckPermission();

                foreach (File file in Response.Files)
                {
                    byte[] data = GetFileData(Stream, file);

                    Console.WriteLine($"Recieved file {file.Name} in {file.Directory}");

                    if (file.Directory != "./" && !Directory.Exists(Path.Combine(DirName, file.Directory)))
                    {
                        Directory.CreateDirectory(Path.Combine(DirName, file.Directory));
                    }
                    
                    if (file.Name.Equals(string.Empty)) continue;

                    System.IO.File.WriteAllBytes(Path.Combine(DirName, file.Directory, file.Name), data);
                }
                try
                {
                    if (_startOnFinish)
                    {
                        Process.Start("Main.exe");
                    }
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
#if !DEBUG
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
#endif
            Exit();


        }

        public static void Exit()
        {
            Environment.Exit(0);
        }


        public static void ExecuteAsAdmin(string fileName)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.Verb = "runas";
            proc.Start();
        }
    }
}