using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using eWayCRM.API;
using MigrateDocuments.Configuration;

namespace MigrateDocuments
{
    class ApiConnection
    {
        public enum DocumentType
        {
            Documents,
            Emails
        }

        private readonly Connection connection;

        public ApiConnection(RootConfigSection config)
        {
            string[] appVersionElements = config.AssemblyVersion.ToString().Split('.');

            connection = new Connection(
                config.Connection.Url,
                config.Connection.UserName,
                config.Connection.Password,
                $"{config.AssemblyName}_v{appVersionElements[0]}.{appVersionElements[1]}"
            );

            connection.EnsureLogin();
        }

        public JObject GetFolder(string folderName)
        {
            return connection.CallMethod($"Get{folderName}");
        }

        public JObject SearchFolder(string folderName, JObject transmitObject)
        {
            return connection.CallMethod($"Search{folderName}", JObject.FromObject(new
            {
                transmitObject
            }));
        }

        public IEnumerable<Guid> GetDocuments(Guid itemGuid, string folderName, DocumentType documentType)
        {
            return connection.GetItemsByItemGuids($"Get{folderName}ByItemGuids", new Guid[] { itemGuid }, false, true)
                .Single()["Relations"]
                .Where(x => x.Value<string>("ForeignFolderName") == Enum.GetName(typeof(DocumentType), documentType))
                .Select(x => new Guid(x.Value<string>("ForeignItemGUID")))
                .ToArray();
        }

        public void DownloadDocument(Guid documentGuid, string directory)
        {
            var revisionInfo = connection.GetLatestRevision(documentGuid)["Datum"];
            string documentName = revisionInfo.Value<string>("FileAs");
            string filePath = Path.Combine(directory, documentName);

            DownloadFile(documentGuid, filePath, directory);
        }

        public void DownloadEmail(Guid emailGuid, string directory)
        {
            var email = SearchFolder("Emails", JObject.FromObject(new { ItemGUID = emailGuid }));
            string emailName = email.Value<JArray>("Data").First().Value<string>("FileAs");
            string emailExtension = email.Value<JArray>("Data").First().Value<string>("EmailFileExtension");
            string filePath = Path.Combine(directory, $"{RemoveInvalidFileNameChars(emailName)}{emailExtension}");

            DownloadFile(emailGuid, filePath, directory);
        }

        private void DownloadFile(Guid fileGuid, string filePath, string directory)
        {
            if (File.Exists(directory))
            {
                Logger.LogDebug($"File '{directory}' already exist");
                return;
            }

            try
            {
                Logger.LogDebug($"Downloading file '{filePath}' to '{directory}'");

                using (var fileStream = File.Create(filePath))
                {
                    connection.DownloadFile(fileGuid, 1, fileStream);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unable to download email '{filePath}' to '{directory}'");

                if (ex is WebException || ex is IOException)
                {
                    Logger.LogDebug("Trying again in 15 seconds");
                    System.Threading.Thread.Sleep(15000);

                    if (File.Exists(directory))
                    {
                        File.Delete(directory);
                    }

                    DownloadFile(fileGuid, filePath, directory);
                }
            }
        }
   
        private string RemoveInvalidFileNameChars(string filename)
        {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
}