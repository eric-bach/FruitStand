using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<Program> _logger;

        public TestController(IHttpClientFactory clientFactory, ILogger<Program> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        [HttpGet]
        public async Task Test()
        {
            await RunTest();
        }

        private async Task RunTest()
        {
            const int maxIterations = 100;
            const int maxParallelRequests = 12;
            const int delay = 100;

            var testData = new List<TestData>
            {
                new TestData
                {
                    Url = "http://localhost:44000/customer/1",
                    Method = "GET"
                },
                new TestData
                {
                    Url = "http://localhost:44000/order/1",
                    Method = "GET"
                }
            };
            
            using var httpClient = new HttpClient();

            // To add any headers like Bearer Token, Media Type etc.
            //httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.xxxxxx");

            for (var step = 1; step < maxIterations; step++)
            {
                //$"Started iteration: {step}".Dump();
                var tasks = new List<Task<HttpResponseMessage>>();
            
                for (var i = 0; i < maxParallelRequests; i++)
                {
                    var method = testData[i % 2].Method;
                    if (method == "GET")
                    {
                        tasks.Add(httpClient.GetAsync(testData[i % 2].Url));
                    }
                    else if (method == "POST")
                    {
                        //tasks.Add(httpClient.PostAsync(testData[i % 2], testData[i % 2].PayloadSerialized));
                    }
                }

                // Run all tasks in parallel
                var result = await Task.WhenAll(tasks);
                //$"Completed Iteration: {step}".Dump();

                // Some delay before new iteration
                await Task.Delay(delay);
            }
        }
    }

    public class TestData
    {
        public string Url { get; set; }
        public string Method { get; set; }
        public string PayloadSerialized { get; set; }
    }
}
