﻿using System;
using System.ComponentModel;
using System.Reflection;
using System.Web.Mvc;
using Rebel.Cms.Web.Context;
using Rebel.Cms.Web.Model;
using Rebel.Framework.Diagnostics;
using Rebel.Framework.Dynamics;
using Rebel.Hive;

namespace Rebel.Cms.Web.Macros
{
    public abstract class PartialViewMacroPage : WebViewPage<PartialViewMacroModel>, IRequiresRoutableRequestContext, IRequiresRebelHelper
    {
        /// <summary>
        /// The current routable request context
        /// </summary>
        public IRoutableRequestContext RoutableRequestContext { get; set; }

        /// <summary>
        /// Gets an rebel helper
        /// </summary>
        public RebelHelper Rebel { get; set; }

        /// <summary>
        /// Gets or sets the dynamic model.
        /// </summary>
        /// <value>The dynamic model.</value>
        [Obsolete("Please use CurrentPage as DynamicModel will be removed in Rebel 1.1", false)]
        public dynamic DynamicModel
        {
            get { return CurrentPage; }
            protected set { CurrentPage = value; }
        }

        /// <summary>
        /// Gets or sets the current page.
        /// </summary>
        /// <value>
        /// The current page.
        /// </value>
        public dynamic CurrentPage { get; protected set; }

        /// <summary>
        /// Gets the <see cref="IHiveManager"/> for the application. You can use this to run queries against the data for this application.
        /// </summary>
        /// <value>The hive.</value>
        public IRenderViewHiveManagerWrapper Hive { get { return new RenderViewHiveManagerWrapper(RoutableRequestContext.Application.Hive); } }

        /// <summary>
        /// This will set any publicly declared properties in the markup that match the name of a dynamic property in the dynamic model
        /// </summary>
        /// <param name="viewData"></param>
        protected override void SetViewData(ViewDataDictionary viewData)
        {
            var publicProps = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var partialViewMacroModel = viewData.Model as PartialViewMacroModel;
            if (partialViewMacroModel != null)
            {
                if (partialViewMacroModel.CurrentPage != null)
                    CurrentPage = partialViewMacroModel.CurrentPage.Bend();

                var bendy = (BendyObject) partialViewMacroModel.MacroParameters;
                foreach (var p in publicProps)
                {
                    if (bendy[p.Name] != null)
                    {
                        //yes, we do need to double check null... something to do with how bendy works
                        var obj = bendy[p.Name];
                        if (obj != null)
                        {
                            if (p.PropertyType.IsAssignableFrom(obj.GetType()))
                            {
                                p.SetValue(this, obj, null);
                            }
                            else
                            {
                                var converter = TypeDescriptor.GetConverter(p.PropertyType);
                                //this will generally always be string
                                if (converter != null && converter.CanConvertFrom(obj.GetType()))
                                {
                                    try
                                    {
                                        p.SetValue(this, converter.ConvertFrom(obj), null);
                                    }
                                    catch (Exception ex)
                                    {
                                        //swallowed... this will be a wierd type conversion error which somehow can still happen even though we are checking CanConertFrom!                                    
                                        LogHelper.Warn(
                                            typeof (PartialViewMacroPage),
                                            "Could not set value {0} on partial view macro proeprty named {1} with exception {2}",
                                            obj,
                                            p.Name,
                                            ex.Message);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            base.SetViewData(viewData);
        }
    }
}
