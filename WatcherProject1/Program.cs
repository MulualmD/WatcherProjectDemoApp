using Octokit;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WatcherProject1
{
   public  class Program
    {

        static void Main(string[] args)

        {
            var pathDemoApp = ConfigurationManager.AppSettings["pathDemoApp"].Split(',');

            for (int i = 0; i < pathDemoApp.Length; i++)
            {
                if (Directory.Exists(pathDemoApp[i]))
                {
                    MonitorDirectory(pathDemoApp[i]);
                    break;
                }
            }

        }

        private static void MonitorDirectory(string path)

        {

            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(path);

            fileSystemWatcher.Path = path;

            fileSystemWatcher.NotifyFilter = NotifyFilters.Attributes
                                | NotifyFilters.CreationTime
                                | NotifyFilters.DirectoryName
                                | NotifyFilters.FileName
                                | NotifyFilters.LastAccess
                                | NotifyFilters.LastWrite
                                | NotifyFilters.Security
                                | NotifyFilters.Size;

            fileSystemWatcher.Created += FileSystemWatcher_Created;
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;

            fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;

            fileSystemWatcher.EnableRaisingEvents = true;
            fileSystemWatcher.Filter = "*.cs";
            fileSystemWatcher.IncludeSubdirectories = true;


            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();

        }

        private static string ReadCSFile(string readFilePath)
        {
            string content = System.IO.File.ReadAllText(readFilePath);
            return content;
        }

       
        public static Process MsBuild()
        {
          
            
            var solutionFile = @"D:\DemoApp\DemoApp.sln";
            var MSBuild = @"C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\MSBuild.exe";

            var processBuild = Process.Start(MSBuild, solutionFile);
            processBuild.WaitForExit();
            return processBuild;


        }
        public static Process RunTests()
        {
            string[] nunitConsoles = ConfigurationManager.AppSettings["nunitConsole"].Split(',');
            string[] nunitDLLs = ConfigurationManager.AppSettings["nunitDLL"].Split(',');
            string nunitConsole = string.Empty;
            string nunitDLL = string.Empty;

            foreach (var tmpNunitConsole in nunitConsoles)
            {
                if (File.Exists(tmpNunitConsole))
                {
                    nunitConsole = tmpNunitConsole;
                    break;
                }
            }

            foreach (var tmpnunitDLLs in nunitDLLs)
            {
                if (File.Exists(tmpnunitDLLs))
                {
                    nunitDLL = tmpnunitDLLs;
                    break;
                }
            }
            var processRnnar = Process.Start(nunitConsole, nunitDLL);

            processRnnar.WaitForExit();
            return processRnnar;
        }
        private static async void GetLastPushDateTime()
        {
            var client = new GitHubClient(new ProductHeaderValue("DemoApp"));
            string owner = "mosmo46";
            var repo = "DemoApp";

            var commits = await client.Repository.Commit.Get(owner, repo, "HEAD");

            var lastTimeCommit = commits.Commit.Author.Date.LocalDateTime;

            var date = lastTimeCommit.TimeOfDay;

            Console.WriteLine($"lastTimeCommit=> {date}");
        }
        public static List<string> ReadXmlFile(string path, string readFilePath)
        {
            List<string> ArrayForPath = new List<string>();
            Serializer ser = new Serializer();

            var xmlFilePaths = ConfigurationManager.AppSettings["xmlFilePath"].Split(',');

            foreach (var xmlFilePath in xmlFilePaths)
            {

                if (File.Exists(xmlFilePath))
                {
                    string xmlInputData = File.ReadAllText(xmlFilePath);

                    XmlModel.testrun resFromXml = ser.Deserialize<XmlModel.testrun>(xmlInputData);

                    if (resFromXml.failed == 0)
                    {
                        ArrayForPath.Add(path);
                        ArrayForPath.Add(readFilePath);
                    }
                    else
                    {
                        Console.WriteLine("One or more of the tests do not pass");
                        return null;
                    }

                }
            }
            return ArrayForPath;
        }
        public static async Task<RepositoryContentChangeSet>UplodaToGithub(string path, string readFilePath)
        {
            var ghClient = new GitHubClient(new ProductHeaderValue("DemoApp"));

            ghClient.Credentials = new Credentials("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");
            string owner = "mosmo46";
            var repo = "DemoApp";
            var master = "master";
            try
            {
                var fileDetails = await ghClient.Repository.Content.GetAllContentsByRef(owner, repo,
                                        path, master);  

                string sha = fileDetails.First().Sha;

                RepositoryContentChangeSet updateResult = await ghClient.Repository.Content.UpdateFile(owner, repo, path,
                                         new UpdateFileRequest("My updated file", ReadCSFile(readFilePath), sha));
                return updateResult;
            }
            catch (Octokit.NotFoundException)
            {
                await ghClient.Repository.Content.CreateFile(owner, repo, path, new CreateFileRequest("API File cs creation", "Hello Universe! " + DateTime.UtcNow, master));
            }
            return null;
        }

        private static async void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            MsBuild();
            RunTests();
            GetLastPushDateTime();
            List<string> paths = ReadXmlFile(e.Name, e.FullPath);
            await UplodaToGithub(paths[0], paths[1]);
            Console.WriteLine("File Changed: {0}", e.Name);
        }
        private static void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)

        {
            Console.WriteLine("File created: {0}", e.FullPath);
        }
        private static void FileSystemWatcher_Renamed(object sender, FileSystemEventArgs e)

        {
            Console.WriteLine("File renamed: {0}", e.Name);
        }
        private static void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)

        {
            Console.WriteLine("File deleted: {0}", e.Name);
        }
    }
   
}
