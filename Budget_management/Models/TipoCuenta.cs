using Budget_management.Validaciones;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Budget_management.Models
{
    public class TipoCuenta
    {
        public int Id { get; set; }
        [Remote(action: "VerificarExisteTipoCuenta",controller:"TiposCuentas",
            AdditionalFields =nameof(Id))]
        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string Nombre { get; set; }
        public int UsuarioId { get; set; }
        public int Orden { get; set; }
    }
}
