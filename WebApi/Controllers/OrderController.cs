using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WebApi.Data;
using WebApi.Models;
using WebApi.ViewModels.Request;
using Mapper = WebApi.ViewModels.Mapper;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly StoreDbContext _context;

        public OrderController(IHttpClientFactory clientFactory, StoreDbContext context)
        {
            _clientFactory = clientFactory;
            _context = context;
        }

        [HttpGet]
        [Route("{customerId}")]
        public async Task<List<Order>> GetByCustomerId(int customerId)
        {
            return await _context.Orders.Include(o => o.Customer).Where(o => o.CustomerId == customerId).ToListAsync();
        }

        [HttpPost]
        public async Task<int> Post(OrderRequest request)
        {
            var order = Mapper.Map<OrderRequest, Order>(request);

            order.Customer = await _context.Customers.SingleOrDefaultAsync(c => c.Id == request.CustomerId);
            
            var i = 0;
            foreach (var lineItem in request.LineItems)
            {
                order.LineItems[i++] =
                    new LineItem
                    {
                        Product = await _context.Products.SingleOrDefaultAsync(p => p.Id == lineItem.ProductId),
                        Quantity = lineItem.Quantity
                    };
            }

            await _context.Orders.AddAsync(order);
            return await _context.SaveChangesAsync();
        }
    }
}
