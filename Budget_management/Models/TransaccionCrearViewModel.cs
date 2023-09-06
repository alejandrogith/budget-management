using Microsoft.AspNetCore.Mvc.Rendering;

namespace Budget_management.Models
{
    public class TransaccionCrearViewModel:Transaccion
    {
        public IEnumerable<SelectListItem> Cuentas { get; set; } =new List<SelectListItem>();
        public IEnumerable<SelectListItem> Categorias { get; set; } = new List<SelectListItem>();



    }
}
