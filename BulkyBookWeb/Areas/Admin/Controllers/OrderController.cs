﻿using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		[BindProperty]
		public OrderVM OrderVM {get;set;}
		public OrderController(IUnitOfWork unitOfWork)
		{
			_unitOfWork=unitOfWork;
		}
		public IActionResult Index()
		{
			return View();
		}
        public IActionResult Details(int orderId)
        {
			OrderVM = new OrderVM()
			{
				OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderId, includeProperties: "ApplicationUser"),
				OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == orderId, includeProperties: "Product"),
			};
            return View(OrderVM);
        }
		[ActionName("Details")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Details_PAY_NOW()
		{
			OrderVM.OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
			OrderVM.OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

			//stripe settings 
			var domain = "https://localhost:44300/";
			var options = new SessionCreateOptions
			{
				PaymentMethodTypes = new List<string>
				{
				  "card",
				},
				LineItems = new List<SessionLineItemOptions>(),
				Mode = "payment",
				SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderid={OrderVM.OrderHeader.Id}",
				CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}",
			};

			foreach (var item in OrderVM.OrderDetail)
			{

				var sessionLineItem = new SessionLineItemOptions
				{
					PriceData = new SessionLineItemPriceDataOptions
					{
						UnitAmount = (long)(item.Price * 100),//20.00 -> 2000
						Currency = "usd",
						ProductData = new SessionLineItemPriceDataProductDataOptions
						{
							Name = item.Product.Title
						},

					},
					Quantity = item.Count,
				};
				options.LineItems.Add(sessionLineItem);

			}

			var service = new SessionService();
			Session session = service.Create(options);
			_unitOfWork.OrderHeader.UpadateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
			_unitOfWork.Save();
			Response.Headers.Add("Location", session.Url);
			return new StatusCodeResult(303);
		}
		public IActionResult PaymentConfirmation(int orderHeaderid)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderHeaderid);
			if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
			{
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);
				//check the stripe status
				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpadateStatus(orderHeaderid, orderHeader.OrderStatus, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}
			return View(orderHeaderid);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles =SD.Role_Admin +","+SD.Role_Employee)]
		public IActionResult UpdateOrderDetail()
		{
			var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id,tracked:false);
			orderHeaderFromDb.Name= OrderVM.OrderHeader.Name;
			orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
			orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
			orderHeaderFromDb.City = OrderVM.OrderHeader.City;
			orderHeaderFromDb.State = OrderVM.OrderHeader.State;
			orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
			if (OrderVM.OrderHeader.Carrier!=null)
			{
				orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
			}
			if (OrderVM.OrderHeader.TrackingNumber != null)
			{
				orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
			}
			_unitOfWork.OrderHeader.Update(orderHeaderFromDb);
			_unitOfWork.Save();
			TempData["Success"] = "Order details updated successfuly.";
			return RedirectToAction("Details", "Order", new {orderId=orderHeaderFromDb.Id});
		}
        [HttpPost]
        [ValidateAntiForgeryToken]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult StartProcessing()
        {
			_unitOfWork.OrderHeader.UpadateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["Success"] = "Order status updated successfuly.";
            return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult ShipOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, tracked: false);
            orderHeader.TrackingNumber= OrderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier=OrderVM.OrderHeader.Carrier;
			orderHeader.OrderStatus= SD.StatusShipped;
			orderHeader.ShippingDate = DateTime.Now;
			if (orderHeader.PaymentStatus==SD.PaymentStatusDelayedPayment)
			{
				orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
			}
            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
            TempData["Success"] = "Order shipped successfuly.";
            return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
        }
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult CancelOrder()
		{
			var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id, tracked: false);
			if (orderHeader.PaymentStatus==SD.PaymentStatusApproved)
			{
				var options = new RefundCreateOptions
				{
					Reason = RefundReasons.RequestedByCustomer,
					PaymentIntent = orderHeader.PaymentIntentId
				};
				var service = new RefundService();
				Refund refund = service.Create(options);

				_unitOfWork.OrderHeader.UpadateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
			}
			else
			{
				_unitOfWork.OrderHeader.UpadateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
			}
			
			_unitOfWork.Save();
			TempData["Success"] = "Order cancelled successfuly.";
			return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
		}
		#region API CALLS
		[HttpGet]
		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeader> orderHeaders;
			if (User.IsInRole(SD.Role_Admin))
			{
                orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
			}
			else
			{
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                orderHeaders = _unitOfWork.OrderHeader.GetAll(u=>u.ApplicationUserId==claim.Value,includeProperties: "ApplicationUser");
            }
            switch (status)
            {
                case "pending":
					orderHeaders = orderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
				case "inprocess":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }
            return Json(new { data = orderHeaders });
		}
		#endregion
	}
}
