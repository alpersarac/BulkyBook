using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private ApplicationDbContext _db;
        public ProductRepository(ApplicationDbContext db):base(db)
        {
            _db = db;
        }

        public void Update(Product obj)
        {
            var objFromDb = _db.Products.Where(x=>x.Id== obj.Id).FirstOrDefault();
            if (objFromDb != null)
            {
                obj.ISBN = objFromDb.ISBN;
                obj.Author = objFromDb.Author;
                obj.Description = objFromDb.Description;    
                obj.Category = objFromDb.Category;
                obj.CategoryId = objFromDb.CategoryId;
                obj.CoverTypeId = objFromDb.CoverTypeId;    
                obj.Price= objFromDb.Price;
                obj.Price100 = objFromDb.Price100;
                obj.Price50= objFromDb.Price50;
                if (obj.ImageUrl!=null)
                {
                    objFromDb.ImageUrl = obj.ImageUrl;
                }
            }
        }
    }
}
