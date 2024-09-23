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

        public IEnumerable<Guid> GetDocuments(Guid itemGuid, string folderName)
        {
            return connection.GetItemsByItemGuids($"Get{folderName}ByItemGuids", new Guid[] { itemGuid }, false, true)
                .Single()["Relations"]
                .Where(x => x.Value<string>("ForeignFolderName") == "Documents")
                .Select(x => new Guid(x.Value<string>("ForeignItemGUID")))
                .ToArray();
        }

        public void DownloadDocument(Guid documentGuid, string directory)
        {
            var revisionInfo = connection.GetLatestRevision(documentGuid)["Datum"];
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
                    connection.DownloadFile(documentGuid, revision, fileStream);
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

        public IEnumerable<Guid> GetEmails(Guid itemGuid, string folderName)
        {
            return connection.GetItemsByItemGuids($"Get{folderName}ByItemGuids", new Guid[] { itemGuid }, false, true)
                .Single()["Relations"]
                .Where(x => x.Value<string>("ForeignFolderName") == "Emails")
                .Select(x => new Guid(x.Value<string>("ForeignItemGUID")))
                .ToArray();
        }

        public void DownloadEmail(Guid emailGuid, string directory)
        {
            var email = SearchFolder("Emails", JObject.FromObject(new { ItemGUID = emailGuid }));
            string emailName = email.Value<JArray>("Data").First().Value<string>("FileAs");
            string emailExtension = email.Value<JArray>("Data").First().Value<string>("EmailFileExtension");
            string filePath = Path.Combine(directory, $"{emailName}{emailExtension}");

            if (File.Exists(filePath))
            {
                Logger.LogDebug($"Email '{filePath}' already exists");
                return;
            }

            try
            {
                Logger.LogDebug($"Downloading email '{emailName}' to '{filePath}'");

                using (var fileStream = File.Create(filePath))
                {
                    connection.DownloadFile(emailGuid, 1, fileStream);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Unable to download email '{emailName}' to '{filePath}'");

                if (ex is WebException || ex is IOException)
                {
                    Logger.LogDebug("Trying again in 15 seconds");
                    System.Threading.Thread.Sleep(15000);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    DownloadEmail(emailGuid, directory);
                }
            }
        }
    }
}