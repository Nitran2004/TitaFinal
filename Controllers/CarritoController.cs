using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Models;

namespace ProyectoIdentity.Controllers
{
    public class CarritoController : Controller
    {
        private readonly CartService _cartService;

        public CarritoController(CartService cartService)
        {
            _cartService = cartService;
        }

        public IActionResult Index()
        {
            var items = _cartService.GetAllItems();
            ViewBag.Total = _cartService.GetTotalAmount();
            return View(items);
        }

        public IActionResult Add(int id, string name, decimal price)
        {
            _cartService.AddItem(new Carrito { Id = id, Name = name, Price = price, Quantity = 1 });
            return RedirectToAction("Index");
        }

        public IActionResult Remove(int id)
        {
            _cartService.RemoveItem(id);
            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            _cartService.ClearCart();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            if (quantity > 0)
            {
                _cartService.UpdateQuantity(id, quantity);
            }
            return RedirectToAction("Index");
        }
    }
}