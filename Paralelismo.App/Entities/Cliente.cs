using System;
using System.Collections.Generic;
using System.Text;

namespace Paralelismo.App.Entities
{
    public class Cliente
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Cedula
        {
            get; set;
        }
    }
}
