using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApi.Data;
using WebApi.Models;
using WebApi.ViewModels.Request;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CheckoutController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly StoreDbContext _context;

        public CheckoutController(IHttpClientFactory clientFactory, StoreDbContext context)
        {
            _clientFactory = clientFactory;
            _context = context;
        }

        [HttpPost]
        public async Task<string> PostAsync([FromBody] CheckoutRequest request)
        {
            var order = new Order();

            var customer = _context.Customers.FirstOrDefault(c => c.Id == request.CustomerId);

            foreach (var item in request.Items)
            {
                var product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId);

                order.LineItems.Add(new LineItem
                {
                    Product = product,
                    Quantity = item.Quantity
                });
            }

            order.Customer = customer;
            if (customer != null) order.CustomerId = customer.Id;

            var client = _clientFactory.CreateClient();
            client.BaseAddress = new Uri("https://run.mocky.io");
            var response = await client.GetAsync("/v3/73826577-f697-4f5f-9abb-6d3d3325486b");
            var content = await response.Content.ReadAsStringAsync();

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            return content;
        }
    }
}
