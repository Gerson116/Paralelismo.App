using System;
using System.Collections.Generic;
using System.Text;

namespace Paralelismo.App.Entities
{
    public class ArticuloVendido
    {
        public int Id { get; set; }
        public int MarcaId { get; set; }
        public string? FechaVencimiento { get; set; }
        public string? NumeroDeLote { get; set; }
    }
}
