using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoFormales
{

    //Interfaz para los parsers
    public interface IParser
    {
        //Metodo para saber si una cadena es valida para el parser
        bool Parse(string input);
    }
}
