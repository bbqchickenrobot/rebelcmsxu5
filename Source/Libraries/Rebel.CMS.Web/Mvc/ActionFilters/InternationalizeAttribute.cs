﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Mvc;
using Rebel.Cms.Web.Context;
using Rebel.Cms.Web.Model;
using Rebel.Framework.Persistence.Model.Constants;
using Rebel.Hive;
using Rebel.Hive.RepositoryTypes;

namespace Rebel.Cms.Web.Mvc.ActionFilters
{
    using Rebel.Framework;
    using Rebel.Framework.Diagnostics;

    public class InternationalizeAttribute : ActionFilterAttribute
    {
        private IRebelApplicationContext _applicationContext;
        public IRebelApplicationContext ApplicationContext
        {
            get { return _applicationContext ?? (_applicationContext = DependencyResolver.Current.GetService<IRebelApplicationContext>()); }
            set { _applicationContext = value; }
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            using (DisposableTimer.TraceDuration<InternationalizeAttribute>("Checking if should set thread culture automatically", "Done check"))
            {
                if (!filterContext.ActionParameters.ContainsKey("model"))
                {
                    LogHelper.TraceIfEnabled<InternationalizeAttribute>("filterContext.ActionParameters didn't contain model");
                    return;
                }

                var model = filterContext.ActionParameters["model"] as IRebelRenderModel;
                if (model == null)
                {
                    LogHelper.TraceIfEnabled<InternationalizeAttribute>("filterContext.ActionParameters didn't contain model that was IRebelRenderModel");
                    return;
                }
                if (!model.IsLoaded)
                {
                    LogHelper.TraceIfEnabled<InternationalizeAttribute>("filterContext.ActionParameters didn't contained model but it wasn't loaded yet");
                    return;
                }

                var currentNodeId = model.CurrentNode.Id;

                //TODO: Cache this

                using (var uow = ApplicationContext.Hive.OpenReader<IContentStore>())
                {
                    var ancestorIds = uow.Repositories.GetAncestorsIdsOrSelf(currentNodeId,
                                                                             FixedRelationTypes.DefaultRelationType);
                    foreach (var ancestorId in ancestorIds)
                    {
                        var languageRelation =
                            uow.Repositories.GetParentRelations(ancestorId, FixedRelationTypes.LanguageRelationType).
                                SingleOrDefault();

                        if (languageRelation == null)
                            continue;

                        var metaDatum = languageRelation.MetaData.FirstOrDefault(x => x.Key == "IsoCode");
                        if (metaDatum == null) return;
                        var isoCode = metaDatum.Value;

                        LogHelper.TraceIfEnabled<InternationalizeAttribute>("Setting thread culture to {0}", () => isoCode);

                        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(isoCode);
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(isoCode);

                        return;
                    }
                }
            }
        }
    }
}
