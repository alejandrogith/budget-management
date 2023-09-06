using AutoMapper;
using Budget_management.Models;
using Budget_management.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Globalization;

namespace Budget_management.Controllers
{
    public class CuentasController : Controller
    {
        private readonly IRepositorioTiposCuentas _repositorioTiposCuentas;
        private readonly IServicioUsuarios _servicioUsuarios;
        private readonly IRepositorioCuentas _repositorioCuentas;
        private readonly IRepositorioTransacciones _repositorioTransacciones;
        private readonly IMapper _mapper;
        private readonly IServiciosReportes _serviciosReportes;

        public CuentasController(IRepositorioTiposCuentas repositorioTiposCuentas,
                                 IRepositorioTransacciones repositorioTransacciones,
                                 IServicioUsuarios servicioUsuarios,
                                 IRepositorioCuentas repositorioCuentas,
                                 IMapper mapper,
                                 IServiciosReportes serviciosReportes)
        {
            _repositorioTiposCuentas = repositorioTiposCuentas;
            _repositorioTransacciones = repositorioTransacciones;
            _servicioUsuarios = servicioUsuarios;
            _repositorioCuentas = repositorioCuentas;
            _mapper = mapper;
            _serviciosReportes = serviciosReportes;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var cuentasConTipoCuenta = await _repositorioCuentas.Buscar(usuarioId);
            var modelo= cuentasConTipoCuenta
                        .GroupBy(x => x.TipoCuenta)
                        .Select(grupo => new IndiceCuentasViewModel 
                        {
                            TipoCuenta=grupo.Key,
                            Cuentas=grupo.AsEnumerable()
                        }).ToList();

            return View(modelo);
        }


        [HttpGet]
        public async Task<IActionResult> Detalle(int id,int mes,int año) {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var cuenta = await _repositorioCuentas.ObtenerPorId(id, usuarioId);
            
            if (cuenta is null)
                return RedirectToAction("NoEncontrado", "Home");


            ViewBag.Cuenta = cuenta.Nombre;

            var modelo =  await _serviciosReportes
                .ObtenerReporteTransaccionesDetalladasPorCuenta(usuarioId,id,mes,año,ViewBag);

            return View(modelo);
        }



        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var modelo = new CuentaViewModel();
            modelo.TiposCuentas = await ObtenerTiposCuentas(usuarioId);
            return View(modelo);
        }


        [HttpPost]
        public async Task<IActionResult> Crear(  CuentaViewModel cuenta)
        {

         
            
            var usuarioId=_servicioUsuarios.ObtenerUsuarioId();
            var tipoCuenta = await _repositorioTiposCuentas.ObtenerPorId(cuenta.TipoCuentaId,usuarioId);

            if (tipoCuenta is null)
                return RedirectToAction("NoEncontrado", "Home");

            if (!ModelState.IsValid) {

                cuenta.TiposCuentas = await ObtenerTiposCuentas(usuarioId);
                return View(cuenta);
            }

            await _repositorioCuentas.Crear(cuenta);
            

            return RedirectToAction(nameof(Index));
           
        }


        private async Task<IEnumerable<SelectListItem>> ObtenerTiposCuentas(int usuarioId) {

            var tiposCuentas = await _repositorioTiposCuentas.Obtener(usuarioId);
            return tiposCuentas.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
        }


        [HttpGet]
        public async Task<IActionResult> Editar(int id) {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var cuenta = await _repositorioCuentas.ObtenerPorId(id, usuarioId);

            if (cuenta is null)
                return RedirectToAction("NoEncontrado", "Home");

            var modelo = _mapper.Map<CuentaViewModel>(cuenta);



            modelo.TiposCuentas = await ObtenerTiposCuentas(usuarioId);


            return View(modelo);

        }


        [HttpPost]
        public async Task<IActionResult> Editar(CuentaViewModel cuentavm)
        { 
            var usuarioId= _servicioUsuarios.ObtenerUsuarioId();
            var cuenta= await _repositorioCuentas.ObtenerPorId(cuentavm.Id, usuarioId);

            if (cuenta is null)
                return RedirectToAction("NoEncontrado", "Home");

            var tipoCuenta = _repositorioTiposCuentas.ObtenerPorId(cuentavm.TipoCuentaId,usuarioId);

            if (cuenta is null)
                return RedirectToAction("NoEncontrado", "Home");

            await _repositorioCuentas.Actualizar(cuentavm);

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Borrar(int id) {

            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var cuenta = await _repositorioCuentas.ObtenerPorId(id, usuarioId);

            if (cuenta is null)
                return RedirectToAction("NoEncontrado", "Home");


            return View(cuenta);

        }

        [HttpPost]
        public async Task<IActionResult> BorrarCuenta(int id)
        {

            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var cuenta = await _repositorioCuentas.ObtenerPorId(id, usuarioId);

            if (cuenta is null)
                return RedirectToAction("NoEncontrado", "Home");



            await _repositorioCuentas.Borrar(id);

            return RedirectToAction(nameof(Index));

        }





    }
}
