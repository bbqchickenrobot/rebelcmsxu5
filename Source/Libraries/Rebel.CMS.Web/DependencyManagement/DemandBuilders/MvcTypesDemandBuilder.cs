﻿using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;
using Rebel.Cms.Web.Context;
using Rebel.Cms.Web.EmbeddedViewEngine;
using Rebel.Cms.Web.Mvc.Areas;
using Rebel.Cms.Web.Mvc.ControllerFactories;
using Rebel.Cms.Web.Mvc.Metadata;
using Rebel.Cms.Web.Mvc.ViewEngines;
using Rebel.Cms.Web.Routing;
using Rebel.Cms.Web.Surface;
using Rebel.Cms.Web.System;
using Rebel.Cms.Web.Trees;
using Rebel.Framework;
using Rebel.Framework.DependencyManagement;

namespace Rebel.Cms.Web.DependencyManagement.DemandBuilders
{
    /// <summary>
    /// registers the MVC types into the container
    /// </summary>
    public class MvcTypesDemandBuilder : IDependencyDemandBuilder
    {
        private readonly TypeFinder _typeFinder;

        public MvcTypesDemandBuilder(TypeFinder typeFinder)
        {
            _typeFinder = typeFinder;
        }

        public void Build(IContainerBuilder containerBuilder, IBuilderContext context)
        {
            //register model binder provider
            containerBuilder.RegisterModelBinderProvider();

            // Register model binders & controllers
            var allReferencedPluginAssemblies = PluginManager.ReferencedPlugins.Select(x => x.ReferencedAssembly).ToArray();
            containerBuilder
                .RegisterModelBinders(allReferencedPluginAssemblies, _typeFinder)
                .RegisterModelBinders(TypeFinder.GetFilteredBinFolderAssemblies(allReferencedPluginAssemblies), _typeFinder)
                .RegisterControllers(allReferencedPluginAssemblies, _typeFinder)
                .RegisterControllers(TypeFinder.GetFilteredBinFolderAssemblies(allReferencedPluginAssemblies), _typeFinder);

            //register view engines
            containerBuilder.For<EmbeddedRazorViewEngine>().KnownAs<IViewEngine>();
            containerBuilder.For<PluginViewEngine>().KnownAs<IViewEngine>();
            containerBuilder.For<RenderViewEngine>().KnownAs<IViewEngine>();
            containerBuilder.For<AlternateLocationViewEngine>().KnownAs<IViewEngine>();

            //register the route handlers
            containerBuilder.For<RenderRouteHandler>()
                .Named<IRouteHandler>(RenderRouteHandler.SingletonServiceName)
                .ScopedAs.NewInstanceEachTime();

            // register our master controller factory and other IFilteredControllerFactories
            containerBuilder.For<MasterControllerFactory>().KnownAs<IControllerFactory>().ScopedAs.Singleton();
            containerBuilder.For<RenderControllerFactory>().KnownAs<IFilteredControllerFactory>().ScopedAs.Singleton();
            containerBuilder.For<PluginControllerFactory>().KnownAs<IFilteredControllerFactory>().ScopedAs.Singleton();

            //register our rebel area, ensure that the TreeRouteHandler named service is injected for 
            //the constructor argument expecting a IRouteHandler
            containerBuilder.For<RebelAreaRegistration>().KnownAsSelf();
            containerBuilder.For<InstallAreaRegistration>().KnownAsSelf();

            //register master view page activator
            containerBuilder.For<MasterViewPageActivator>().KnownAs<IViewPageActivator>().ScopedAs.Singleton();
            //register our IPostViewPageActivators
            containerBuilder.For<RebelContextViewPageActivator>().KnownAs<IPostViewPageActivator>().ScopedAs.Singleton();
            containerBuilder.For<RebelHelperViewPageActivator>().KnownAs<IPostViewPageActivator>().ScopedAs.Singleton();
            containerBuilder.For<RenderViewPageActivator>().KnownAs<IPostViewPageActivator>().ScopedAs.Singleton();

            //register model meta data provider
            containerBuilder.For<RebelModelMetadataProvider>().KnownAs<ModelMetadataProvider>();

            ////register the default controller factory
            ////(We can't put this in the container based on the DependencyResolver because that will cause
            ////an infinite loop. Also, trying to use the Singly registered 'old school' way to get the
            ////ControllerFactory will cause AutoFac to call the DependencyResolver anyways)
            //containerBuilder.For<DefaultControllerFactory>().KnownAs<IControllerFactory>();
        }
    }
}
