using AutoMapper;
using Budget_management.Models;
using Budget_management.Servicios;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace Budget_management.Controllers
{
 
    public class TransaccionesController : Controller
    {
        private readonly IRepositorioTransacciones _repositorioTransacciones;
        private readonly IRepositorioCategorias _repositorioCategorias;
        private readonly IServicioUsuarios _servicioUsuarios;
        private readonly IRepositorioCuentas _repositorioCuentas;
        private readonly IMapper _mapper;
        private readonly IServiciosReportes _serviciosReportes;

        public TransaccionesController(IRepositorioTransacciones repositorioTransacciones,
                                 IServicioUsuarios servicioUsuarios,
                                 IRepositorioCuentas repositorioCuentas,
                                 IRepositorioCategorias repositorioCategorias,
                                 IMapper mapper,
                                 IServiciosReportes serviciosReportes)
        {
            _repositorioTransacciones = repositorioTransacciones;
            _servicioUsuarios = servicioUsuarios;
            _repositorioCuentas = repositorioCuentas;
            _repositorioCategorias= repositorioCategorias;
            _mapper = mapper;
            _serviciosReportes = serviciosReportes;
        }



        public async Task<IActionResult> Index(int mes,int año)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();


            var modelo = await _serviciosReportes.ObtenerReporteTransaccionesDetalladas(usuarioId,
                mes,año,ViewBag);



            return View(modelo);
        }


        [HttpGet]
        public async Task<IActionResult> Semanal(int mes,int año) 
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();


            IEnumerable<ResultadoObtenerPorSemana> transaccionesPorSemana =
                await _serviciosReportes.ObtenerReporteSemanal(usuarioId,mes, año, ViewBag);

            var agrupado = transaccionesPorSemana.GroupBy(x => x.Semana)
                .Select(x => new ResultadoObtenerPorSemana()
                {
                    Semana = x.Key,
                    Ingresos = x.Where(x => x.TipoOperacionId == TipoOperacion.Ingreso)
                       .Select(x => x.Monto).FirstOrDefault(),
                    Gastos= x.Where(x => x.TipoOperacionId == TipoOperacion.Gasto)
                       .Select(x => x.Monto).FirstOrDefault()

                }).ToList();


            if (año==0 || mes==0) {
                var hoy = DateTime.Today;
                año = hoy.Year;
                mes=hoy.Month;
            }

            var fechaReferencia = new DateTime(año,mes,1);
            var diasDelMes = Enumerable.Range(1,fechaReferencia.AddMonths(1).AddDays(-1).Day);

            var diasSegmentados = diasDelMes.Chunk(7).ToList();

            for (int i=0; i < diasSegmentados.Count(); i++) {

                var semana = i + 1;
                var fechaInicio = new DateTime(año,mes,diasSegmentados[i].First());
                var fechaFin = new DateTime(año, mes, diasSegmentados[i].Last());
                var grupoSemana = agrupado.FirstOrDefault(x => x.Semana == semana);

                if (grupoSemana is null)
                {
                    agrupado.Add(new ResultadoObtenerPorSemana()
                    {
                        Semana = semana,
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin,

                    });

                }
                else {
                    grupoSemana.FechaInicio = fechaInicio;
                    grupoSemana.FechaFin = fechaFin;
                
                }

            }

            agrupado= agrupado.OrderByDescending(x => x.Semana).ToList();


            var modelo = new ReporteSemanalViewModel();
            modelo.TransaccionesPorSemana = agrupado;
            modelo.FechaReferencia = fechaReferencia;

            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> Mensual(int año)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();

            if (año == 0)
                año = DateTime.Today.Year;

            var transaccionesPorMes = await _repositorioTransacciones.ObtenerPorMes(usuarioId,año);

            var transaccionesAgrupadas = transaccionesPorMes.GroupBy(x => x.Mes)
                .Select(x => new ResultadoObtenerPorMes()
                {
                    Mes=x.Key,
                    Ingreso = x.Where(x => x.TipoOperacionId == TipoOperacion.Ingreso)
                       .Select(x => x.Monto).FirstOrDefault(),
                    Gasto = x.Where(x => x.TipoOperacionId == TipoOperacion.Gasto)
                       .Select(x => x.Monto).FirstOrDefault()
                }).ToList();

            for (var mes=1; mes<=12; mes++) {
                var transaccion = transaccionesAgrupadas.FirstOrDefault(x=>x.Mes==mes);
                var fechaReferencia = new DateTime(año,mes,1);
                if (transaccion is null)
                {
                    transaccionesAgrupadas.Add(new ResultadoObtenerPorMes()
                    {
                        Mes = mes,
                        FechaReferencia = fechaReferencia,
                    });
                }
                else 
                {
                    transaccion.FechaReferencia = fechaReferencia;
                }


            }

            transaccionesAgrupadas= transaccionesAgrupadas.OrderByDescending(x => x.Mes).ToList();

            var modelo= new ReporteMensualViewModel() 
            { 
              Año=año,
              TransaccionesPorMes=transaccionesAgrupadas
            };

            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> ExcelReporte()
        {


            return View();
        }


        [HttpGet]
        public async Task<FileResult> ExportarExcelPorMes(int mes,int año)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();

            var fechaInicio = new DateTime(año,mes,1);
            var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);

            var transacciones = await _repositorioTransacciones.ObtenerPorUsuarioId(
                new ParametroObtenerTransaccionesPorUsuario
                {
                    UsuarioId=usuarioId,
                    FechaInicio=fechaInicio,
                    FechaFin=fechaFin
                });

            var nombreArchivo = $"Manejo Presupuesto - {fechaInicio.ToString("MMM yyyy")}.xlsx";


            return GenerarExcel(nombreArchivo,transacciones);
        }


        [HttpGet]
        public async Task<FileResult> ExportarExcelPorAño( int año)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();

            var fechaInicio = new DateTime(año, 1, 1);
            var fechaFin = fechaInicio.AddYears(1).AddDays(-1);

            var transacciones = await _repositorioTransacciones.ObtenerPorUsuarioId(
                new ParametroObtenerTransaccionesPorUsuario
                {
                    UsuarioId = usuarioId,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin
                });

            var nombreArchivo = $"Manejo Presupuesto - {fechaInicio.ToString("yyyy")}.xlsx";


            return GenerarExcel(nombreArchivo, transacciones);
        }

        [HttpGet]
        public async Task<FileResult> ExportarExcelTodo()
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();

            var fechaInicio = DateTime.Today.AddYears(-100);
            var fechaFin = DateTime.Today.AddYears(1000);




            var transacciones = await _repositorioTransacciones.ObtenerPorUsuarioId(
                new ParametroObtenerTransaccionesPorUsuario
                {
                    UsuarioId = usuarioId,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin
                });

            var nombreArchivo = $"Manejo Presupuesto - {fechaInicio.ToString("dd-MM-yyyy")}.xlsx";


            return GenerarExcel(nombreArchivo, transacciones);
        }


        private FileResult GenerarExcel(string nombreArchivo,
            IEnumerable<Transaccion> transacciones)
        {
            var dataTable = new DataTable("Transacciones");
            dataTable.Columns.AddRange(new DataColumn[] 
            { 
                new DataColumn("Fecha"),
                new DataColumn("Cuenta"),
                new DataColumn("Categoria"),
                new DataColumn("Nota"),
                new DataColumn("Monto"),
                new DataColumn("Ingreso/Gasto"),
            });

            foreach (var transaccion in transacciones) { 
                dataTable.Rows.Add(transaccion.FechaTransaccion,
                    transaccion.Cuenta,
                    transaccion.Categoria,
                    transaccion.Nota,
                    transaccion.Monto,
                    transaccion.TipoOperacionId);
            }

            using (var wb=new XLWorkbook()) {
                wb.Worksheets.Add(dataTable);

                using (var stream= new MemoryStream()) { 

                    wb.SaveAs(stream);

                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        nombreArchivo);
                }

            }

        } 

        [HttpGet]
        public async Task<IActionResult> Calendario()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerTransaccionesCalendario(
            DateTime start,DateTime end)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();

            var transacciones = await _repositorioTransacciones.ObtenerPorUsuarioId(
                new ParametroObtenerTransaccionesPorUsuario
                {
                    UsuarioId = usuarioId,
                    FechaInicio = start,
                    FechaFin = end
                });

            var eventosCalendario = transacciones
                .Select(x=> new EventoCalendario()
                { 
                    Title=x.Monto.ToString("N"),
                    Start = x.FechaTransaccion.ToString("yyyy-MM-dd"),
                    End = x.FechaTransaccion.ToString("yyyy-MM-dd"),
                    Color= (x.TipoOperacionId==TipoOperacion.Gasto) ? "Red" : null
                });


            return Json(eventosCalendario);
        }


        [HttpGet]
        public async Task<JsonResult> ObtenerTransaccionesPorFecha(DateTime fecha)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();

            var transacciones = await _repositorioTransacciones.ObtenerPorUsuarioId(
                new ParametroObtenerTransaccionesPorUsuario
                {
                    UsuarioId = usuarioId,
                    FechaInicio = fecha,
                    FechaFin = fecha
                });


            return Json(transacciones);
        }



        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var modelo = new TransaccionCrearViewModel();
            modelo.Cuentas =await ObtenerCuentas(usuarioId);
            modelo.Categorias = await ObtenerCategorias(usuarioId,modelo.TipoOperacionId);
            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(TransaccionCrearViewModel transaccionvm)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();

            if (!ModelState.IsValid) {

                transaccionvm.Cuentas = await ObtenerCuentas(usuarioId);
                transaccionvm.Categorias = await ObtenerCategorias(usuarioId, transaccionvm.TipoOperacionId);
                return View(transaccionvm);
            }

            var cuenta = await _repositorioCuentas.ObtenerPorId(transaccionvm.CuentaId,usuarioId);

            if (cuenta is null) {
                return RedirectToAction("NoEncontrado","Home");
            }

            var categoria = await _repositorioCategorias.ObtenerPorId(transaccionvm.CategoriaId, usuarioId);

            if (categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            transaccionvm.UsuarioId = usuarioId;

            if (transaccionvm.TipoOperacionId==TipoOperacion.Gasto) {

                transaccionvm.Monto *= -1;

            }

            await _repositorioTransacciones.Crear(transaccionvm);

            return RedirectToAction(nameof(Index));
        }


        private async Task<IEnumerable<SelectListItem>> ObtenerCuentas(int usuarioId) {
            var cuentas = await _repositorioCuentas.Buscar(usuarioId);

            return  cuentas.Select(x => new SelectListItem(x.Nombre,x.Id.ToString())); ;
        }


        private async Task<IEnumerable<SelectListItem>> ObtenerCategorias(int usuarioId, 
            TipoOperacion tipoOperacion)
        {
            var categorias = await _repositorioCategorias.Obtener(usuarioId,tipoOperacion);


            return categorias.Select(x => new SelectListItem(x.Nombre, x.Id.ToString())); ;
        }


        [HttpPost]
        public async Task<IActionResult> ObtenerCategorias([FromBody] TipoOperacion tipoOperacion)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var categoria = await ObtenerCategorias(usuarioId,tipoOperacion);

            return Ok(categoria);
        }


        [HttpGet]
        public async Task<IActionResult> Editar(int id,string urlRetorno=null)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var transaccion = await _repositorioTransacciones.ObtenerPorId(id,usuarioId);

            if (transaccion is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var modelo = _mapper.Map<TransaccionActualizacionViewModel>(transaccion);

            modelo.MontoAnterior = modelo.Monto;

            if (modelo.TipoOperacionId==TipoOperacion.Gasto) {
                modelo.MontoAnterior = modelo.Monto *= -1;
            }

            modelo.CuentaAnteriorId = transaccion.CuentaId;
            modelo.Categorias = await ObtenerCategorias(usuarioId,transaccion.TipoOperacionId);
            modelo.Cuentas = await ObtenerCuentas(usuarioId);
            modelo.UrlRetorno = urlRetorno;

            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(TransaccionActualizacionViewModel transaccionvm)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();

            if (!ModelState.IsValid)
            {

                transaccionvm.Cuentas = await ObtenerCuentas(usuarioId);
                transaccionvm.Categorias = await ObtenerCategorias(usuarioId, transaccionvm.TipoOperacionId);
                return View(transaccionvm);
            }

            var cuenta = await _repositorioCuentas.ObtenerPorId(transaccionvm.CuentaId, usuarioId);

            if (cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var categoria = await _repositorioCategorias.ObtenerPorId(transaccionvm.CategoriaId, usuarioId);

            if (categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            transaccionvm.UsuarioId = usuarioId;

            if (transaccionvm.TipoOperacionId == TipoOperacion.Gasto)
            {

                transaccionvm.Monto *= -1;

            }

            await _repositorioTransacciones.Actualizar(transaccionvm,
                transaccionvm.MontoAnterior,transaccionvm.CuentaAnteriorId);

            if (!string.IsNullOrEmpty(transaccionvm.UrlRetorno))
                return LocalRedirect(transaccionvm.UrlRetorno);



            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> Borrar(int id, string urlRetorno = null)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();

            var transaccion = await _repositorioTransacciones.ObtenerPorId(id, usuarioId);


            if (transaccion is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }



            if (transaccion.TipoOperacionId == TipoOperacion.Gasto)
            {

                transaccion.Monto *= -1;

            }

            await _repositorioTransacciones.Borrar(transaccion);

            if (!string.IsNullOrEmpty(urlRetorno))
                return LocalRedirect(urlRetorno);

            return RedirectToAction(nameof(Index));
        }





    }
}
