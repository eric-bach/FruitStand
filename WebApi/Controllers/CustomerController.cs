using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebApi.Data;
using WebApi.Models;
using WebApi.ViewModels.Request;
using Mapper = WebApi.ViewModels.Mapper;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<Program> _logger;
        private readonly StoreDbContext _context;

        public CustomerController(IHttpClientFactory clientFactory, ILogger<Program> logger, StoreDbContext context)
        {
            _clientFactory = clientFactory;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<Customer> Get(int id)
        {
            _logger.LogInformation("Hello from Customer");
            return await _context.Customers.Include(c => c.Orders).SingleOrDefaultAsync(c => c.Id == id);
        }

        [HttpPut]
        public async Task<int> Put(CustomerUpdateRequest request)
        {
            var customer = await _context.Customers.SingleOrDefaultAsync(c => c.Id == request.Id);

            customer.Email = request.Email;
            customer.Phone = request.Phone;
            customer.Address = request.Address;

            _context.Customers.Update(customer);
            return await _context.SaveChangesAsync();
        }

        [HttpPost]
        public async Task<int> Post(CustomerRequest request)
        {
            var customer = Mapper.Map<CustomerRequest, Customer>(request);

            await _context.Customers.AddAsync(customer);
            return await _context.SaveChangesAsync();
        }
    }
}
