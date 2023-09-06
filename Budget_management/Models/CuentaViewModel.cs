using Microsoft.AspNetCore.Mvc.Rendering;

namespace Budget_management.Models
{
    public class CuentaViewModel:Cuenta
    {

        public IEnumerable<SelectListItem> TiposCuentas { get; set; } = new List<SelectListItem>();
    }
}
