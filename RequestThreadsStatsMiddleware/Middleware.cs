using Microsoft.AspNetCore.Builder;
using System;
using System.Threading;

namespace RequestThreadsStatsMiddleware
{
    public static class Middleware
    {
        private static readonly Thread MonitoringThread = new Thread(ShowThreadStatistics) { IsBackground = true };
        private static int requests;
        private static int appThreads;

        private const int DefaultAppThreads = 10;
        private const int DefaultCompletionPortThreads = 100;

        /// <summary>
        /// Middleware that displays stats about the request threadpool usage to the console
        /// </summary>
        /// <param name="app"></param>
        /// <param name="applicationThreads">The number of threads for the application (defaults to 10)</param>
        /// <returns></returns>
        public static IApplicationBuilder UseRequestThreading(this IApplicationBuilder app, int applicationThreads = DefaultAppThreads)
        {
            appThreads = applicationThreads;

            MonitoringThread.Start();

            app.Use(async (context, next) =>
            {
                Interlocked.Increment(ref requests);
                await next();
                Interlocked.Decrement(ref requests);
            });

            return app;
        }

        private static void ShowThreadStatistics(object obj)
        {
            ThreadPool.SetMaxThreads(appThreads, DefaultCompletionPortThreads);

            while (true)
            {
                ThreadPool.GetAvailableThreads(out var workerThreads, out var _);
                ThreadPool.GetMaxThreads(out var maxThreads, out int _);

                Console.WriteLine($"Available: {workerThreads}, Active: {maxThreads - workerThreads}, Max: {maxThreads}, Requests: {requests}");

                Thread.Sleep(1000);
            }
        }
    }
}
