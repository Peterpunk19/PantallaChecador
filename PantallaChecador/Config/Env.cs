using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PantallaChecador.Config
{
    public class Env
    {
        public Env() { }
        public static string GetApiUrl(string env)
        {
            string api = "http://localhost:4040/api/";

            if (env == "prod")
            {
                api = "http://192.168.1.105:4004/";
            }

            return api;
        }
    }
}
