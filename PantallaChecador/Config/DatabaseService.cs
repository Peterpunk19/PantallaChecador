namespace PantallaChecador.Config
{
    public class DatabaseService
    {
        public DatabaseService() { }
        public static string GetConnectionString(string env)
        {
            string server = "localhost";
            int port = 9904;
            string database = "fingerprint";
            string userId = "MYSQL_USER";
            string password = "MYSQL_PASSWORD";

            if (env == "prod")
            {
                server = "192.168.1.105";
                port = 3306;
                database = "fingerprint_db";
                userId = "admin";
                password = "password";
            }

            return $"Server={server};Port={port};Database={database};User Id={userId};Password={password};";
        }
    }
}
