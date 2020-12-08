using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApi.Data;
using WebApi.Models;
using WebApi.ViewModels.Request;
using Mapper = WebApi.ViewModels.Mapper;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly StoreDbContext _context;

        public ProductController(IHttpClientFactory clientFactory, StoreDbContext context)
        {
            _clientFactory = clientFactory;
            _context = context;
        }

        [HttpGet]
        public async Task<List<Product>> GetAll()
        {
            return await _context.Products.ToListAsync();
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<Product> Get(int id)
        {
            return await _context.Products.SingleOrDefaultAsync(p => p.Id == id);
        }

        [HttpGet]
        [Route("{name}")]
        public async Task<Product> GetByName(string name)
        {
            return await _context.Products.SingleOrDefaultAsync(p => p.Name == name);
        }

        [HttpPost]
        public async Task<int> Post(ProductRequest request)
        {
            var product = Mapper.Map<ProductRequest, Product>(request);

            await _context.Products.AddAsync(product);

            return await _context.SaveChangesAsync();
        }
    }
}
