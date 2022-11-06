using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Security.Principal;

namespace Main
{
    public class Commit
    {
        public string sha { get; set; }
        public string url { get; set; }
    }

    public class Root
    {
        public string name { get; set; }
        public string zipball_url { get; set; }
        public string tarball_url { get; set; }
        public Commit commit { get; set; }
        public string node_id { get; set; }
    }




    internal static class Program 
    {
        private static string _versionId = null;
        private static bool? _isRunningAsAdmin = null;

        private static readonly HttpClient client = new HttpClient();

        public const string Author = "mightytry";
        public const string Repository = "PictureToPC";


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
            if (_isRunningAsAdmin == false)
            {
                ExecuteAsAdmin("Main.exe");
                Environment.Exit(0);
            }
        }
        private static void Get_Version()
        {
            try
            {
                if (_versionId == null)
                {
                    _versionId = File.ReadAllText("version");
                }
            }

            catch (Exception)
            {
                _versionId = "0";
                File.Create("version").Close();
            }
        }

        private static string Get_Online_Version(string url)
        {
#if !DEBUG
            try
            {
#endif  
            string response = client.GetStringAsync(url).Result;
            Root[] myDeserializedClass = JsonConvert.DeserializeObject<Root[]>(response);
            return myDeserializedClass[0].name;
#if !DEBUG
            }
            catch (Exception)
            {
                return "0";
            }
#endif
        }

        private static void Get_Zip(string url, string path)
        {
#if !DEBUG
            try
            {
#endif

            Console.WriteLine(url);
            client.GetStreamAsync(url).ContinueWith((task) =>
            {
                using (var stream = task.Result)
                {
                    using (var archive = new ZipArchive(stream))
                    {
                        archive.ExtractToDirectory(path);
                    }
                }
            }).Wait();
#if !DEBUG
            }
            catch (Exception)
            {
                return;
            }
#endif
        }
        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        private static void Main()
        {
            client.DefaultRequestHeaders.Add("User-Agent", Repository);


            //url = https://github.com/mightytry/PictureToPC/releases/latest/download/Exe.zip

            if (Directory.Exists("Update"))
            {
                 CopyFilesRecursively("Update", "Exe");
                 Directory.Delete("Update", true);
            }

            Get_Version();

            if (_versionId != "0")
            {
                Process.Start("Exe\\PictureToPC.exe");
            }

            string onlineVersion = Get_Online_Version($"https://api.github.com/repos/{Author}/{Repository}/tags");

            Console.WriteLine($"Online Version: {onlineVersion}");

            if (onlineVersion != _versionId)
            {
                CheckPermission();

                if (_versionId == "0")
                {
                    Get_Zip($"https://github.com/{Author}/{Repository}/releases/download/v0.0.0/x64.zip", "Update");
                }

                Get_Zip($"https://github.com/{Author}/{Repository}/releases/latest/download/Exe.zip", "Update");

                if (_versionId == "0")
                {
                    CopyFilesRecursively("Update", "Exe");
                    Process.Start("Exe\\PictureToPC.exe");
                    Directory.Delete("Update", true);
                }

                File.WriteAllText("version", onlineVersion);

            }

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