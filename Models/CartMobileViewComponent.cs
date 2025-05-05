using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Models;

namespace ProyectoIdentity.ViewComponents
{
    public class CartMobileViewComponent : ViewComponent
    {
        private readonly CartService _cartService;

        public CartMobileViewComponent(CartService cartService)
        {
            _cartService = cartService;
        }

        public IViewComponentResult Invoke()
        {
            var itemCount = _cartService.GetItemCount();
            return View(itemCount);
        }
    }
}