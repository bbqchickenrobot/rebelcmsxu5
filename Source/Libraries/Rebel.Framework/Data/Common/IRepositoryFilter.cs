﻿using System.Collections.Generic;

namespace Rebel.Framework.Data.Common
{
    public interface IRepositoryFilter<TEnumeratorType, TEntityType>

        where TEnumeratorType : IEnumerable<TEntityType>
        where TEntityType : class
    {
        TEnumeratorType ToList();
    }
}
