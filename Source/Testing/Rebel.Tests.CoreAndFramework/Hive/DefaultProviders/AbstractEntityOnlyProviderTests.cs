using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Web.Hosting;
using Artem.Web.Security;
using NUnit.Framework;
using Rebel.Cms.Web.DependencyManagement;
using Rebel.Framework;
using Rebel.Framework.Persistence;
using Rebel.Framework.Persistence.Model;
using Rebel.Framework.Persistence.Model.Attribution.MetaData;
using Rebel.Framework.Persistence.Model.Constants;
using Rebel.Framework.Persistence.Model.Constants.Entities;
using Rebel.Framework.Persistence.Model.Versioning;
using Rebel.Framework.Persistence.ProviderSupport._Revised;
using Rebel.Framework.Security.Model.Entities;
using Rebel.Hive;
using Rebel.Tests.Extensions;
using FixedEntities = Rebel.Framework.Security.Model.FixedEntities;
using FixedHiveIds = Rebel.Framework.Security.Model.FixedHiveIds;

namespace Rebel.Tests.CoreAndFramework.Hive.DefaultProviders
{

   

    /// <summary>
    /// Unit tests for providers only implementing an Entity repository
    /// </summary>
    public abstract class AbstractEntityOnlyProviderTests : DisposableObject
    {
        protected IAttributeTypeRegistry AttributeTypeRegistry = new CmsAttributeTypeRegistry();
        protected abstract Action PostWriteCallback { get; }
        protected abstract ProviderSetup ProviderSetup { get; }
        protected abstract ReadonlyProviderSetup ReadonlyProviderSetup { get; }

        protected virtual TypedEntity GetMockedEntity()
        {
            return HiveModelCreationHelper.MockTypedEntity();
        }

        [Test]
        public void Get_All_Entities()
        {
            using (var uow = ProviderSetup.UnitFactory.Create())
            {
                for (var i = 0; i < 30; i++)
                {
                    uow.EntityRepository.AddOrUpdate(GetMockedEntity());
                }                
                uow.Complete();
            }
            PostWriteCallback.Invoke();

            using (var reader = ReadonlyProviderSetup.ReadonlyUnitFactory.CreateReadonly())
            {
                Assert.AreEqual(30, reader.EntityRepository.GetAll<TypedEntity>().Count());
            }


        }

        [Test]
        public void Entity_Exists()
        {
            var entity = GetMockedEntity();

            using (var uow = ProviderSetup.UnitFactory.Create())
            {
                uow.EntityRepository.AddOrUpdate(entity);
                uow.Complete();
            }
            PostWriteCallback.Invoke();

            using (var reader = ReadonlyProviderSetup.ReadonlyUnitFactory.CreateReadonly())
            {
                Assert.IsTrue(reader.EntityRepository.Exists<TypedEntity>(entity.Id));
            }

            
        }

        [Test]
        public void Add_Or_Update_Entity()
        {
            var entity = GetMockedEntity();

            using (var uow = ProviderSetup.UnitFactory.Create())
            {
                uow.EntityRepository.AddOrUpdate(entity);
                uow.Complete();
            }
            PostWriteCallback.Invoke();

            using (var reader = ReadonlyProviderSetup.ReadonlyUnitFactory.CreateReadonly())
            {

            }

            Assert.IsFalse(entity.Id.IsNullValueOrEmpty());
        }

        [Test]
        public void NestedUnitOfWork_NoProblemo()
        {
            TypedEntity entity = GetMockedEntity();
            TypedEntity entity2 = GetMockedEntity();
            AssignFakeIdsIfPassthrough(ProviderSetup.ProviderMetadata, entity);
            AssignFakeIdsIfPassthrough(ProviderSetup.ProviderMetadata, entity2);

            using (var outerUnit = ProviderSetup.UnitFactory.Create())
            {
                outerUnit.EntityRepository.AddOrUpdate(entity);
                var outerTransaction = outerUnit.EntityRepository.Transaction.GetTransactionId();

                // Do some nested writing
                using (var innerUnit = ProviderSetup.UnitFactory.Create())
                {
                    Assert.That(innerUnit.EntityRepository.Transaction.GetTransactionId(), Is.Not.EqualTo(outerTransaction));
                    innerUnit.EntityRepository.AddOrUpdate(entity2);
                    innerUnit.Complete();
                }

                // Do some nested reading
                using (var innerUnit = ReadonlyProviderSetup.ReadonlyUnitFactory.CreateReadonly())
                {
                    innerUnit.EntityRepository.Exists<TypedEntity>(new HiveId(Guid.NewGuid()));
                }

                // Do some nested reading with the writer
                using (var innerUnit = ProviderSetup.UnitFactory.Create())
                {
                    innerUnit.EntityRepository.Exists<TypedEntity>(new HiveId(Guid.NewGuid()));
                }

                // Do some nested writing
                using (var innerUnit = ProviderSetup.UnitFactory.Create())
                {
                    innerUnit.EntityRepository.AddOrUpdate(entity2);
                    innerUnit.Complete();
                }

                outerUnit.Complete();
            }

            HiveId id1 = entity.Id;
            HiveId id2 = entity2.Id;

            using (var outerUnit = ProviderSetup.UnitFactory.Create())
            {
                // Do some nested reading
                using (var innerUnit = ReadonlyProviderSetup.ReadonlyUnitFactory.CreateReadonly())
                {
                    innerUnit.EntityRepository.Exists<TypedEntity>(new HiveId(Guid.NewGuid()));
                    //innerUnit.Complete();
                }

                outerUnit.EntityRepository.AddOrUpdate(entity);

                outerUnit.Complete();
            }

            PostWriteCallback.Invoke();

            using (var unit = ReadonlyProviderSetup.ReadonlyUnitFactory.CreateReadonly())
            {
                var compare = unit.EntityRepository.Get<TypedEntity>(id1);
                var compare2 = unit.EntityRepository.Get<TypedEntity>(id2);

                Assert.IsNotNull(compare);
                Assert.IsNotNull(compare2);
            }
        }

