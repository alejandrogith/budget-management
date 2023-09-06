using Budget_management.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Budget_management.Servicios
{

    public interface IRepositorioCuentas
    {
        Task Actualizar(CuentaViewModel cuentavm);
        Task Borrar(int id);
        Task<IEnumerable<Cuenta>> Buscar(int usuarioId);
        Task Crear(CuentaViewModel cuenta);
        Task<Cuenta> ObtenerPorId(int id, int usuarioId);
    }
    public class RepositorioCuentas: IRepositorioCuentas
    {
        private readonly string _conectionString;

        public RepositorioCuentas(IConfiguration configuration)
        {
            _conectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task Crear(CuentaViewModel cuenta)
        {


            using var connection = new SqlConnection(_conectionString);
            var id = await connection.QuerySingleAsync<int>(@"Insert into TbCuentas (Nombre,TipoCuentaId,Descripcion,Balance)
                                                            values (@Nombre,@TipoCuentaId,@Descripcion,@Balance);
                                                            Select  SCOPE_IDENTITY();", cuenta);

            cuenta.Id = id;

        }

        public async Task<IEnumerable<Cuenta>> Buscar(int usuarioId) {

            using var connection = new SqlConnection(_conectionString);
            return await connection.QueryAsync<Cuenta>(@"Select Cu.Id, Cu.Nombre,Cu.Balance,Tc.Nombre as TipoCuenta
                                                            from TbCuentas as Cu
                                                            inner join TbTiposCuentas AS Tc
                                                            on TC.Id= Cu.TipoCuentaId
                                                            WHERE Tc.UsuarioId = @UsuarioId
                                                            Order by Tc.Orden", new { usuarioId });
        }


        public async Task<Cuenta> ObtenerPorId(int id, int usuarioId)
        {
            using var connection = new SqlConnection(_conectionString);
            return await connection.QueryFirstOrDefaultAsync<Cuenta>(
                @"Select Cu.Id, Cu.Nombre,Cu.Balance,Cu.Descripcion,tc.Id as 'TipoCuentaId'
                from TbCuentas as Cu
                inner join TbTiposCuentas AS Tc
                on TC.Id= Cu.TipoCuentaId
                WHERE Tc.UsuarioId = @UsuarioId AND Cu.Id = @Id;", new {id, usuarioId });
        }

        public async Task Actualizar(CuentaViewModel cuentavm)
        {
            using var connection = new SqlConnection(_conectionString);
             await connection.ExecuteAsync(@"UPDATE TbCuentas
                                    SET Nombre = @Nombre, Balance = @Balance, Descripcion = @Descripcion,
                                    TipoCuentaId = @TipoCuentaId
                                    WHERE Id = @Id;", cuentavm);
        }

        public async Task Borrar(int id) {
            using var connection = new SqlConnection(_conectionString);
            await connection.ExecuteAsync(@"DELETE TbCuentas
                                    WHERE Id = @Id;", new { id});
        }

    }
}
