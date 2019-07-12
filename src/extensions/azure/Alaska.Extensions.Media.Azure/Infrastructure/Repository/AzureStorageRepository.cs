﻿using Alaska.Extensions.Media.Azure.Application.Converters;
using Alaska.Extensions.Media.Azure.Infrastructure.Clients;
using Alaska.Extensions.Media.Azure.Infrastructure.Settings;
using Alaska.Services.Contents.Domain.Models.Media;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alaska.Extensions.Media.Azure.Infrastructure.Repository
{
    internal class AzureStorageRepository
    {
        private const string PlaceholderFile = "_.txt";

        private readonly AzureStorageClientFactory _client;
        private readonly MediaFolderConverter _mediaFolderConverter;
        private readonly MediaContentConverter _mediaContentConverter;
        private readonly IOptions<AzureMediaStorageOptions> _storageConfig;

        public AzureStorageRepository(
            AzureStorageClientFactory clientFactory,
            MediaFolderConverter mediaFolderConverter,
            MediaContentConverter mediaContentConverter,
            IOptions<AzureMediaStorageOptions> storageConfig)
        {
            _client = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _mediaFolderConverter = mediaFolderConverter ?? throw new ArgumentNullException(nameof(mediaFolderConverter));
            _mediaContentConverter = mediaContentConverter ?? throw new ArgumentNullException(nameof(mediaContentConverter));
            _storageConfig = storageConfig ?? throw new ArgumentNullException(nameof(storageConfig));
        }

        public async Task<CloudBlobDirectory> CreateDirectory(string name, CloudBlobContainer container)
        {
            var placeholderPath = $"{name}/{PlaceholderFile}";
            var blob = container.GetBlockBlobReference(placeholderPath);
            await blob.UploadTextAsync("_");
            return blob.Parent;
        }

        public async Task<CloudBlobDirectory> CreateDirectory(string name, CloudBlobDirectory parent)
        {
            var placeholderPath = $"{name}/{PlaceholderFile}";
            var blob = parent.GetBlockBlobReference(placeholderPath);
            await blob.UploadTextAsync("_");
            return blob.Parent;
        }

        public CloudBlobDirectory GetDirectoryReference(string id)
        {
            return RootContainerReference().GetDirectoryReference(id);
        }

        public async Task<IEnumerable<MediaFolder>> GetRootContainerDirectories()
        {
            var root = await RootContainer();
            return await GetContainerDirectories(root);
        }

        public async Task<IEnumerable<MediaFolder>> GetChildrenDirectories(CloudBlobDirectory directory)
        {
            var blobs = new List<MediaFolder>();
            BlobContinuationToken continuationToken = null;

            do
            {
                var result = await directory.ListBlobsSegmentedAsync(continuationToken);
                blobs.AddRange(result.Results
                    .Where(x => x is CloudBlobDirectory)
                    .Select(x => _mediaFolderConverter.ConvertToMediaFolder((CloudBlobDirectory)x)));
                continuationToken = result.ContinuationToken;
            }
            while (continuationToken != null);

            return blobs;
        }

        public async Task<IEnumerable<MediaFolder>> GetContainerDirectories(CloudBlobContainer container)
        {
            var blobs = new List<MediaFolder>();
            BlobContinuationToken continuationToken = null;

            do
            {
                var result = await container.ListBlobsSegmentedAsync(continuationToken);
                blobs.AddRange(result.Results
                    .Where(x => x is CloudBlobDirectory)
                    .Select(x => _mediaFolderConverter.ConvertToMediaFolder((CloudBlobDirectory)x)));
                continuationToken = result.ContinuationToken;
            }
            while (continuationToken != null);

            return blobs;
        }
        
        public async Task<CloudBlobContainer> GetContainer(string id)
        {
            var container = _client.CreateBlobClient().GetContainerReference(id);
            await container.CreateIfNotExistsAsync();
            return container;
        }

        public async Task<CloudBlobContainer> RootContainer()
        {
            var root = RootContainerReference();
            await RootContainerReference().CreateIfNotExistsAsync();
            return root;
        }

        private CloudBlobContainer RootContainerReference()
        {
            return _client.CreateBlobClient().GetContainerReference(_storageConfig.Value.RootContainerName);
        }
    }
}
