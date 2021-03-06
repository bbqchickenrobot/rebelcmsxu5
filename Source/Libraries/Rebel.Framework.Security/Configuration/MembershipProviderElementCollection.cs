﻿using System.Configuration;
using Rebel.Framework.Configuration;

namespace Rebel.Framework.Security.Configuration
{
    [ConfigurationCollection(typeof(MembershipProviderElement))]
    public class MembershipProviderElementCollection : ConfigurationElementCollection<string, MembershipProviderElement>
    {
        public const string CollectionXmlKey = "membershipProviders";

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.AddRemoveClearMap; }
        }

        protected override string ElementName
        {
            get { return CollectionXmlKey; }
        }

        protected override string GetElementKey(MembershipProviderElement element)
        {
            return element.Name;
        }
    }
}
