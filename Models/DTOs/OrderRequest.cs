namespace ProyectoIdentity.Models.DTOs
{
    public class OrderRequest
    {
        public List<CartItem> Cart { get; set; }
        public int CollectionPointId { get; set; }
    }
    public class CartItem
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }

}
