using Budget_management.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Budget_management.Servicios
{

    public interface IRepositorioCategorias
    {
        Task Actualizar(Categoria categoria);
        Task Borrar(int id);
        Task Crear(Categoria categoria);
        Task<IEnumerable<Categoria>> Obtener(int usuarioId, PaginacionViewModel paginacion);
        Task<IEnumerable<Categoria>> Obtener(int usuarioId, TipoOperacion tipoOperacionid);
        Task<int> Contar(int usuarioId);
        Task<Categoria> ObtenerPorId(int id, int usuarioId);
    }
    public class RepositorioCategorias: IRepositorioCategorias
    {
        private readonly string _connectionString;
        public RepositorioCategorias(IConfiguration configuration)
        {
            _connectionString  = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task Crear(Categoria categoria)
        {
            using var connection = new SqlConnection(_connectionString);
            var id = await connection.QuerySingleAsync<int>(@"INSERT INTO TbCategorias (Nombre,TipoOperacionId,UsuarioId)
                                                            Values (@Nombre,@TipoOperacionId,@UsuarioId)
                                                            Select  SCOPE_IDENTITY();", categoria);

            categoria.Id = id;
        }


        public async Task<IEnumerable<Categoria>> Obtener(int usuarioId,PaginacionViewModel paginacion)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<Categoria>(
                @$"SELECT * 
                  FROM TbCategorias 
                  WHERE UsuarioId = @usuarioId
                  ORDER BY Nombre
                  OFFSET {paginacion.RecordsASaltar} ROWS FETCH NEXT {paginacion.RecordsPorPagina} 
                    ROWS ONLY", new { usuarioId });
        }


        public async Task<int> Contar(int usuarioId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM TbCategorias WHERE UsuarioId=@UsuarioId", 
                new { usuarioId });
        }


        public async Task<IEnumerable<Categoria>> Obtener(int usuarioId,TipoOperacion tipoOperacionid)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<Categoria>(@"Select * FROM TbCategorias 
                                                           WHERE UsuarioId=@UsuarioId
                                                            AND TipoOperacionId=@TipoOperacionId;",
                                                           new { usuarioId, tipoOperacionid });
        }

        public async Task<Categoria> ObtenerPorId(int id,int usuarioId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Categoria>(@"Select * FROM TbCategorias 
                                                                         WHERE UsuarioId=@UsuarioId and
                                                                         Id = @Id;",
                                                                         new { id,usuarioId });
        }


        public async Task Actualizar(Categoria categoria)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(@"UPDATE TbCategorias 
                                                              SET Nombre=@Nombre,
                                                              TipoOperacionId=@TipoOperacionId
                                                              WHERE Id = @Id;",
                                                              categoria );
        }



        public async Task Borrar(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(@"Delete TbCategorias
                                            WHERE Id = @Id;",
                                            new { id });
        }




    }
}
