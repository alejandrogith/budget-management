namespace Budget_management.Models
{
    public class PaginacionRespuesta
    {
        public int Pagina { get; set; }
        public int RecordsPorPagina { get; set; }
        public int CantidadTotalRecords { get; set; }
        public int CantidadTotalDePaginas => (int)Math.Ceiling((double)CantidadTotalRecords/RecordsPorPagina);
    
        public string BaseURL { get; set; }
    }


    public class PaginacionRespuesta<T>:PaginacionRespuesta
    {
        public IEnumerable<T> Elementos { get; set; }

    }

}
