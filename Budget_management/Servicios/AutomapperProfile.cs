using AutoMapper;
using Budget_management.Models;

namespace Budget_management.Servicios
{
    public class AutomapperProfile:Profile
    {
        public AutomapperProfile()
        {
            CreateMap<Cuenta,CuentaViewModel>();
            CreateMap<TransaccionActualizacionViewModel, Transaccion>().ReverseMap();
        }
    }
}
