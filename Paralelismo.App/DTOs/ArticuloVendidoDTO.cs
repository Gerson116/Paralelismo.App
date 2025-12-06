using System;
using System.Collections.Generic;
using System.Text;

namespace Paralelismo.App.DTOs
{
    public class ArticuloVendidoDTO
    {
        //  MarcaId,    FechaVencimiento,   NumeroDeLote
        public int MarcaId { get; set; }
        public string FechaVencimiento { get; set; }
        public string NumeroDeLote { get; set; } = string.Empty;
    }
}
