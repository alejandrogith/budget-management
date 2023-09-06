using Budget_management.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Budget_management.Servicios
{
    public interface IRepositorioTransacciones
    {
        Task Crear(Transaccion transaccion);
        Task Actualizar(Transaccion transaccion, decimal montoAnterior, int cuentaAnterior);
        Task<Transaccion> ObtenerPorId(int id, int usuarioId);
        Task Borrar(Transaccion transaccion);
        Task<IEnumerable<Transaccion>> ObtenerPorCuentaId(ObtenerTransaccionesPorCuenta modelo);
        Task<IEnumerable<Transaccion>> ObtenerPorUsuarioId(ParametroObtenerTransaccionesPorUsuario modelo);
        Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerPorSemana(ParametroObtenerTransaccionesPorUsuario modelo);
        Task<IEnumerable<ResultadoObtenerPorMes>> ObtenerPorMes(int usuarioId, int año);
    }

    public class RepositorioTransacciones : IRepositorioTransacciones
    {
        private readonly string _connectionString;
        public RepositorioTransacciones(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }



        public async Task Crear(Transaccion transaccion)
        {
            using var connection = new SqlConnection(_connectionString);
            var id = await connection.QuerySingleAsync<int>(@"INSERT INTO TbTransacciones(UsuarioId,FechaTransaccion,Monto,TipoOperacionId,CategoriaId,CuentaId,Nota)
                                                            values(@UsuarioId,@FechaTransaccion,ABS(@Monto),@TipoOperacionId,@CategoriaId,@CuentaId,@Nota);
                                                            Select  SCOPE_IDENTITY();", transaccion);
            //Actualizar el monto de la cuenta
            await connection.ExecuteAsync(@"Update TbCuentas
                                            set Balance += @Monto 
                                            Where Id = @CuentaId;", transaccion);

            transaccion.Id = id;
        }


        public async Task Actualizar(Transaccion transaccion, decimal montoAnterior, int cuentaAnteriorId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(@"Update TbTransacciones
                                                SET Monto= ABS(@Monto),FechaTransaccion=@FechaTransaccion,
                                                CategoriaId=CategoriaId,CuentaId=@CuentaId,Nota=@Nota
                                                Where Id= @id;", transaccion);
            //Revertir transaccion anterior
            await connection.ExecuteAsync(@"Update TbCuentas
                                            SET Balance -=@MontoAnterior
                                            Where Id= @CuentaAnteriorId;", new { montoAnterior, cuentaAnteriorId });

            //Realizar nueva transaccion 
            await connection.ExecuteAsync(@"Update TbCuentas
                                            SET Balance +=@Monto
                                            Where Id= @CuentaId;", new { transaccion.Monto, transaccion.CuentaId });

        }


        public async Task<Transaccion> ObtenerPorId(int id, int usuarioId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Transaccion>(
                @"SELECT *   FROM 
                TbTransacciones 
                where Id =@Id AND
                UsuarioId=@UsuarioId;", new { id, usuarioId });


        }


        public async Task<IEnumerable<Transaccion>> ObtenerPorCuentaId(
            ObtenerTransaccionesPorCuenta modelo)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<Transaccion>(
                @"SELECT TR.Id,TR.Monto,TR.FechaTransaccion,C.Nombre AS Categoria,
                CU.Nombre AS Cuenta, C.TipoOperacionId
                FROM TbTransacciones as TR
                INNER JOIN TbCategorias as C
                on C.Id=TR.CategoriaId
                INNER JOIN TbCuentas as CU
                ON CU.Id=TR.CuentaId
                WHERE TR.CuentaId=@CuentaId AND 
                TR.UsuarioID=@UsuarioId AND
                TR.FechaTransaccion BETWEEN @FechaInicio AND @FechaFin;", modelo);


        }


        public async Task<IEnumerable<Transaccion>> ObtenerPorUsuarioId(
       ParametroObtenerTransaccionesPorUsuario modelo)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<Transaccion>(
                @"SELECT TR.Id,TR.Monto,TR.FechaTransaccion,C.Nombre AS Categoria,
                CU.Nombre AS Cuenta, C.TipoOperacionId
                FROM TbTransacciones as TR
                INNER JOIN TbCategorias as C
                on C.Id=TR.CategoriaId
                INNER JOIN TbCuentas as CU
                ON CU.Id=TR.CuentaId
                WHERE TR.UsuarioID=@UsuarioId AND
                TR.FechaTransaccion BETWEEN @FechaInicio AND @FechaFin
                ORDER BY TR.FechaTransaccion DESC;", modelo);


        }


        public async Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerPorSemana(
            ParametroObtenerTransaccionesPorUsuario modelo)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<ResultadoObtenerPorSemana>(@"
                   SELECT DATEDIFF(d,@fechaInicio,FechaTransaccion) / 7 + 1 as Semana,
                    SUM(Monto) as Monto, CAT.TipoOperacionId
                    FROM TbTransacciones as Transac
                    INNER JOIN TbCategorias AS CAT
                    ON CAT.Id =Transac.CategoriaId
                    where Transac.UsuarioId =@usuarioId AND
                    FechaTransaccion BETWEEN @fechaInicio and @fechaFin
                    Group by DATEDIFF(d,@fechaInicio,FechaTransaccion) / 7 + 1,CAT.TipoOperacionId"
                    , modelo);
        }

        public async Task<IEnumerable<ResultadoObtenerPorMes>> ObtenerPorMes(
            int usuarioId,int año)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<ResultadoObtenerPorMes>(@"
                    SELECT MONTH(FechaTransaccion) as Mes,
                    SUM(Transac.Monto) as Monto,Cat.TipoOperacionId
                    FROM TbTransacciones as Transac
                    inner join TbCategorias as Cat
                    on Cat.Id=Transac.CategoriaId
                    where Transac.UsuarioId=@usuarioId
                    AND YEAR(FechaTransaccion) =@Año
                    group by  MONTH(FechaTransaccion),Cat.TipoOperacionId"
                    , new { usuarioId, año }); 
        }

        public async Task Borrar(Transaccion transaccion)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(@"Update TbCuentas
                                            set Balance -=@Monto
                                            where Id=@CuentaId", transaccion);

            await connection.ExecuteAsync(@"Delete TbTransacciones
                                            where Id=@Id", transaccion);


        }



    }
}
