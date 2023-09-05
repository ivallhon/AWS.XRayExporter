using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using Opentelemetry.Proto.Collector.Trace.V1;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace XRayExporter.Test
{
    public class IntegrationTest
    {
        [SetUp]
        public void Setup()
        {
            Environment.SetEnvironmentVariable("OTLP_ENDPOINT", "http://localhost:4318/v1/traces");
        }

        [Test]
        public async Task SendSampleToBackend()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            string tracesJson = File.ReadAllText("data/batchgettraces.json");

            var conv = new XRay2OTLP.Convert(null, true);
            var exportTraceServiceRequest = conv.FromXRay(tracesJson);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            
            var authHeader = Environment.GetEnvironmentVariable("OTLP_HEADER_AUTHORIZATION");
            if (!String.IsNullOrEmpty(authHeader))
                client.DefaultRequestHeaders.Add("Authorization", authHeader);

            var otlpEndpoint = Environment.GetEnvironmentVariable("OTLP_ENDPOINT");
            
            if (!otlpEndpoint.Contains("v1/traces"))
                if (otlpEndpoint.EndsWith("/"))
                    otlpEndpoint = otlpEndpoint += "v1/traces";
                else
                    otlpEndpoint = otlpEndpoint += "/v1/traces";

            var content = new XRay2OTLP.ExportRequestContent(exportTraceServiceRequest);

            var res = await client.PostAsync(otlpEndpoint, content);
            if (!res.IsSuccessStatusCode)
            {
                throw new Exception("Couldn't send span " + (res.StatusCode));
            }

        }
    }
}