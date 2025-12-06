using System;
using System.Collections.Generic;
using System.Text;

namespace Paralelismo.App.DTOs
{
    public class ClienteDTO
    {
        //  Nombre,Apellido,Email,Cedula
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Cedula { get; set; }
    }
}
