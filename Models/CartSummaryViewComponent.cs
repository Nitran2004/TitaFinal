using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Models;

namespace ProyectoIdentity.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private readonly CartService _cartService;

        public CartSummaryViewComponent(CartService cartService)
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