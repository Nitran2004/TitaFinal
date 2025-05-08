namespace ProyectoIdentity.Dtos
{
    using System.ComponentModel.DataAnnotations;
    using System.Collections.Generic;

    public class PedidoCreacionDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un punto de recolección válido")]
        public int CollectionPointId { get; set; }

        [Required(ErrorMessage = "El carrito no puede estar vacío")]
        public List<ItemCarritoDto> Cart { get; set; }
    }

    public class ItemCarritoDto
    {
        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        public string Nombre { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a cero")]
        public decimal Precio { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1")]
        public int Cantidad { get; set; }
    }
}