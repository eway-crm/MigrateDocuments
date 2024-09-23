using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Reflection;

namespace MigrateDocuments
{
    class Program
    {
        enum DocumentType
        {
            Documents,
            Emails
        }

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

            if (!Enum.TryParse(args[1].Trim(), true, out DocumentType documentTypeToSave))
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

                    if (!string.IsNullOrEmpty(Config.Instance.Connection.allowBackupField))
                    {
                        JObject additionalFields = new JObject
                        {
                            { Config.Instance.Connection.allowBackupField, true }
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

                    if (documentTypeToSave == DocumentType.Documents)
                    {
                        MigrateDocuments(items["Data"], folderName);
                    }
                    else if (documentTypeToSave == DocumentType.Emails)
                    {
                        MigrateEmails(items["Data"], folderName);
                    }
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
            _baseDirectory = Path.Combine(AssemblyDirectory, name);

            Directory.CreateDirectory(_baseDirectory);
        }

        static void MigrateDocuments(JToken items, string folderName)
        {
            foreach (var item in items)
            {
                Logger.LogDebug($"Downloading documents for item from folder '{folderName}' with name '{item.Value<string>("FileAs")}'");

                string parentDirectory = Path.Combine(_baseDirectory, item.Value<string>("FileAs") ?? "INVALID_NAME");
                bool parentDirectoryCreated = false;

                var documents = _connection.GetDocuments(new Guid(item.Value<string>("ItemGUID")), folderName);
                foreach (var documentGuid in documents)
                {
                    // Avoid creating empty directories
                    if (!parentDirectoryCreated)
                    {
                        Directory.CreateDirectory(parentDirectory);
                        parentDirectoryCreated = true;
                    }
                    _connection.DownloadDocument(documentGuid, parentDirectory);
                }
            }
        }

        static void MigrateEmails(JToken items, string folderName)
        {
            foreach (var item in items)
            {
                Logger.LogDebug($"Downloading emails for item from folder '{folderName}' with name '{item.Value<string>("FileAs")}'");

                string parentDirectory = Path.Combine(_baseDirectory, item.Value<string>("FileAs") ?? "INVALID_NAME");
                bool parentDirectoryCreated = false;

                var documents = _connection.GetEmails(new Guid(item.Value<string>("ItemGUID")), folderName);
                foreach (var documentGuid in documents)
                {
                    // Avoid creating empty directories
                    if (!parentDirectoryCreated)
                    {
                        Directory.CreateDirectory(parentDirectory);
                        parentDirectoryCreated = true;
                    }
                    _connection.DownloadEmail(documentGuid, parentDirectory);
                }
            }
        }
    }
}
