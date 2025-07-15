namespace PantallaChecador.Modelos
{
    public class Employee
    {

        public Employee() { }

        public int id_employee { get; set; }
        public string assistance_code { get; set; }
        public string supervisor { get; set; }
        public string name { get; set; }
        public string paternal_last_name { get; set; }
        public string maternal_last_name { get; set; }
        public string picture { get; set; }
        public string curp { get; set; }
        public string birthday { get; set; }
        public string area_display_name { get; set; }
        public int fk_employee_type { get; set; }
        public string employee_type_display_name { get; set; }
        public int fk_gender { get; set; }
        public string gender_display_name { get; set; }
        public int fk_area { get; set; }
        public string date_admission { get; set; }
        public int id_status { get; set; }
        public int id_employee_fingerprint { get; set; }
        public int id_employee_fingerprint_thumb { get; set; }
        public int id_employee_fingerprint_index { get; set; }
        public string status_display_name { get; set; }
        public byte[] fingerprint { get; set; }
        public byte[] fingerprint_thumb { get; set; }
        public byte[] fingerprint_index { get; set; }

    }

}
