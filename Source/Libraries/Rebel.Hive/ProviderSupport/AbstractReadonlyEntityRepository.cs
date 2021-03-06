﻿using System.Collections.Generic;
using System.Linq;
using Rebel.Framework;
using Rebel.Framework.Context;
using Rebel.Framework.Persistence.Model;
using Rebel.Framework.Persistence.Model.Associations;
using Rebel.Framework.Persistence.Model.Attribution.MetaData;
using Rebel.Framework.Persistence.ProviderSupport._Revised;
using Rebel.Hive.ProviderGrouping;

namespace Rebel.Hive.ProviderSupport
{
    using System;
    using Rebel.Framework.Caching;
    using Rebel.Framework.Data;
    using Rebel.Framework.Linq.QueryModel;

    using Rebel.Framework.Linq.ResultBinding;
    using Rebel.Hive.Caching;

    public abstract class AbstractReadonlyEntityRepository : AbstractProviderRepository, IReadonlyEntityRepository
    {
        protected AbstractReadonlyEntityRepository(ProviderMetadata providerMetadata, AbstractReadonlyRevisionRepository<TypedEntity> revisions, AbstractReadonlySchemaRepository schemas, IFrameworkContext frameworkContext)
            : base(providerMetadata, frameworkContext)
        {
            Revisions = revisions;
            Schemas = schemas;
            EntitiesAddedOrUpdated = new HashSet<IRelatableEntity>();
        }

        protected AbstractReadonlyEntityRepository(ProviderMetadata providerMetadata, IFrameworkContext frameworkContext)
            : base(providerMetadata, frameworkContext)
        {
            Revisions = new NullProviderRevisionRepository<TypedEntity>(providerMetadata, frameworkContext);
            EntitiesAddedOrUpdated = new HashSet<IRelatableEntity>();
        }

        public abstract IEnumerable<T> PerformGet<T>(bool allOrNothing, params HiveId[] ids) where T : TypedEntity;
        public IEnumerable<T> Get<T>(bool allOrNothing, params HiveId[] ids) where T : TypedEntity
        {
            if (!ids.Any()) return Enumerable.Empty<T>();

            var performGet = PerformGet<T>(allOrNothing, ids) ?? Enumerable.Empty<T>();

            return performGet.WhereNotNull().Select(x =>
                    {
                        x.EntitySchema = Schemas.GetComposite<EntitySchema>(x.EntitySchema.Id);
                        return ProviderRepositoryHelper.SetProviderAliasOnId(ProviderMetadata, this.SetRelationProxyLazyLoadDelegate(x));
                    });
        }

        public abstract IEnumerable<T> PerformExecuteMany<T>(QueryDescription query, ObjectBinder objectBinder);
        public IEnumerable<T> ExecuteMany<T>(QueryDescription query, ObjectBinder objectBinder)
        {
            var key = CacheKey.Create(new HiveQueryCacheKey(query));
            Func<List<T>> execution = () =>
                {
                    var executeItems = PerformExecuteMany<T>(query, objectBinder) ?? Enumerable.Empty<T>();
                    return executeItems.ToList(); // Ensure the executed sequence is cached
                };
            var cacheCheck = ContextCacheAvailable() ? HiveContext.GenerationScopedCache.GetOrCreate(key, execution) : null;

            var performExecute = cacheCheck != null ? cacheCheck.Value.Item : execution.Invoke();
            //var performExecute = PerformExecuteMany<T>(query, objectBinder) ?? Enumerable.Empty<T>();

            var items = performExecute
                    .ForAllThatAre<T, IReferenceByHiveId>(entity => ProviderRepositoryHelper.SetProviderAliasOnId(ProviderMetadata, entity))
                    .ForAllThatAre<T, IRelatableEntity>(entity => this.SetRelationProxyLazyLoadDelegate(entity));

            if (query.From.RequiredEntityIds.Any() && !query.SortClauses.Any())
            {
                // Return in same order as supplied ids
                return query.From.RequiredEntityIds.Select(x => items.OfType<IReferenceByHiveId>().FirstOrDefault(y => y.Id.Value == x.Value)).WhereNotNull().OfType<T>();
            }

            return items;
        }

