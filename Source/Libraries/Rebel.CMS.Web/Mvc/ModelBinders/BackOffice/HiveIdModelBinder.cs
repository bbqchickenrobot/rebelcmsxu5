﻿using System;
using System.Web.Mvc;
using Rebel.Cms.Web.DependencyManagement;
using Rebel.Framework;

namespace Rebel.Cms.Web.Mvc.ModelBinders.BackOffice
{
    /// <summary>
    /// Model binder used to bind the HiveId parameter
    /// </summary>
    [ModelBinderFor(typeof(HiveId))]
    public class HiveIdModelBinder : DefaultModelBinder
    {
        /// <summary>
        /// Binds the model
        /// </summary>
        /// <param name="controllerContext"></param>
        /// <param name="bindingContext"></param>
        /// <returns></returns>
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            return PerformBindModel(controllerContext, bindingContext);
        }

        public static object PerformBindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var idName = string.IsNullOrEmpty(bindingContext.ModelName) ? "id" : bindingContext.ModelName;


            var valueProviderResult = bindingContext.GetValue(idName);
            if (valueProviderResult == null)
            {
                return null;
            }

            var rawId = valueProviderResult.ConvertTo(typeof(string)) as string;

            var parseResult = HiveId.TryParse(rawId);
            if (parseResult.Success)
            {
                //add the bound value to model state if it's not already there, generally only simple props will be there
                if (!bindingContext.ModelState.ContainsKey(idName))
                {
                    bindingContext.ModelState.Add(idName, new ModelState());
                    bindingContext.ModelState.SetModelValue(idName, new ValueProviderResult(parseResult.Result, parseResult.Result.ToString(), null));
                }
            }

            return parseResult.Result;
        }

    }
}