using Budget_management.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Budget_management.Servicios
{
    public interface IRepositorioTiposCuentas
    {
        Task  Crear(TipoCuenta tipoCuenta);
        Task<bool> Existe(string nombre, int usuarioId, int id);

        Task<IEnumerable<TipoCuenta>> Obtener(int usuarioId);

        Task Actualizar(TipoCuenta tipoCuenta);
        Task<TipoCuenta> ObtenerPorId(int id, int usuarioId);
        Task Borrar(int id);
        Task Ordenar(IEnumerable<TipoCuenta> tipoCuentasordenados);
    }

    public class RepositorioTiposCuentas : IRepositorioTiposCuentas {

        private readonly string _configuration;
        public RepositorioTiposCuentas(IConfiguration configuartion)
        {
            _configuration = configuartion.GetConnectionString("DefaultConnection");
        }

        public async Task Crear(TipoCuenta tipoCuenta) 
        {

            tipoCuenta.Orden= await  ObtenerOrdenContador();

            using var connection = new SqlConnection(_configuration);
            var id = await connection.QuerySingleAsync<int>($@"INSERT INTO TbTiposCuentas (Nombre,UsuarioId,Orden)
                                                    values (@Nombre,@UsuarioId,@Orden);
                                                    Select  SCOPE_IDENTITY();", tipoCuenta);

            tipoCuenta.Id = id;
            
        }

        private async Task<int> ObtenerOrdenContador() {

            var query = "Select COALESCE(MAX(Orden),0)+1  From TbTiposCuentas ";
            using var connection = new SqlConnection(_configuration);
            var OrdenContador = await connection.QueryFirstOrDefaultAsync<int>(query);

            return OrdenContador;
        }


        public async Task<bool> Existe(string nombre, int usuarioId,int id=0)
        {
            using var connection = new SqlConnection(_configuration);
            var existe = await connection.QueryFirstOrDefaultAsync<int>(@"Select 1 
                                                                        From TbTiposCuentas 
                                                                        where Nombre=@Nombre and UsuarioId=@UsuarioId AND Id <> @Id;",
                                                                        new {nombre,usuarioId,id });
            return existe == 1;
        }

        public async Task<IEnumerable<TipoCuenta>> Obtener(int usuarioId) {
            using var connection = new SqlConnection(_configuration);

            return await connection.QueryAsync<TipoCuenta>(@"select Id, Nombre, Orden
                                                             from TbTiposCuentas
                                                             where UsuarioId = @UsuarioId
                                                             ORDER BY Orden",
                                                             new {usuarioId });
        }

        public async Task Actualizar(TipoCuenta tipoCuenta) {
            using var connection = new SqlConnection(_configuration);
            await connection.ExecuteAsync(@"Update TbTiposCuentas
                                            set Nombre =@Nombre
                                            where Id=@Id", tipoCuenta);
        }

        public async Task<TipoCuenta> ObtenerPorId(int id,int usuarioId)
        {
            using var connection = new SqlConnection(_configuration);
           return await connection.QueryFirstOrDefaultAsync<TipoCuenta>(@"select Id,Nombre,Orden
                                                            from TbTiposCuentas
                                                            where Id=@Id and UsuarioId=@UsuarioId", 
                                                            new {id,usuarioId });
        }

        public async Task Borrar(int id)
        {
            using var connection = new SqlConnection(_configuration);
            await connection.ExecuteAsync(@"DELETE TbTiposCuentas WHERE Id=@Id", 
                                            new { id });
        }

        public async Task Ordenar(IEnumerable<TipoCuenta> tipoCuentasordenados)
        {
            var query = "UPDATE TbTiposCuentas SET Orden=@Orden where Id=@Id;";
            using var connection = new SqlConnection(_configuration);
            await connection.ExecuteAsync(query,tipoCuentasordenados);
        }



    }
}
