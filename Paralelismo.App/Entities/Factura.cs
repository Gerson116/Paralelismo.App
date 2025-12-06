using System;
using System.Collections.Generic;
using System.Text;

namespace Paralelismo.App.Entities
{
    public class Factura
    {
        public int Id { get; set; }
        public decimal Monto { get; set; }
        public int Cantidad { get; set; }
        public decimal ITBIS { get; set; }
        public decimal Descuento { get; set; }
    }
}
