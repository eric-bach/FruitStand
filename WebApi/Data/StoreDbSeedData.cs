using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebApi.Models;

namespace WebApi.Data
{
    public static class StoreDbSeedData
    {
        internal static StoreDbContext SeedData(this StoreDbContext context)
        {
            // Reset sequences
            context.Database.ExecuteSqlRaw("DBCC CHECKIDENT('Customers', RESEED, 0)");
            context.Database.ExecuteSqlRaw("DBCC CHECKIDENT('Products', RESEED, 0)");
            context.Database.ExecuteSqlRaw("DBCC CHECKIDENT('Orders', RESEED, 0)");
            context.Database.ExecuteSqlRaw("DBCC CHECKIDENT('LineItems', RESEED, 0)");

            // Clear tables
            context.LineItems.RemoveRange(context.LineItems);
            context.Orders.RemoveRange(context.Orders);
            context.Products.RemoveRange(context.Products);
            context.Customers.RemoveRange(context.Customers);
            context.SaveChanges();

            // Seed data
            var customers = new Customer[]
            {
                new Customer {FirstName = "Jane", LastName = "Doe", Email = "jane@doe.com", Phone = "1234567890", Address = "123 Fake Street"},
                new Customer {FirstName = "John", LastName = "Doe", Email = "john@doe.com", Phone = "2345678901", Address = "125 Fake Street"},
            };
            context.Customers.AddRange(customers);
            context.SaveChanges();

            var products = new []
            {
                new Product {Name = "Apple", Description = "Juicy apple", Price = 0.5m},
                new Product {Name = "Banana", Description = "Yellow banana", Price = 0.25m},
                new Product {Name = "Orange", Description = "Sweet orange", Price = 0.75m},
                new Product {Name = "Pineapple", Description = "Sweet pineapple", Price = 2.50m},
                new Product {Name = "Grapefruit", Description = "Large grapefruit", Price = 1.00m}
            };
            context.Products.AddRange(products);
            context.SaveChanges();

            var orders = new[]
            {
                new Order {
                    LineItems = new List<LineItem>
                    {
                        new LineItem { Product = context.Products.First(p => p.Name == "Apple"), Quantity = 3 },
                        new LineItem { Product = context.Products.First(p => p.Name == "Banana"), Quantity = 5 },
                        new LineItem { Product = context.Products.First(p => p.Name == "Orange"), Quantity = 3 },
                    },
                    CustomerId = context.Customers.Single(c => c.Id == 1).Id
                },
                new Order {
                    LineItems = new List<LineItem>
                    {
                        new LineItem { Product = context.Products.First(p => p.Name == "Banana"), Quantity = 10 },
                        new LineItem { Product = context.Products.First(p => p.Name == "Pineapple"), Quantity = 2 },
                        new LineItem { Product = context.Products.First(p => p.Name == "Grapefruit"), Quantity = 5 },
                    },
                    CustomerId = context.Customers.Single(c => c.Id == 2).Id
                },
            };
            context.Orders.AddRange(orders);
            context.SaveChanges();

            return context;
        }
    }
}
