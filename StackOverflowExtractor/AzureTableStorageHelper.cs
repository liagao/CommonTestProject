namespace StackOverflowExtractor
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    public static class AzureTableStorageHelper
    {
        private static CloudStorageAccount storageAccount;
        private static CloudTableClient tableClient;
        private static CloudTable perfAnalyzerRecordTable;

        internal static void Initialize()
        {
            storageAccount = CloudStorageAccount.Parse(Resources.StorageConnectionString);
            tableClient = storageAccount.CreateCloudTableClient();
            perfAnalyzerRecordTable = tableClient.GetTableReference(Resources.XapResourceTable);
        }

        internal static CloudStorageAccount GetCloudStorageAccount()
        {
            return storageAccount ?? CloudStorageAccount.Parse(Resources.StorageConnectionString);
        }

        internal static CloudTableClient GetTableClient()
        {
            return tableClient ?? GetCloudStorageAccount().CreateCloudTableClient();
        }

        internal static CloudTable GetCloudTable(string tableName)
        {
            return GetTableClient().GetTableReference(tableName);
        }

        internal static void InsertTable(IList<SearchResultItem> records)
        {
            try
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (var record in records)
                {
                    if (record != null)
                    {
                        batchOperation.Insert(record);
                    }
                }

                perfAnalyzerRecordTable.ExecuteBatchAsync(batchOperation);
            }
            catch (Exception ex)
            {
                File.AppendAllLines("log.txt", new[] { string.Format("Record operation failed with exception: {0}", ex) });
            }
        }
    }
}