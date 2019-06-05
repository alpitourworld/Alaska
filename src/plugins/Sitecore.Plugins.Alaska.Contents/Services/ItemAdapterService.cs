﻿using Alaska.Services.Contents.Domain.Models.Items;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Plugins.Alaska.Contents.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.Plugins.Alaska.Contents.Services
{
    internal class ItemAdapterService
    {
        private readonly FieldAdapterService _fieldAdapter = new FieldAdapterService();

        public ContentItem AdaptItem(Item item)
        {
            return AdaptItemWithChildren(item, null);
        }

        public ContentItem AdaptItemWithChildren(Item item, IEnumerable<Item> children)
        {
            return AdaptItemWithDescendantNodes(item, children);
        }

        public ContentItem AdaptItemWithDescendants(Item item, IEnumerable<Item> descendants)
        {
            return AdaptItemWithDescendantNodes(item, descendants);
        }

        private ContentItem AdaptItemWithDescendantNodes(Item item, IEnumerable<Item> descendants)
        {
            return new ContentItem
            {
                Info = GetItemInfo(item),
                Children = GetDirectChildren(item, descendants)?.Select(x => AdaptItemWithDescendantNodes(x, descendants)).ToList(),
                Fields = GetItemFields(item),
            };
        }

        private IEnumerable<Item> GetDirectChildren(Item item, IEnumerable<Item> descendants)
        {
            return descendants?
                .Where(x => x.ParentID == item.ID)
                .ToList();
        }

        private ContentItemFields GetItemFields(Item item)
        {
            var fields = GetValidFields(item);
            return new ContentItemFields(fields.ToDictionary(x => GetNormalizedFieldName(x), x => _fieldAdapter.AdaptField(x)));
        }

        private ContentItemInfo GetItemInfo(Item item)
        {
            return new ContentItemInfo
            {
                Id = item.ID.ToString(),
                TemplateId = item.TemplateID.ToString(),
                Path = GetPathSegments(item.Paths.Path).ToList(),
                IdPath = GetPathSegments(item.Paths.LongID).ToList(),
            };
        }

        private IEnumerable<Field> GetValidFields(Item item)
        {
            return item.Fields
                .Where(x => !IsSystemField(x))
                .ToList();
        }

        private bool IsSystemField(Field field) => field.Name.StartsWith("__");

        private string GetNormalizedFieldName(Field field)
        {
            return StringHelpers.ToCamelCase(field.Name)
                .Replace(" ", string.Empty);
        }

        private IEnumerable<string> GetPathSegments(string path) => path
            .Split('/')
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => x.ToLower())
            .ToList();
    }
}
