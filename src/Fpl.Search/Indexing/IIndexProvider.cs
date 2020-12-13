﻿using System.Threading.Tasks;
using Fpl.Search.Models;

namespace Fpl.Search.Indexing
{
    public interface IIndexProvider<T> where T : IIndexableItem
    {
        string IndexName { get; }
        Task<(T[], bool)> GetBatchToIndex(int i, int batchSize);
    }
}