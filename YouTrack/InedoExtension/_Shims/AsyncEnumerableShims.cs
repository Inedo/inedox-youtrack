using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq;

internal static class AsyncEnumerableShims
{
        /// <summary>
        /// Creates a list from an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">The source async-enumerable sequence to get a list of elements for.</param>
        /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
        /// <returns>An async-enumerable sequence containing a single element with a list containing all the elements of the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
        public static ValueTask<List<TSource>> ToListAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return Core(source, cancellationToken);

            static async ValueTask<List<TSource>> Core(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
            {
                var list = new List<TSource>();

                await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    list.Add(item);
                }

                return list;
            }
        }

    /// <summary>
    /// Determines whether an async-enumerable sequence contains any elements.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">An async-enumerable sequence to check for non-emptiness.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>An async-enumerable sequence containing a single element determining whether the source sequence contains any elements.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
    public static ValueTask<bool> AnyAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return Core(source, cancellationToken);

        static async ValueTask<bool> Core(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
        {
            await using var e = source.GetConfiguredAsyncEnumerator(cancellationToken, false);

            return await e.MoveNextAsync();
        }
    }

    /// <summary>
    /// Determines whether any element of an async-enumerable sequence satisfies a condition.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">An async-enumerable sequence whose elements to apply the predicate to.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">The optional cancellation token to be used for cancelling the sequence at any time.</param>
    /// <returns>An async-enumerable sequence containing a single element determining whether any elements in the source sequence pass the test in the specified predicate.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is null.</exception>
    /// <remarks>The return type of this operator differs from the corresponding operator on IEnumerable in order to retain asynchronous behavior.</remarks>
    public static ValueTask<bool> AnyAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        return Core(source, predicate, cancellationToken);

        static async ValueTask<bool> Core(IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken)
        {
            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (predicate(item))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static ConfiguredCancelableAsyncEnumerable<T>.Enumerator GetConfiguredAsyncEnumerator<T>(this IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken, bool continueOnCapturedContext)
    {
        return enumerable.ConfigureAwait(continueOnCapturedContext).WithCancellation(cancellationToken).GetAsyncEnumerator();
    }
}
