﻿using System;

namespace Rebel.Hive.ProviderSupport
{
    using System.Collections.Generic;
    using System.Linq;
    using Rebel.Framework;
    using Rebel.Framework.Diagnostics;

    public class NullProviderTransaction : IProviderTransaction
    {
        public bool IsTransactional { get { return false; } }

        public bool IsActive
        {
            get { return false; }
        }

        public bool WasCommitted { get; private set; }

        public bool WasRolledBack { get; private set; }

        public void EnsureBegun()
        {
            return;
        }

        public void Rollback(IProviderUnit work)
        {
            WasRolledBack = true;
            return;
        }

        public bool HasCommitalActionsToPerform()
        {
            return CacheFlushActions != null && CacheFlushActions.Any();
        }

        public void PerformPreCommitalActions()
        {
            CacheFlushActions.IfNotNull(x => x.ForEach(y => y.Invoke()));
        }

        public void Commit(IProviderUnit work)
        {
            try
            {
                PerformPreCommitalActions();
            }
            catch (Exception ex)
            {
                LogHelper.Error<NullProviderTransaction>("Could not perform an action on completion of a transaction. The transaction will continue.", ex);
            } 
            WasCommitted = true;
            return;
        }

        public string GetTransactionId()
        {
            return GetHashCode().ToString();
        }

        public void Dispose()
        {
            return;
        }

        private readonly List<Action> _cacheFlushActions = new List<Action>();
        /// <summary>
        /// Gets a list of actions that will be performed on completion, for example cache updating
        /// </summary>
        /// <value>The on completion actions.</value>
        public List<Action> CacheFlushActions
        {
            get
            {
                return _cacheFlushActions;
            }
        }
    }
}