using Budget_management.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Budget_management.Servicios
{
    public interface IRepositoriosUsuarios
    {
        Task<Usuario> BuscarUsuarioPorEmail(string emailNormalizado);
        Task<int> CrearUsuario(Usuario usuario);
    }

    public class RepositoriosUsuarios: IRepositoriosUsuarios
    {
        private readonly string _connectionString;
        public RepositoriosUsuarios(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<int> CrearUsuario(Usuario usuario)
        {
         //   usuario.EmailNormalizado= usuario.Email.ToUpper();
            using var connection = new SqlConnection(_connectionString);
            var usuarioId = await connection.QuerySingleAsync<int>(@"INSERT INTO TbUsuarios (Email,EmailNormalizado,PasswordHash)
                                                            VALUES(@Email,@EmailNormalizado,@PasswordHash);
                                                            Select  SCOPE_IDENTITY();", usuario);

 


            await connection.ExecuteAsync(@"INSERT  INTO TbTiposCuentas(Nombre,UsuarioId,Orden)
                                            Values('Efectivo',@UsuarioId,1),
                                            ('CuentasDeBanco',@UsuarioId,2),
                                            ('Tarjetas',@UsuarioId,3);

                                            INSERT INTO TbCuentas(Nombre,Balance,TipoCuentaId)
                                            SELECT Nombre,0,Id
                                            FROM TbTiposCuentas
                                            WHERE UsuarioId=@UsuarioId;
                                            
                                            

                                            INSERT INTO TbCategorias(Nombre,TipoOperacionId,UsuarioId)
                                            VALUES
                                            ('Libros',2,@UsuarioId),
                                            ('Salario',1,@UsuarioId),
                                            ('Mesada',1,@UsuarioId),
                                            ('Comida',2,@UsuarioId)", new { usuarioId });

            return usuarioId;
        }

        public async Task<Usuario> BuscarUsuarioPorEmail(string emailNormalizado)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<Usuario>(
                @"select * from TbUsuarios
                WHERE EmailNormalizado = @EmailNormalizado;", 
                new { emailNormalizado });
        }



    }
}