        protected Revision<T> WithFakeIdsIfPassthrough<T>(ProviderMetadata providerMetadata, Revision<T> revision)
            where T : class, IVersionableEntity
        {
            AssignFakeIdsIfPassthrough(providerMetadata, revision);
            return revision;
        }

        protected void AssignFakeIdsIfPassthrough<T>(ProviderMetadata providerMetadata, params Revision<T>[] revision)
            where T : class, IVersionableEntity
        {
            revision.ForEach(
                x =>
                    {
                        AssignFakeIdsIfPassthrough(providerMetadata, x.Item);
                        AssignFakeIdsIfPassthrough(providerMetadata, x.MetaData);
                    });
        }

        protected void AssignFakeIdsIfPassthrough(ProviderMetadata providerMetadata, params IReferenceByHiveId[] entity)
        {
            if (!providerMetadata.IsPassthroughProvider) return;
            entity.ForEach(
                x =>
                    {
                        var allItems = x.GetAllIdentifiableItems();
                        foreach (var referenceByHiveId in allItems)
                        {
                            // Only change / set certain ids otherwise relations are broken e.g. between an attributedefinition and its group
                            if (referenceByHiveId.Id.IsNullValueOrEmpty() && (TypeFinder.IsTypeAssignableFrom<TypedEntity>(referenceByHiveId) 
                                                                              || TypeFinder.IsTypeAssignableFrom<EntitySchema>(referenceByHiveId)
                                                                              || TypeFinder.IsTypeAssignableFrom<Revision<TypedEntity>>(referenceByHiveId)))
                                referenceByHiveId.Id = new HiveId(providerMetadata.MappingRoot, providerMetadata.Alias, new HiveIdValue(Guid.NewGuid()));
                        }

                        // If we've specifically been given the item, then set its id irrespective of the rule above
                        if (x.Id.IsNullValueOrEmpty())
                            x.Id = new HiveId(providerMetadata.MappingRoot, providerMetadata.Alias, new HiveIdValue(Guid.NewGuid()));
                    });
        }

        [Test]
        public void Typed_Entity_Saves_With_Null_Attribute_Values()
        {
            var entity = GetMockedEntity();
            foreach(var a in entity.Attributes)
            {
                a.Values.Clear();
                a.DynamicValue = null;
            }

            using (var writer = ProviderSetup.UnitFactory.Create())
            {
                writer.EntityRepository.AddOrUpdate(entity);
                writer.Complete();
            }
            PostWriteCallback.Invoke();

            using (var reader = ReadonlyProviderSetup.ReadonlyUnitFactory.CreateReadonly())
            {
                var rev = reader.EntityRepository.Get<TypedEntity>(entity.Id);
                Assert.AreEqual(entity.Attributes.Count, rev.Attributes.Count);
                foreach (var a in rev.Attributes)
                {
                    Assert.AreEqual(null, a.DynamicValue);
                }

            }

        }

        [Test]
        public void Can_Save_And_Return_Subclass_Of_Typed_Entity()
        {
            //TODO: Update this test so that other providers can override the 'User' object created , for example, create an virtual 
            // method to return a subclass of TypedEntity, then change the asserts to be dynamic instead of hard coded
            // then we can get this working for membership tests.

            var admin = new UserGroup()
                {
                    Name = "Admin",
                    Id = HiveId.ConvertIntToGuid(999)
                };

            using (var writer = ProviderSetup.UnitFactory.Create())
            {
                writer.EntityRepository.AddOrUpdate(new SystemRoot());
                writer.EntityRepository.AddOrUpdate(FixedEntities.UserGroupVirtualRoot);
                writer.EntityRepository.AddOrUpdate(admin);
                writer.Complete();
            }

            using (var reader = ProviderSetup.UnitFactory.Create())
            {
                var output = reader.EntityRepository.Get<UserGroup>(true, HiveId.ConvertIntToGuid(999));
                Assert.AreEqual(1, output.Count());
                var outputUser = output.Single();
                Assert.AreEqual(admin.Name, outputUser.Name);
                Assert.AreEqual(admin.Id, outputUser.Id);
            }   
        }
    }
}