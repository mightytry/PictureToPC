using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Main
{
    internal class Messages
    {
        public class Message<T>
        {
            public MessageId ID;
            public T DATA;

            public Message(MessageId id, T data)
            {
                ID = id;
                DATA = data;
            }

            public static Message<T> FromJson(string json)
            {
                return JsonConvert.DeserializeObject<Message<T>>(json);
            }

            public string ToJson()
            {
                return JsonConvert.SerializeObject(this);
            }
        }
        public class FileInfo
        {
            public List<File> Files;

            public FileInfo(List<File> files)
            {
                Files = files;
            }
        }

        public class File
        {
            public string Name;
            public string Directory;
            public string Hash;

            public byte[]? Data;

            public File(string location, string directory,string hash, byte[]? data = null)
            {
                Name = location;
                Directory = directory;
                Hash = hash;
                Data = data;
            }
        }

        public enum MessageId
        {
            FileInfo,
            GetFile,
            End
        }
    } 
}
