﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Rebel.Cms.Web.Configuration.Languages;
using Rebel.Cms.Web.Context;
using Rebel.Framework.Persistence;
using Rebel.Framework.Persistence.Model;
using Rebel.Framework.Persistence.Model.Constants;
using Rebel.Framework.Persistence.Model.Constants.AttributeDefinitions;
using Rebel.Framework.Persistence.Model.Constants.Schemas;
using Rebel.Hive.ProviderGrouping;
using Rebel.Hive;
using Rebel.Hive.RepositoryTypes;

namespace Rebel.Cms.Web.Dictionary
{
    public class DictionaryHelper
    {
        private IHiveManager _hiveManager;
        private IEnumerable<LanguageElement> _installedLanguages;

        public DictionaryHelper(IRebelApplicationContext appContext)
            : this(appContext.Hive, appContext.Settings.Languages)
        { }

        public DictionaryHelper(IHiveManager hive, IEnumerable<LanguageElement> installedLanguages)
        {
            _hiveManager = hive;
            _installedLanguages = installedLanguages;
        }

        /// <summary>
        /// Gets a dictionary item.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public DictionaryResult GetDictionaryItem(string key)
        {
            return GetDictionaryItem(key, Thread.CurrentThread.CurrentCulture.Name);
        }

        /// <summary>
        /// Gets a dictionary item.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="language">The language.</param>
        /// <returns></returns>
        public DictionaryResult GetDictionaryItem(string key, string language)
        {
            var languageConfig = _installedLanguages.SingleOrDefault(x => x.IsoCode == language);
            if(languageConfig == null)
                return new DictionaryResult(false, key);

            var hive = _hiveManager.GetReader<IDictionaryStore>();
            using (var uow = hive.CreateReadonly())
            {
                var item = uow.Repositories.GetEntityByPath<TypedEntity>(FixedHiveIds.DictionaryVirtualRoot, key);
                if(item == null)
                    return new DictionaryResult(false, key);

                var val = item.InnerAttribute<string>(DictionaryItemSchema.TranslationsAlias, languageConfig.IsoCode);
                if (!string.IsNullOrWhiteSpace(val))
                    return new DictionaryResult(true, key, val, languageConfig.IsoCode);

                foreach(var fallback in languageConfig.Fallbacks)
                {
                    val = item.InnerAttribute<string>(DictionaryItemSchema.TranslationsAlias, fallback.IsoCode);
                    if (!string.IsNullOrWhiteSpace(val))
                        return new DictionaryResult(true, key, val, fallback.IsoCode);
                }
            }
            return new DictionaryResult(false, key);
        }

        /// <summary>
        /// Gets a dictionary item value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public string GetDictionaryItemValue(string key, string defaultValue = null)
        {
            var result = GetDictionaryItem(key);
            return result.Found ? result.Value : (defaultValue ?? "[" + key +"]");
        }

        /// <summary>
        /// Gets a dictionary item value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="language">The language.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public string GetDictionaryItemValueForLanguage(string key, string language, string defaultValue = null)
        {
            var result = GetDictionaryItem(key, language);
            return result.Found ? result.Value : (defaultValue ?? "[" + key + "]");
        }
    }
}
