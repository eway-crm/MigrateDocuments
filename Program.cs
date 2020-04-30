using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

                MigrateDocuments(leads["Data"]);
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

        static void MigrateDocuments(JToken leads)
        {
            foreach (var lead in leads)
            {
                Logger.LogDebug($"Downloading documents for lead '{lead.Value<string>("HID")}'");

                string leadDirectory = Path.Combine(_baseDirectory, lead.Value<string>("HID"));
                Directory.CreateDirectory(leadDirectory);

                var documents = GetDocuments(new Guid(lead.Value<string>("ItemGUID")));
                foreach (var documentGuid in documents)
                {
                    DownloadDocument(documentGuid, leadDirectory);
                }
            }
        }

        static IEnumerable<Guid> GetDocuments(Guid leadGuid)
        {
            return _connection.GetItemsByItemGuids($"GetLeadsByItemGuids", new Guid[] { leadGuid }, false, true)
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
            }
        }
    }
}
