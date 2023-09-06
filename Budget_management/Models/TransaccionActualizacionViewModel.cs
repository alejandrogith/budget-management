using Microsoft.AspNetCore.Mvc.Rendering;

namespace Budget_management.Models
{
    public class TransaccionActualizacionViewModel : TransaccionCrearViewModel
    {
        public int CuentaAnteriorId { get; set; }
        public decimal MontoAnterior { get; set; }

        public string UrlRetorno { get; set; }
    }
}
