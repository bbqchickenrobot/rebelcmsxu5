﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Contracts;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Rebel.Foundation;
using Rebel.Foundation.Configuration;
using Rebel.Framework.DataManagement;
using Rebel.Framework.DependencyManagement;
using Rebel.Framework.Persistence;
using Rebel.Framework.Persistence.ProviderSupport;

namespace Rebel.Framework.Bootstrappers.Autofac.Modules
{
    public class LoadFromConfigurationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            Contract.Assert(builder != null, "Builder container is null");

            builder.RegisterType<FoundationConfigurationSection>().As<IFoundationConfigurationSection>()
              .ExternallyOwned();

            var configSection = ConfigurationManager.GetSection("rebel.foundation") as IFoundationConfigurationSection;

            builder.RegisterInstance(configSection)
              .As<IFoundationConfigurationSection>()
              .ExternallyOwned()
              .SingleInstance();

            foreach (PersistenceProviderElement element in configSection.PersistenceProviders)
            {
                IList<Service> services = new List<Service>();

                // Resolve the type itself
                Type implementationType = ConfigurationExtensions.GetTypeFromTypeConfigName(element.Type);

                // Offer to invoke the provider's setup module (if any exist)
                builder.RegisterModule(new ProviderAutoSetupModule(implementationType.Assembly, element.Key));

                IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle>
                  providerRegistrar = builder
                    .RegisterType(implementationType)
                    .Named<IPersistenceManager>(element.Key)
                    .SingleInstance();

                // Resolve the reader type from config
                Type readerType = ConfigurationExtensions.GetTypeFromTypeConfigName(element.Reader.Type);
                string compoundReaderKey = string.Format("{0}/{1}", element.Key, element.Reader.Key);

                // Overwrite the configuration key so that it is a compound of the reader and its parent provider
                element.Reader.InternalKey = compoundReaderKey;

                // Register the reader with IoC against the interface
                builder.RegisterType(readerType)
                    // Name this registration
                    .Named<IPersistenceReadWriter>(compoundReaderKey)
                    // Allow passing in a named constructor parameter for the alias
                    .WithParameter(new NamedParameter("alias", element.Reader.InternalKey))
                    // Allow passing in a resolved ITypeMapper
                    .WithParameter(GenerateResolvedParameter<ITypeMapper>())
                    // Allow passing in a resolved IUnitOfWork
                    .WithParameter(GenerateResolvedParameter<IUnitOfWork>());

                // Register the reader with IoC against the concrete type declared in config too
                // TODO: There's a way to do this in one call with Autofac ;)
                builder.RegisterType(readerType)
                    .Named(compoundReaderKey, readerType)
                    .WithParameter(new NamedParameter("alias", element.Reader.InternalKey))
                    .WithParameter(GenerateResolvedParameter<ITypeMapper>())
                    .WithParameter(GenerateResolvedParameter<IUnitOfWork>());

                providerRegistrar.WithParameter(element.Reader.ToParameter());

                foreach (PersistenceReadWriterElement readWriter in element.ReadWriters)
                {
                    Type repoType = ConfigurationExtensions.GetTypeFromTypeConfigName(readWriter.Type);

                    string compoundRepositoryKey = string.Format("{0}/{1}", element.Key, readWriter.Key);

                    // Overwrite the configuration key so that it is a compound of the read-writer and its parent provider
                    readWriter.InternalKey = compoundRepositoryKey;

                    builder
                      .RegisterType(repoType)
                      .Named<IPersistenceReadWriter>(compoundRepositoryKey)
                      .WithParameter(new NamedParameter("alias", readWriter.InternalKey))
                      .WithParameter(GenerateResolvedParameter<ITypeMapper>())
                      .WithParameter(GenerateResolvedParameter<IUnitOfWork>());

                    builder
                      .RegisterType(repoType)
                      .Named(compoundRepositoryKey, repoType)
                      .WithParameter(new NamedParameter("alias", readWriter.InternalKey))
                      .WithParameter(GenerateResolvedParameter<ITypeMapper>())
                      .WithParameter(GenerateResolvedParameter<IUnitOfWork>());
                }

                //TODO: This only works at the moment because the two test providers accept one ReadWriter and we're only declaring
                //one in config. Need to refactor providers to accept a list of read-writers to complete the chaining refactor
                foreach (Parameter param in element.ReadWriters.ToParameters())
                    providerRegistrar.WithParameter(param);

                providerRegistrar.WithParameter(new TypedParameter(typeof(string), element.Key));
            }
        }

        private ResolvedParameter GenerateResolvedParameter<T>()
        {
            return new ResolvedParameter(
                (paramInfo, context) =>
                (paramInfo.ParameterType == typeof(T)),
                (paramInfo, context) =>
                (context.Resolve<T>()));
        }
    }
}