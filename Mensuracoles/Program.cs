using Mensuracoles.Repository;
using System;
using System.IO;

namespace Mensuracoles
{
    class Program
    {
        static void Main(string[] args)
        {
            var token = Environment.GetEnvironmentVariable("BotToken");
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Token cant be null or empty, please set BotToken env variable");
                return;
            }
            Console.WriteLine("starting bot!");
            var basepath2 = Directory.GetCurrentDirectory();
            var basepath = Environment.GetEnvironmentVariable("basepath") ?? basepath2;

            var messagesFilePath = Path.Combine(basepath, "measurments", "measurments.json");
            if (!Directory.Exists(Path.Combine(basepath, "measurments")))
            {
                Directory.CreateDirectory(Path.Combine(basepath, "measurments"));
            }
            System.Console.WriteLine("Saving messages to " + messagesFilePath);
            var repository = new FileRepository(messagesFilePath);

            var myBotListner = new ColesHandler(token, repository);

            Console.WriteLine("ending bot");
        }
    }
}
