namespace PantallaChecador.Modelos
{
    public class Registry
    {

        public Registry() { }

        public int id_registry { get; set; }
        public int fk_employee { get; set; }
        public int fk_employee_fingerprint { get; set; }
        public string checkin { get; set; }

    }

}
