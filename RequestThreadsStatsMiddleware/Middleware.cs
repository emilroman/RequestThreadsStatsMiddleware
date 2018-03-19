using Microsoft.AspNetCore.Builder;
using System;
using System.Threading;

namespace RequestThreadsStatsMiddleware
{
    public static class Middleware
    {
        private static Thread _monitoringThread = new Thread(ShowThreadStatistics) { IsBackground = true };
        private static int _requests;
        private static int _appThreads;
        private static string _appUrl;

        private const int _defaultAppThreads = 10;
        private const int _defaultCompletionPortThreads = 100;

        public static IApplicationBuilder UseRequestThreading(this IApplicationBuilder app, string appUrl, int appThreads = _defaultAppThreads)
        {
            _appThreads = appThreads;
            _appUrl = appUrl;

            _monitoringThread.Start();

            app.Use(async (context, next) =>
            {
                Interlocked.Increment(ref _requests);
                await next();
                Interlocked.Decrement(ref _requests);
            });

            return app;
        }

        private static void ShowThreadStatistics(object obj)
        {
            ThreadPool.SetMaxThreads(_appThreads, _defaultCompletionPortThreads);

            while (true)
            {
                ThreadPool.GetAvailableThreads(out var workerThreads, out var _);
                ThreadPool.GetMaxThreads(out var maxThreads, out int _);

                Console.WriteLine($"Available: {workerThreads}, Active: {maxThreads - workerThreads}, Max: {maxThreads}, Requests: {_requests}");

                Thread.Sleep(1000);
            }
        }
    }
}
