﻿using System;
using System.Configuration;

namespace Rebel.Cms.Web.Configuration.Dashboards
{
    public class DashboardGroupCollection : ConfigurationElementCollection
    {      

        protected override ConfigurationElement CreateNewElement()
        {
            return new DashboardGroupElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            //allow multiple elements, just means you can't reference by a key
            return Guid.NewGuid();
        }

    }
}