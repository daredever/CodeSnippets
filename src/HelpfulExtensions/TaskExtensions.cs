using System;
using System.Threading;
using System.Threading.Tasks;

namespace HelpfulExtensions
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Run task with cancellation.
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <typeparam name="T">Output type</typeparam>
        /// <returns>Task result</returns>
        /// <exception cref="ArgumentNullException">task is null</exception>
        /// <exception cref="OperationCanceledException">when cancellation is requested</exception>
        public static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (!cancellationToken.CanBeCanceled || task.IsCompleted)
            {
                return task;
            }

            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<T>(cancellationToken)
                : WithCancellation(task, cancellationToken, false);
        }

        /// <summary>
        /// Run task with cancellation.
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        /// <exception cref="ArgumentNullException">task is null</exception>
        /// <exception cref="OperationCanceledException">when cancellation is requested</exception>
        public static Task WithCancellation(this Task task, CancellationToken cancellationToken)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (!cancellationToken.CanBeCanceled || task.IsCompleted)
            {
                return task;
            }

            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : WithCancellation(task, cancellationToken, false);
        }

        private static async Task<T> WithCancellation<T>(Task<T> task, CancellationToken cancellationToken,
            bool continueOnCapturedContext)
        {
            await ThrowIfCancellationRequested(task, cancellationToken);
            return await task.ConfigureAwait(continueOnCapturedContext);
        }

        private static async Task WithCancellation(Task task, CancellationToken cancellationToken,
            bool continueOnCapturedContext)
        {
            await ThrowIfCancellationRequested(task, cancellationToken);
            await task.ConfigureAwait(continueOnCapturedContext);
        }

        private static async Task ThrowIfCancellationRequested(Task task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<Empty>();
            static void Callback(object s) => ((TaskCompletionSource<Empty>) s).TrySetResult(new Empty());
            await using (cancellationToken.Register(Callback, tcs))
            {
                var taskForCancel = tcs.Task;
                var completedTask = await Task.WhenAny(task, taskForCancel).ConfigureAwait(false);
                if (completedTask == taskForCancel)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        /// <summary>
        /// Empty struct.
        /// </summary>
        private struct Empty
        {
        }
    }
}