        public abstract T PerformExecuteScalar<T>(QueryDescription query, ObjectBinder objectBinder);
        public T ExecuteScalar<T>(QueryDescription query, ObjectBinder objectBinder)
        {
            var key = CacheKey.Create(new HiveQueryCacheKey(query));
            Func<T> execution = () => PerformExecuteScalar<T>(query, objectBinder);
            var cacheCheck = ContextCacheAvailable() ? HiveContext.GenerationScopedCache.GetOrCreate(key, execution) : null;
            //var performExecute = PerformExecuteScalar<T>(query, objectBinder);
            var performExecute = cacheCheck != null ? cacheCheck.Value.Item : execution.Invoke();

            if ((!typeof(T).IsValueType && performExecute == null) || performExecute.Equals(default(T)))
            {
                return default(T);
            }

            if (typeof(IReferenceByHiveId).IsAssignableFrom(typeof(T)))
                ProviderRepositoryHelper.SetProviderAliasOnId(ProviderMetadata, (IReferenceByHiveId)performExecute);

            if (typeof(IRelatableEntity).IsAssignableFrom(typeof(T)))
                this.SetRelationProxyLazyLoadDelegate((IRelatableEntity)performExecute);

            return performExecute;
        }

        public abstract T PerformExecuteSingle<T>(QueryDescription query, ObjectBinder objectBinder);
        public T ExecuteSingle<T>(QueryDescription query, ObjectBinder objectBinder)
        {
            var key = CacheKey.Create(new HiveQueryCacheKey(query));
            Func<T> execution = () => PerformExecuteSingle<T>(query, objectBinder);
            var cacheCheck = ContextCacheAvailable() ? HiveContext.GenerationScopedCache.GetOrCreate(key, execution) : null;
            //var performExecute = PerformExecuteSingle<T>(query, objectBinder);
            var performExecute = cacheCheck != null ? cacheCheck.Value.Item : execution.Invoke();

            if ((!typeof(T).IsValueType && performExecute == null) || performExecute.Equals(default(T)))
            {
                return default(T);
            }

            if (typeof(IReferenceByHiveId).IsAssignableFrom(typeof(T)))
                ProviderRepositoryHelper.SetProviderAliasOnId(ProviderMetadata, (IReferenceByHiveId)performExecute);

            if (typeof(IRelatableEntity).IsAssignableFrom(typeof(T)))
                this.SetRelationProxyLazyLoadDelegate((IRelatableEntity)performExecute);

            return performExecute;
        }

        public abstract IEnumerable<T> PerformGetAll<T>() where T : TypedEntity;
        public IEnumerable<T> GetAll<T>() where T : TypedEntity
        {
            var performGetAll = PerformGetAll<T>() ?? Enumerable.Empty<T>();

            return performGetAll.Select(x =>
                    {
                        x.EntitySchema = Schemas.Get<EntitySchema>(x.EntitySchema.Id) ?? x.EntitySchema;
                        return ProviderRepositoryHelper.SetProviderAliasOnId(ProviderMetadata, this.SetRelationProxyLazyLoadDelegate(x));
                    });
        }

        public AbstractReadonlyRevisionRepository<TypedEntity> Revisions { get; protected set; }
        public AbstractReadonlySchemaRepository Schemas { get; protected set; }
        public abstract bool CanReadRelations { get; }
        protected internal HashSet<IRelatableEntity> EntitiesAddedOrUpdated { get; set; }

        public abstract IEnumerable<IRelationById> PerformGetParentRelations(HiveId childId, RelationType relationType = null);
        public IEnumerable<IRelationById> GetParentRelations(HiveId childId, RelationType relationType = null)
        {
            var key = CacheKey.Create(new HiveRelationCacheKey(HiveRelationCacheKey.RepositoryTypes.Entity, childId, Direction.Parents, relationType));
            Func<List<IRelationById>> execution = () =>
                {
                    var getRelations = PerformGetParentRelations(childId, relationType) ?? Enumerable.Empty<IRelationById>();
                    return getRelations.ToList();
                };
            var items = ContextCacheAvailable() ? HiveContext.GenerationScopedCache.GetOrCreate(key, execution) : null;
            var results = items != null ? items.Value.Item : execution.Invoke();

            return this.ProcessRelations(results, ProviderMetadata);
        }

        public abstract IRelationById PerformFindRelation(HiveId sourceId, HiveId destinationId, RelationType relationType);
        public IRelationById FindRelation(HiveId sourceId, HiveId destinationId, RelationType relationType)
        {
            var findRelation = PerformFindRelation(sourceId, destinationId, relationType);
            return findRelation == null ? null : this.ProcessRelations(Enumerable.Repeat(findRelation, 1), this.ProviderMetadata).FirstOrDefault();
        }

