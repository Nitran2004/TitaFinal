using ProyectoIdentity.Models.ProyectoIdentity.Extensions;

namespace ProyectoIdentity.Models
{
    public class CartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CartSessionKey = "CartSession";

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private List<Carrito> GetCart()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var cart = session.GetObjectFromJson<List<Carrito>>(CartSessionKey);
            return cart ?? new List<Carrito>();
        }

        private void SaveCart(List<Carrito> cart)
        {
            _httpContextAccessor.HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
        }

        public void AddItem(Carrito item)
        {
            var cart = GetCart();
            var existing = cart.FirstOrDefault(x => x.Id == item.Id);
            if (existing != null)
            {
                existing.Cantidad += item.Cantidad;
            }
            else
            {
                cart.Add(item);
            }
            SaveCart(cart);
        }

        public void RemoveItem(int itemId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.Id == itemId);
            if (item != null)
            {
                if (item.Cantidad > 1)
                    item.Cantidad--;
                else
                    cart.Remove(item);
            }
            SaveCart(cart);
        }

        public void UpdateQuantity(int itemId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.Id == itemId);
            if (item != null)
            {
                item.Cantidad = quantity;
                if (item.Cantidad <= 0)
                    cart.Remove(item);
            }
            SaveCart(cart);
        }

        public int GetItemCount()
        {
            return GetCart().Sum(x => x.Cantidad);
        }

        public decimal GetTotalAmount()
        {
            return GetCart().Sum(x => x.Total * x.Cantidad);
        }

        public List<Carrito> GetAllItems() => GetCart();

        public void ClearCart()
        {
            SaveCart(new List<Carrito>());
        }
    }
}