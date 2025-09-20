using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationService
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private HttpListener httpListener;
        private Task httpListenerTask;
        private const string HealthPath = "/health-monitoring";

        public override void Run()
        {
            Trace.TraceInformation("NotificationService is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Use TLS 1.2 for service connections
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            bool result = base.OnStart();

            Trace.TraceInformation("NotificationService has been started");

            // Start lightweight HTTP endpoint for health monitoring
            StartHealthEndpoint();

            // Log current configuration (safe subset)
            try
            {
                if (RoleEnvironment.IsAvailable)
                {
                    var queueName = SafeGetConfig("NotificationsQueueName");
                    Trace.TraceInformation($"Config: NotificationsQueueName={queueName}");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Failed reading configuration at startup: {0}", ex);
            }

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("NotificationService is stopping");

            this.cancellationTokenSource.Cancel();

            // Stop health listener
            try
            {
                if (httpListener != null)
                {
                    httpListener.Close();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error stopping HttpListener: {0}", ex);
            }

            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("NotificationService has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // Main worker loop. Queue processing is planned for the next increment.
            // This loop keeps the role alive and can be extended to poll Azure Queue Storage.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("NotificationService heartbeat");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // Expected on shutdown
                }
            }
        }

        private void StartHealthEndpoint()
        {
            try
            {
                var prefix = GetHealthListenerPrefix();
                httpListener = new HttpListener();
                httpListener.Prefixes.Add(prefix);
                httpListener.Start();
                httpListenerTask = Task.Run(() => HealthListenLoopAsync(cancellationTokenSource.Token));
                Trace.TraceInformation("Health endpoint listening on {0}", prefix);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Failed to start health endpoint: {0}", ex);
            }
        }

        private string GetHealthListenerPrefix()
        {
            if (RoleEnvironment.IsAvailable)
            {
                // Bind to the specific IP and port allocated to this instance endpoint to avoid conflicts
                var ep = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["Health"].IPEndpoint;
                var ip = ep.Address.ToString();
                var port = ep.Port;
                return $"http://{ip}:{port}/";
            }
            else
            {
                // Fallback for local debugging without role environment
                return "http://127.0.0.1:8082/";
            }
        }

        private async Task HealthListenLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext context = null;
                try
                {
                    context = await httpListener.GetContextAsync();
                }
                catch (ObjectDisposedException)
                {
                    // Listener closed during shutdown
                    break;
                }
                catch (HttpListenerException)
                {
                    if (token.IsCancellationRequested) break;
                    continue;
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Health listener accept error: {0}", ex);
                    continue;
                }

                try
                {
                    var request = context.Request;
                    var response = context.Response;

                    if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(request.Url.AbsolutePath, HealthPath, StringComparison.OrdinalIgnoreCase))
                    {
                        var buffer = Encoding.UTF8.GetBytes("OK");
                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.ContentType = "text/plain";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                    }

                    response.Close();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Health listener request error: {0}", ex);
                }
            }
        }

        private static string SafeGetConfig(string key)
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(key);
            }
            catch
            {
                return null;
            }
        }
    }
}
