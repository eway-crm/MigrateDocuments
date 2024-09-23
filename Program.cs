using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace MigrateDocuments
{
    class Program
    {
        private static eWayCRM.API.Connection _connection;
        private static string _baseDirectory;

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        static void Main(string[] args)
        {
            try
            {
                CreateBaseDirectory();

                _connection = new eWayCRM.API.Connection(Config.Instance.Connection.Url, Config.Instance.Connection.UserName, Config.Instance.Connection.Password);
                _connection.EnsureLogin();

                JObject additionalFields = new JObject();
                additionalFields.Add("af_559", true);

                JObject leads = _connection.CallMethod("SearchLeads", JObject.FromObject(new
                {
                    transmitObject = new
                    {
                        AdditionalFields = additionalFields
                    }
                }));

                MigrateDocuments(leads["Data"], "Leads");

                additionalFields = new JObject();
                additionalFields.Add("af_560", true);

                JObject projects = _connection.CallMethod("SearchProjects", JObject.FromObject(new
                {
                    transmitObject = new
                    {
                        AdditionalFields = additionalFields
                    }
                }));

                MigrateDocuments(projects["Data"], "Projects");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            Logger.LogDebug("Done.");
            Console.ReadLine();
        }

        static void CreateBaseDirectory()
        {
            _baseDirectory = Path.Combine(AssemblyDirectory, "Documents");

            Directory.CreateDirectory(_baseDirectory);
        }

        static void MigrateDocuments(JToken items, string folderName)
        {
            foreach (var item in items)
            {
                Logger.LogDebug($"Downloading documents for item from folder '{folderName}' with ID '{item.Value<string>("HID")}'");

                string parentDirectory = Path.Combine(_baseDirectory, item.Value<string>("HID"));
                Directory.CreateDirectory(parentDirectory);

                var documents = GetDocuments(new Guid(item.Value<string>("ItemGUID")), folderName);
                foreach (var documentGuid in documents)
                {
                    DownloadDocument(documentGuid, parentDirectory);
                }
            }
        }

        static IEnumerable<Guid> GetDocuments(Guid itemGuid, string folderName)
        {
            return _connection.GetItemsByItemGuids($"Get{folderName}ByItemGuids", new Guid[] { itemGuid }, false, true)
                .Single()["Relations"]
                .Where(x => x.Value<string>("ForeignFolderName") == "Documents")
                .Select(x => new Guid(x.Value<string>("ForeignItemGUID")))
                .ToArray();
        }

        static void DownloadDocument(Guid documentGuid, string directory)
        {
            var revisionInfo = _connection.GetLatestRevision(documentGuid)["Datum"];
            string documentName = revisionInfo.Value<string>("FileAs");
            string filePath = Path.Combine(directory, documentName);

            if (File.Exists(filePath))
            {
                Logger.LogDebug($"File '{filePath}' already exist");
                return;
            }

            int revision = revisionInfo.Value<int>("Revision");

            try
            {
                Logger.LogDebug($"Downloading file '{documentName}' to '{filePath}'");

                using (var fileStream = File.Create(filePath))
                {
                    _connection.DownloadFile(documentGuid, revision, fileStream);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unable to download file '{documentName}' to '{filePath}'");

                if (ex is WebException || ex is IOException)
                {
                    Logger.LogDebug("Trying again in 15 seconds");
                    System.Threading.Thread.Sleep(15000);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    DownloadDocument(documentGuid, directory);
                }
            }
        }
    }
}
