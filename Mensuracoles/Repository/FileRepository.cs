using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mensuracoles.Repository
{
    public class FileRepository
    {
        private string _fileName;
        private Dictionary<int, UserMeasurement> InMemory = new Dictionary<int, UserMeasurement>();
        public FileRepository(string fileName)
        {

            _fileName = fileName;
            CreateFileIfNotExist(_fileName);
            GetMessagesFromFile().ForEach(x => InMemory.TryAdd(x.MessageId, x));
        }
        private void SaveContentToFile(string content)
        {
            var existingContent = this.GetFileContent();
            var allContent = existingContent += content;
            File.WriteAllText(_fileName, content);
        }

        private void CreateFileIfNotExist(string filePath)
        {
            if (!File.Exists(filePath))
            {
                var fs = File.Create(filePath);
                fs.Close();
            }
        }
        private string GetFileContent()
        {
            if (!File.Exists(_fileName))
            {
                var fs = File.Create(_fileName);
                fs.Close();
                return "";
            }
            return File.ReadAllText(_fileName);
        }

        public void SaveMessagesToFile(List<UserMeasurement> messages, bool replace = false)
        {
            try
            {
                if (replace)
                {
                    var replaceInMemory = new Dictionary<int, UserMeasurement>();
                    InMemory = replaceInMemory;
                }
                messages.ForEach(x => InMemory.TryAdd(x.MessageId, x));

                var formatting = Newtonsoft.Json.Formatting.Indented;
                var seriaizedTable = Newtonsoft.Json.JsonConvert.SerializeObject(GetMessages(), formatting);
                CreateFileIfNotExist(_fileName);
                File.WriteAllText(_fileName, seriaizedTable);
                
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e);
            }
        }

        public List<UserMeasurement> GetMessages()
        {
            return InMemory.Select(x => x.Value).ToList();
        }

        private List<UserMeasurement> GetMessagesFromFile()
        {
            CreateFileIfNotExist(_fileName);

            var messagesJson = File.ReadAllText(_fileName);
            var loadedMessages = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UserMeasurement>>(messagesJson);
            if (loadedMessages == null)
            {
                loadedMessages = new List<UserMeasurement>();
            }
            return loadedMessages;
        }
    }
}
