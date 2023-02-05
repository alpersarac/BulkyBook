using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class OrderHeaderyRepository: Repository<OrderHeader>,IOrderHeaderRepository
    {
        private ApplicationDbContext _db;
        public OrderHeaderyRepository(ApplicationDbContext db):base(db)
        {
            _db = db;
        }

		public void UpadateStatus(int id, string orderStatus, string? paymentStatus = null)
		{
            var orderFromDb = _db.OrderHeaders.FirstOrDefault(x=>x.Id==id);
            if (orderFromDb!=null)
            {
                orderFromDb.OrderStatus = orderStatus;
                if (paymentStatus!=null)
                {
                    orderFromDb.PaymentStatus = paymentStatus;
                }
            }
		}
		public void UpadateStripePaymentID(int id, string sessionId, string PaymentIntentId)
		{
			var orderFromDb = _db.OrderHeaders.FirstOrDefault(x => x.Id == id);
            orderFromDb.SessionId = sessionId;
			orderFromDb.PaymentIntentId = PaymentIntentId;

		}

		public void Update(OrderHeader obj)
        {
            _db.OrderHeaders.Update(obj);
        }
    }
}
