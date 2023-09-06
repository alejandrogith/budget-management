using AutoMapper;
using Budget_management.Models;
using Budget_management.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Budget_management.Controllers
{
    public class CategoriasController : Controller
    {

    
        private readonly IServicioUsuarios _servicioUsuarios;
        private readonly IRepositorioCategorias _repositorioCategorias;
        private readonly IMapper _mapper;

        public CategoriasController( IServicioUsuarios servicioUsuarios,
                                 IRepositorioCategorias repositorioCategorias,
                                 IMapper mapper)
        {

            _servicioUsuarios = servicioUsuarios;
            _repositorioCategorias = repositorioCategorias;
            _mapper = mapper;
        }


        public async Task<IActionResult> Index(PaginacionViewModel paginacion)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var categorias = await _repositorioCategorias.Obtener(usuarioId,paginacion);

            var totalCategorias = await _repositorioCategorias.Contar(usuarioId);

            var respuestaVM = new PaginacionRespuesta<Categoria>()
            {
                Elementos = categorias,
                Pagina = paginacion.Pagina,
                RecordsPorPagina = paginacion.RecordsPorPagina,
                CantidadTotalRecords = totalCategorias,
                BaseURL = Url.Action()

            };


            var activarBotonSiguiente = respuestaVM.Pagina < respuestaVM.CantidadTotalDePaginas;


            return View(respuestaVM);
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Crear(Categoria categoria)
        {
            if (!ModelState.IsValid) {
                return View(categoria);
            }

            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            categoria.UsuarioId=usuarioId;
            await _repositorioCategorias.Crear(categoria);

              return RedirectToAction(nameof(Index));

        }


        [HttpGet]
        public  async Task<IActionResult> Editar(int id)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var categoria = await _repositorioCategorias.ObtenerPorId(id,usuarioId);

            if (categoria is null)
                return RedirectToAction("NoEncontrado","Home");


            return View(categoria);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(Categoria categoriaEditar)
        {
            if (!ModelState.IsValid)
            {
                return View(categoriaEditar);
            }

            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var categoria = await _repositorioCategorias.ObtenerPorId(categoriaEditar.Id, usuarioId);

            if (categoria is null)
                return RedirectToAction("NoEncontrado", "Home");

            categoriaEditar.UsuarioId= usuarioId;

            await _repositorioCategorias.Actualizar(categoriaEditar);

            return RedirectToAction(nameof(Index));

        }

        [HttpGet]
        public async Task<IActionResult> Borrar(int id)
        {
            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var categoria = await _repositorioCategorias.ObtenerPorId(id, usuarioId);

            if (categoria is null)
                return RedirectToAction("NoEncontrado", "Home");


            return View(categoria);
        }

        [HttpPost]
        public async Task<IActionResult> BorrarCategoria(int id)
        {

            var usuarioId = _servicioUsuarios.ObtenerUsuarioId();
            var categoria = await _repositorioCategorias.ObtenerPorId(id, usuarioId);

            if (categoria is null)
                return RedirectToAction("NoEncontrado", "Home");


            await _repositorioCategorias.Borrar(id);

            return RedirectToAction(nameof(Index));

        }


    }
}
