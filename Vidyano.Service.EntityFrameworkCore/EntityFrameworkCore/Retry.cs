using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Vidyano.Service.EntityFrameworkCore
{
    internal static class Retry
    {
        private static readonly TimeSpan delay = TimeSpan.FromSeconds(1d);

        [DebuggerStepThrough]
        public static void Do(Action action, int retryCount = 5)
        {
            new DefaultExecutionStrategy(retryCount, delay).Do(action);
        }

        [DebuggerStepThrough]
        public static TResult Do<TResult>(Func<TResult> action, int retryCount = 5)
        {
            return new DefaultExecutionStrategy(retryCount, delay).Do(action);
        }

        [DebuggerStepThrough]
        public static Task DoAsync(Func<Task> action, int retryCount = 5)
        {
            return new DefaultExecutionStrategy(retryCount, delay).DoAsync(action);
        }

        [DebuggerStepThrough]
        public static Task<TResult> DoAsync<TResult>(Func<Task<TResult>> action, int retryCount = 5)
        {
            return new DefaultExecutionStrategy(retryCount, delay).DoAsync(action);
        }

        private sealed class DefaultExecutionStrategy
        {
            private readonly List<Exception> _exceptionsEncountered = new List<Exception>();
            private static readonly Random _random = new Random();

            private readonly int _maxRetryCount;
            private readonly TimeSpan _maxDelay;

            // <summary>
            // The default number of retry attempts, must be nonnegative.
            // </summary>
            private const int DefaultMaxRetryCount = 5;

            // <summary>
            // The default maximum random factor, must not be lesser than 1.
            // </summary>
            private const double DefaultRandomFactor = 1.1;

            // <summary>
            // The default base for the exponential function used to compute the delay between retries, must be positive.
            // </summary>
            private const double DefaultExponentialBase = 2;

            // <summary>
            // The default coefficient for the exponential function used to compute the delay between retries, must be nonnegative.
            // </summary>
            private static readonly TimeSpan DefaultCoefficient = TimeSpan.FromSeconds(1);

            // <summary>
            // The default maximum time delay between retries, must be nonnegative.
            // </summary>
            private static readonly TimeSpan DefaultMaxDelay = TimeSpan.FromSeconds(30);


            public DefaultExecutionStrategy(int maxRetryCount, TimeSpan maxDelay)
            {
            }

            public void Do(Action operation)
            {
                while (true)
                {
                    TimeSpan? nextDelay;
                    try
                    {
                        operation();
                        return;
                    }
                    catch (Exception ex)
                    {
                        if (!UnwrapAndHandleException(ex, ShouldRetryOn))
                            throw;

                        nextDelay = GetNextDelay(ex);

                        if (!nextDelay.HasValue)
                            throw new RetryLimitExceededException("Retry limit exceeded", ex);
                    }

                    var timeout = nextDelay.GetValueOrDefault();
                    if (timeout < TimeSpan.Zero)
                        throw new InvalidOperationException("DEV: Negative delay in Retry.Do");

                    Thread.Sleep(timeout);
                }
            }

            public TResult Do<TResult>(Func<TResult> operation)
            {
                while (true)
                {
                    TimeSpan? nextDelay;
                    try
                    {
                        return operation();
                    }
                    catch (Exception ex)
                    {
                        if (!UnwrapAndHandleException(ex, ShouldRetryOn))
                            throw;

                        nextDelay = GetNextDelay(ex);

                        if (!nextDelay.HasValue)
                            throw new RetryLimitExceededException("Retry limit exceeded", ex);
                    }

                    var timeout = nextDelay.GetValueOrDefault();
                    if (timeout < TimeSpan.Zero)
                        throw new InvalidOperationException("DEV: Negative delay in Retry.Do");

                    Thread.Sleep(timeout);
                }
            }

            public async Task DoAsync(Func<Task> operation)
            {
                while (true)
                {
                    TimeSpan? nextDelay;
                    try
                    {
                        await operation().ConfigureAwait(false);
                        return;
                    }
                    catch (Exception ex)
                    {
                        if (!UnwrapAndHandleException(ex, ShouldRetryOn))
                            throw;

                        nextDelay = GetNextDelay(ex);

                        if (!nextDelay.HasValue)
                            throw new RetryLimitExceededException("Retry limit exceeded", ex);
                    }

                    var timeout = nextDelay.GetValueOrDefault();
                    if (timeout < TimeSpan.Zero)
                        throw new InvalidOperationException("DEV: Negative delay in Retry.DoAsync");

                    await Task.Delay(timeout).ConfigureAwait(false);
                }
            }

            public async Task<TResult> DoAsync<TResult>(Func<Task<TResult>> operation)
            {
                while (true)
                {
                    TimeSpan? nextDelay;
                    try
                    {
                        return await operation().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (!UnwrapAndHandleException(ex, ShouldRetryOn))
                            throw;

                        nextDelay = GetNextDelay(ex);

                        if (!nextDelay.HasValue)
                            throw new RetryLimitExceededException("Retry limit exceeded", ex);
                    }

                    var timeout = nextDelay.GetValueOrDefault();
                    if (timeout < TimeSpan.Zero)
                        throw new InvalidOperationException("DEV: Negative delay in Retry.DoAsync");

                    await Task.Delay(timeout).ConfigureAwait(false);
                }
            }

            private static bool ShouldRetryOn(Exception ex)
            {
                if (ex is SqlException sqlException)
                {
                    // Enumerate through all errors found in the exception.
                    foreach (SqlError err in sqlException.Errors)
                    {
                        switch (err.Number)
                        {
                            // SQL Error Code: 40627
                            // Operation on server YYYY and database XXXX is in progress.  Please wait a few minutes before trying again.
                            case 40627:
                            // SQL Error Code: 40613
                            // Database XXXX on server YYYY is not currently available. Please retry the connection later.
                            // If the problem persists, contact customer support, and provide them the session tracing ID of ZZZZZ.
                            case 40613:
                            // SQL Error Code: 40545
                            // The service is experiencing a problem that is currently under investigation. Incident ID: %ls. Code: %d.
                            case 40545:
                            // SQL Error Code: 40540
                            // The service has encountered an error processing your request. Please try again.
                            case 40540:
                            // SQL Error Code: 40501
                            // The service is currently busy. Retry the request after 10 seconds. Code: (reason code to be decoded).
                            case 40501:
                            // SQL Error Code: 40197
                            // The service has encountered an error processing your request. Please try again.
                            case 40197:
                            // SQL Error Code: 10929
                            // Resource ID: %d. The %s minimum guarantee is %d, maximum limit is %d and the current usage for the database is %d.
                            // However, the server is currently too busy to support requests greater than %d for this database.
                            // For more information, see http://go.microsoft.com/fwlink/?LinkId=267637. Otherwise, please try again.
                            case 10929:
                            // SQL Error Code: 10928
                            // Resource ID: %d. The %s limit for the database is %d and has been reached. For more information,
                            // see http://go.microsoft.com/fwlink/?LinkId=267637.
                            case 10928:
                            // SQL Error Code: 10060
                            // A network-related or instance-specific error occurred while establishing a connection to SQL Server.
                            // The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server
                            // is configured to allow remote connections. (provider: TCP Provider, error: 0 - A connection attempt failed
                            // because the connected party did not properly respond after a period of time, or established connection failed
                            // because connected host has failed to respond.)"}
                            case 10060:
                            // SQL Error Code: 10054
                            // A transport-level error has occurred when sending the request to the server.
                            // (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)
                            case 10054:
                            // SQL Error Code: 10053
                            // A transport-level error has occurred when receiving results from the server.
                            // An established connection was aborted by the software in your host machine.
                            case 10053:
                            // SQL Error Code: 233
                            // The client was unable to establish a connection because of an error during connection initialization process before login.
                            // Possible causes include the following: the client tried to connect to an unsupported version of SQL Server;
                            // the server was too busy to accept new connections; or there was a resource limitation (insufficient memory or maximum
                            // allowed connections) on the server. (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by
                            // the remote host.)
                            case 233:
                            // SQL Error Code: 64
                            // A connection was successfully established with the server, but then an error occurred during the login process.
                            // (provider: TCP Provider, error: 0 - The specified network name is no longer available.)
                            case 64:
                            // DBNETLIB Error Code: 20
                            // The instance of SQL Server you attempted to connect to does not support encryption.
                            case 20:
                                return true;
                                // This exception can be thrown even if the operation completed succesfully, so it's safer to let the application fail.
                                // DBNETLIB Error Code: -2
                                // Timeout expired. The timeout period elapsed prior to completion of the operation or the server is not responding. The statement has been terminated.
                                //case -2:
                        }
                    }

                    return false;
                }

                return ex is TimeoutException;
            }

            /// <summary>
            /// Determines whether the operation should be retried and the delay before the next attempt.
            /// </summary>
            /// <param name="lastException">The exception thrown during the last execution attempt.</param>
            /// <returns>
            /// Returns the delay indicating how long to wait for before the next execution attempt if the operation should be retried;
            /// <c>null</c> otherwise
            /// </returns>
            private TimeSpan? GetNextDelay(Exception lastException)
            {
                _exceptionsEncountered.Add(lastException);

                var currentRetryCount = _exceptionsEncountered.Count - 1;
                if (currentRetryCount < _maxRetryCount)
                {
                    var delta = (Math.Pow(DefaultExponentialBase, currentRetryCount) - 1.0)
                                * (1.0 + _random.NextDouble() * (DefaultRandomFactor - 1.0));

                    var delay = Math.Min(
                        DefaultCoefficient.TotalMilliseconds * delta,
                        _maxDelay.TotalMilliseconds);

                    return TimeSpan.FromMilliseconds(delay);
                }

                return null;
            }

            /// <summary>
            /// Recursively gets InnerException from <paramref name="exception" /> as long as it's an <see cref="DbUpdateException" />
            /// and passes it to <paramref name="exceptionHandler" />
            /// </summary>
            /// <typeparam name="T">The type of the unwrapped exception.</typeparam>
            /// <param name="exception"> The exception to be unwrapped. </param>
            /// <param name="exceptionHandler"> A delegate that will be called with the unwrapped exception. </param>
            /// <returns>
            /// The result from <paramref name="exceptionHandler" />.
            /// </returns>
            private static T UnwrapAndHandleException<T>(Exception exception, Func<Exception, T> exceptionHandler)
            {
                if (exception is DbUpdateException dbUpdateException)
                {
                    return UnwrapAndHandleException(dbUpdateException.InnerException, exceptionHandler);
                }

                return exceptionHandler(exception);
            }
        }
    }
}