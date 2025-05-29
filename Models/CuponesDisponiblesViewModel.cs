namespace ProyectoIdentity.Models
{
    public class CuponesDisponiblesViewModel
    {
        public List<CuponViewModel> CuponesPromos { get; set; } = new List<CuponViewModel>();
        public List<CuponViewModel> CuponesCervezas { get; set; } = new List<CuponViewModel>();
        public List<CuponViewModel> CuponesCocteles { get; set; } = new List<CuponViewModel>();
        public List<CuponViewModel> CuponesEspeciales { get; set; } = new List<CuponViewModel>();
    }
}
