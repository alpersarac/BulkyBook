using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        IUnitOfWork _unitOfWork;
        public ProductController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<CoverType> objCoverType = _unitOfWork.CoverType.GetAll();
            return View(objCoverType);
        }
        //GET
        public IActionResult Create()
        {
            return View();
        }
       
        //GET
        public IActionResult Upsert(int? id)
        {
            Product product = new();
            IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category.GetAll().Select(
                u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }
            );
            IEnumerable<SelectListItem> CoverTypeList = _unitOfWork.CoverType.GetAll().Select(
                u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }
             );
            if (id == null || id == 0)
            {
                //create a product
                ViewBag.CategoryList = CategoryList;
                ViewData["CoverTypeList"] = CoverTypeList;
                return View(product);
            }
            else
            {
                //update the product
            }

            return View();
        }
        //Post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(CoverType obj)
        {
            //if (obj.Name == obj.Name.ToString())
            //{
            //    ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the name");
            //    //Custom Error.
            //    //ModelState.AddModelError("CustomError", "The DisplayOrder cannot exactly match the name");
            //}
            if (ModelState.IsValid)
            {
                _unitOfWork.CoverType.Update(obj);
                _unitOfWork.Save();
                TempData["success"] = "Cover Type has been updated successfully";

                return RedirectToAction("Index");
            }
            return View(obj);

        }

        //GET
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var CoverTypeFromDb = _unitOfWork.CoverType.GetFirstOrDefault(u => u.Id == id);

            if (CoverTypeFromDb == null)
            {
                return NotFound();
            }

            return View(CoverTypeFromDb);
        }
        //Post
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var obj = _unitOfWork.CoverType.GetFirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return NotFound();
            }

            _unitOfWork.CoverType.Remove(obj);
            _unitOfWork.Save();
            TempData["success"] = "Cover Type has been deleted successfully";

            return RedirectToAction("Index");

        }
    }
}
