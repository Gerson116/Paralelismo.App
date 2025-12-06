using System;
using System.Collections.Generic;
using System.Text;

namespace Paralelismo.App.DTOs
{
    public class FacturaDTO
    {
        // Monto,Cantidad,ITBIS,Descuento
        public decimal Monto { get; set; }
        public int Cantidad { get; set; }
        public decimal ITBIS { get; set; }
        public decimal Descuento { get; set; }
    }
}
