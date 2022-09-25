using Newtonsoft.Json;

namespace PictureToPC
{
    internal class Config
    {
        public static string FolderName = "Mees Studio";
        public static string FileName = "ImageToPC.json";

        public static string FilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + FolderName + "\\" + FileName;
        public Data Data;
        internal Config()
        {
            check();
            load();
        }

        private void load()
        {
            Data = JsonConvert.DeserializeObject<Data>(File.ReadAllText(FilePath));
        }

        public void Save()
        {
            save();
        }

        private void check()
        {
            //check if file and folder exist
            if (!File.Exists(FilePath))
            {
                _ = Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + FolderName);
                File.Create(FilePath).Close();
                Data = new Data();
                save();
            }
        }

        private void save()
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(Data));
        }
    }

    internal class Data
    {
        public int InternalResulutionIndex;
        public int OutputResulutionIndex;
        internal Data(int iI, int oI)
        {
            InternalResulutionIndex = iI;
            OutputResulutionIndex = oI;
        }
        internal Data()
        {
            InternalResulutionIndex = 0;
            OutputResulutionIndex = 0;
        }
    }
}

