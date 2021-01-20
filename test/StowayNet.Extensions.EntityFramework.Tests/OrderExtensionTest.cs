using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using StowayNet;

namespace StowayNet.Extensions.EntityFramework.Tests
{
    public enum BookType
    {
        Paperback = 1,
        eBook = 2,
    }
    public class Book
    {
        public BookType BookType { get; set; } = BookType.Paperback;

        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime PublishDate { get; set; }

        public string Author { get; set; }

        public decimal Price { get; set; }

    }

    public class OrderExtensionTest
    {
        private static List<Book> GetBooks()
        {
            return new List<Book>
            {
                new Book { BookType = BookType.eBook, Id = 1, Name = "Learning C# by Developing Games with Unity 2020: An enjoyable and intuitive approach to getting started with C# programming and Unity, 5th Edition", Author = "Harrison Ferrone", PublishDate = DateTime.Parse("2020-8-21"), Price = 53.88m },
                new Book { BookType = BookType.eBook, Id = 2, Name = "Starting out with Visual C#", Author = "Tony Gaddis", PublishDate = DateTime.Parse("2019-4-19"), Price = 45.2m },
                new Book { Id = 3, Name = "C# 9 and .NET 5 – Modern Cross-Platform Development: Build intelligent apps, websites, and services with Blazor, ASP.NET Core, and Entity Framework Core using Visual Studio Code, 5th Edition", Author = "Mark J. Price", PublishDate = DateTime.Parse("2020-10-10"), Price = 36.32m },
                new Book { Id = 4, Name = "Murach's ASP.NET Core MVC", Author = "Joel Murach and Mary Delamater", PublishDate = DateTime.Parse("2020-1-14"), Price = 56.84m },
            };
        }

        [Fact]
        public void OrderTest()
        {
            var books = GetBooks();

            var orderBooks = books.AsQueryable().OrderBy("Price").ToList();
            Assert.Equal(3, orderBooks[0].Id);
        }

        [Fact]
        public void OrderThenTest()
        {
            var books = GetBooks();

            var orderBooks = books.AsQueryable().OrderBy("BookType").ThenBy("PublishDate").ToList();
            Assert.Equal(4, orderBooks[0].Id);
        }
    }
}
