using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PantallaChecador.Modelos
{
   public class Empleado
    {
        public Empleado() { }
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellidos { get; set; }
        public string Numero { get; set; }
        public string Foto { get; set; }
        public byte[] Huella { get; set; }
    }
}
