using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HelpfulExtensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Performs the specified delegate on each element of a collection. 
        /// For each asynchronously.
        /// </summary>
        /// <param name="source">Source collection</param>
        /// <param name="function">Delegate</param>
        /// <typeparam name="T">Input type</typeparam>
        /// <returns>Task</returns>
        public static Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> function)
        {
            var tasks = source.Select(function);
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Performs the specified delegate on each element of a collection.
        /// </summary>
        /// <param name="source">Source collection</param>
        /// <param name="function">Delegate</param>
        /// <typeparam name="TIn">Input type</typeparam>
        /// <typeparam name="TOut">Output type</typeparam>
        /// <returns>Collection with results</returns>
        public static async Task<IEnumerable<TOut>> ForEachAsync<TIn, TOut>(this IEnumerable<TIn> source,
            Func<TIn, Task<TOut>> function)
        {
            var tasks = source.Select(function);
            var result = await Task.WhenAll(tasks).ConfigureAwait(false);

            return result.AsEnumerable();
        }

        /// <summary>
        /// Performs the specified delegate on each element of a collection. 
        /// For each asynchronously.
        /// </summary>
        /// <param name="source">Source collection</param>
        /// <param name="partitionCount">Degree of parallelism</param>
        /// <param name="function">Delegate</param>
        /// <typeparam name="T">Input type</typeparam>
        /// <returns>Task</returns>
        public static Task ForEachAsync<T>(this IEnumerable<T> source, int partitionCount, Func<T, Task> function)
        {
            var partitions = Partitioner.Create(source).GetPartitions(partitionCount);
            var tasks = partitions.Select(partition =>
                Task.Run(async () =>
                {
                    using (partition)
                    {
                        while (partition.MoveNext())
                        {
                            await function(partition.Current).ConfigureAwait(false);
                        }
                    }
                }));

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Performs the specified delegate on each element of a collection.
        /// </summary>
        /// <param name="source">Source collection</param>
        /// <param name="partitionCount">Degree of parallelism</param>
        /// <param name="function">Delegate</param>
        /// <typeparam name="TIn">Input type</typeparam>
        /// <typeparam name="TOut">Output type</typeparam>
        /// <returns>Collection with results</returns>
        public static async Task<IEnumerable<TOut>> ForEachAsync<TIn, TOut>(this IEnumerable<TIn> source,
            int partitionCount, Func<TIn, Task<TOut>> function)
        {
            var partitions = Partitioner.Create(source).GetPartitions(partitionCount);
            var tasks = partitions.Select(partition =>
                Task.Run<List<TOut>>(async () =>
                {
                    var results = new List<TOut>();
                    using (partition)
                    {
                        while (partition.MoveNext())
                        {
                            var result = await function(partition.Current).ConfigureAwait(false);
                            results.Add(result);
                        }
                    }

                    return results;
                }));

            var tasksResults = await Task.WhenAll(tasks).ConfigureAwait(false);
            return tasksResults.SelectMany(taskResults => taskResults);
        }
    }
}