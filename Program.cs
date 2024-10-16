using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Reflection;

namespace MigrateDocuments
{
    class Program
    {
        private static ApiConnection _connection;
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

        // Valid call example - MigrateDocuments.exe "Projects,Leads" "Emails"
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException($"Incorrect number of arguments. Expected 2, received {args.Length}");
            }

            string[] folders = args[0].Trim().Trim(',').Split(',');

            if (!Enum.TryParse(args[1].Trim(), true, out ApiConnection.DocumentType documentTypeToSave))
            {
                throw new ArgumentException("Incorrect document type. Document type argument must be 'Documents' or 'Emails'");
            }

            try
            {
                _connection = new ApiConnection(Config.Instance);

                foreach (var folderName in folders)
                {
                    CreateBaseDirectory(folderName);

                    JObject items;

                    if (!string.IsNullOrEmpty(Config.Instance.Connection.AllowBackupField))
                    {
                        JObject additionalFields = new JObject
                        {
                            { Config.Instance.Connection.AllowBackupField, true }
                        };

                        items = _connection.SearchFolder(folderName, JObject.FromObject(new
                        {
                            AdditionalFields = additionalFields
                        }));
                    }
                    else
                    {
                        items = _connection.GetFolder(folderName);
                    }

                    MigrateDocuments(items["Data"], folderName, documentTypeToSave);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }

            Logger.LogDebug("Done.");
            Console.ReadLine();
        }

        static void CreateBaseDirectory(string name)
        {
            _baseDirectory = Path.Combine(AssemblyDirectory, FileHelper.RemoveInvalidFileNameChars(name).Trim());

            Directory.CreateDirectory(_baseDirectory);
        }

        static void MigrateDocuments(JToken items, string folderName, ApiConnection.DocumentType documentType)
        {
            foreach (var item in items)
            {
                Logger.LogDebug($"Downloading {Enum.GetName(typeof(ApiConnection.DocumentType), documentType).ToLower()} for item from folder '{folderName}' with name '{item.Value<string>("FileAs")}'");

                string parentDirectory = Path.Combine(_baseDirectory, FileHelper.RemoveInvalidFileNameChars($"{item.Value<string>("FileAs")} ({item.Value<string>("ItemGUID").Split('-')[0]})").Trim() ?? "INVALID_NAME");
                bool parentDirectoryCreated = false;

                var documents = _connection.GetDocuments(new Guid(item.Value<string>("ItemGUID")), folderName, documentType);
                foreach (var documentGuid in documents)
                {
                    // Avoid creating empty directories
                    if (!parentDirectoryCreated)
                    {
                        Directory.CreateDirectory(parentDirectory);
                        parentDirectoryCreated = true;
                    }

                    switch (documentType)
                    {
                        case ApiConnection.DocumentType.Documents:
                            _connection.DownloadDocument(documentGuid, parentDirectory);
                            break;
                        case ApiConnection.DocumentType.Emails:
                            _connection.DownloadEmail(documentGuid, parentDirectory);
                            break;
                    }
                }
            }
        }
    }
}