        public abstract IEnumerable<IRelationById> PerformGetAncestorRelations(HiveId descendentId, RelationType relationType = null);
        public IEnumerable<IRelationById> GetAncestorRelations(HiveId descendentId, RelationType relationType = null)
        {
            //var key = CacheKey.Create(new HiveRelationCacheKey(HiveRelationCacheKey.RepositoryTypes.Entity, descendentId, Direction.Ancestors, relationType));
            //var items = HiveContext.GenerationScopedCache.GetOrCreate(key, () =>
            //    {
            //        var performGetRelations = PerformGetAncestorRelations(descendentId, relationType) ?? Enumerable.Empty<IRelationById>();
            //        return performGetRelations.ToList();
            //    });
            //return this.ProcessRelations(items.Value.Item, ProviderMetadata);
            var performGetRelations = PerformGetAncestorRelations(descendentId, relationType) ?? Enumerable.Empty<IRelationById>();
            return this.ProcessRelations(performGetRelations.ToList(), ProviderMetadata);
        }

        public abstract IEnumerable<IRelationById> PerformGetDescendentRelations(HiveId ancestorId, RelationType relationType = null);
        public IEnumerable<IRelationById> GetDescendentRelations(HiveId ancestorId, RelationType relationType = null)
        {
            //var key = CacheKey.Create(new HiveRelationCacheKey(HiveRelationCacheKey.RepositoryTypes.Entity, ancestorId, Direction.Descendents, relationType));
            //var items = HiveContext.GenerationScopedCache.GetOrCreate(key, () =>
            //    {
            //        var performGetRelations = PerformGetDescendentRelations(ancestorId, relationType) ?? Enumerable.Empty<IRelationById>();
            //        return performGetRelations.ToList();
            //    });
            //return this.ProcessRelations(items.Value.Item, ProviderMetadata);
            var performGetRelations = PerformGetDescendentRelations(ancestorId, relationType) ?? Enumerable.Empty<IRelationById>();
            return this.ProcessRelations(performGetRelations.ToList(), ProviderMetadata);
        }

        public abstract IEnumerable<IRelationById> PerformGetChildRelations(HiveId parentId, RelationType relationType = null);
        public IEnumerable<IRelationById> GetChildRelations(HiveId parentId, RelationType relationType = null)
        {
            var key = CacheKey.Create(new HiveRelationCacheKey(HiveRelationCacheKey.RepositoryTypes.Entity, parentId, Direction.Children, relationType));
            Func<List<IRelationById>> execution = () =>
                {
                    var performGetRelations = PerformGetChildRelations(parentId, relationType) ?? Enumerable.Empty<IRelationById>();
                    return performGetRelations.ToList();
                };
            var items = ContextCacheAvailable() ? HiveContext.GenerationScopedCache.GetOrCreate(key, execution) : null;
            var results = items != null ? items.Value.Item : execution.Invoke();
            return this.ProcessRelations(results, ProviderMetadata);
        }

        public virtual IEnumerable<IRelationById> PerformGetBranchRelations(HiveId siblingId, RelationType relationType = null)
        {
            var parentRelation = GetParentRelations(siblingId, relationType).FirstOrDefault();
            if (parentRelation == null) return Enumerable.Empty<IRelationById>();
            return GetChildRelations(parentRelation.SourceId, relationType);
        }

        public IEnumerable<IRelationById> GetBranchRelations(HiveId siblingId, RelationType relationType = null)
        {
            var key = CacheKey.Create(new HiveRelationCacheKey(HiveRelationCacheKey.RepositoryTypes.Entity, siblingId, Direction.Siblings, relationType));
            Func<List<IRelationById>> execution = () =>
                {
                    var performGetRelations = PerformGetBranchRelations(siblingId, relationType) ?? Enumerable.Empty<IRelationById>();
                    return performGetRelations.ToList();
                };
            var items = ContextCacheAvailable() ? HiveContext.GenerationScopedCache.GetOrCreate(key, execution) : null;
            var results = items != null ? items.Value.Item : execution.Invoke();

            return this.ProcessRelations(results, ProviderMetadata);
        }

        public abstract bool Exists<TEntity>(HiveId id) where TEntity : TypedEntity;
    }
}