namespace Budget_management.Models
{
    public class ReporteMensualViewModel
    {
        public decimal Ingresos => TransaccionesPorMes.Sum(x => x.Ingreso);
        public decimal Gastos => TransaccionesPorMes.Sum(x => x.Gasto);
        public decimal Total => Ingresos - Gastos;
        public IEnumerable<ResultadoObtenerPorMes> TransaccionesPorMes { get; set; }
        public int Año { get; set; }
    }
}
