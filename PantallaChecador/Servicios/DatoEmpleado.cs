using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using PantallaChecador.Modelos;
using System.Data.Common;
using MySql.Data.MySqlClient;
using System.Windows;
using System.Drawing.Printing;

namespace PantallaChecador.Servicios
{
    public class DatoEmpleado
    {
        public static List<Employee> MuestraEmpleados(string assistanceCode, int pageNumber, int pageSize)
        {
            List<Employee> listaEmpleados = new List<Employee>();

            try
            {

                using (var conn = new MySqlConnection(Config.DatabaseService.GetConnectionString("prod")))
                {
                    conn.Open();
                    string query = @"SELECT
                            e.id_employee,
                            e.fk_user,
                            e.assistance_code,
                            e.supervisor,
                            e.name                                                                   AS employee_name,
                            e.paternal_last_name,
                            e.maternal_last_name,
                            e.picture,
                            e.curp,
                            e.birthday,
                            e.fk_employee_type,
                            et.display_name                                                          AS employee_type_display_name,
                            e.fk_gender,
                            g.display_name                                                           AS gender_display_name,
                            e.fk_area,
                            a.display_name                                                           AS area_display_name,
                            e.date_admission,
                            e.date_hiring,
                            e.date_dismiss,
                            MAX(IF(f.name = 'thumb', ef.id_employee_fingerprint, 0)) AS id_employee_fingerprint_thumb,
                            MAX(IF(f.name = 'thumb', ef.fingerprint, NULL))          AS fingerprint_thumb,
                            MAX(IF(f.name = 'index_finger', ef.id_employee_fingerprint, 0)) AS id_employee_fingerprint_index,
                            MAX(IF(f.name = 'index_finger', ef.fingerprint, NULL))   AS fingerprint_index,
                            e.status,
                            s.display_name                                                           AS status_display_name,
                            e.created_at
                        FROM employee e
                                 INNER JOIN employee_fingerprint ef on e.id_employee = ef.fk_employee
                                 INNER JOIN finger f on ef.fk_finger = f.id_finger
                                 INNER JOIN employee_type et on e.fk_employee_type = et.id_employee_type
                                 INNER JOIN gender g on e.fk_gender = g.id_gender
                                 INNER JOIN area a on e.fk_area = a.id_area
                                 INNER join status s on e.status = s.id_status AND e.status = 1";

                    if (!string.IsNullOrEmpty(assistanceCode))
                    {
                        query += " WHERE e.assistance_code LIKE @assistance_code";
                    }

                    query += " GROUP BY e.id_employee, e.updated_at ORDER BY e.updated_at LIMIT @pageSize OFFSET @offset;";

                    using (var command = new MySqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(assistanceCode))
                        {
                            command.Parameters.AddWithValue("@assistance_code", "%" + assistanceCode + "%");
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@assistance_code", DBNull.Value);
                        }

                        command.Parameters.AddWithValue("@pageSize", pageSize);
                        command.Parameters.AddWithValue("@offset", (pageNumber - 1) * pageSize);

                        using (DbDataReader dr = command.ExecuteReader())
                        {
                            if (dr.HasRows)
                            {
                                while (dr.Read())
                                {
                                    Employee emp = new Employee();
                                    emp.id_employee = int.Parse(dr["id_employee"].ToString());
                                    emp.supervisor = dr["supervisor"].ToString();
                                    emp.name = dr["employee_name"].ToString();
                                    emp.paternal_last_name = dr["paternal_last_name"].ToString();
                                    emp.maternal_last_name = dr["maternal_last_name"].ToString();
                                    emp.assistance_code = dr["assistance_code"].ToString();
                                    emp.curp = dr["curp"].ToString();
                                    emp.birthday = dr["birthday"].ToString();
                                    emp.fk_area = int.Parse(dr["fk_area"].ToString());
                                    emp.area_display_name = dr["area_display_name"].ToString();
                                    emp.date_admission = dr["date_admission"].ToString();
                                    emp.fk_employee_type = int.Parse(dr["fk_employee_type"].ToString());
                                    emp.employee_type_display_name = dr["employee_type_display_name"].ToString();
                                    emp.fk_gender = int.Parse(dr["fk_gender"].ToString());
                                    emp.id_status = int.Parse(dr["status"].ToString());
                                    emp.status_display_name = dr["status_display_name"].ToString();
                                    emp.picture = dr["picture"].ToString();

                                    emp.fingerprint_thumb = null;
                                    if (dr["fingerprint_thumb"].ToString() != "")
                                    {
                                        emp.id_employee_fingerprint_thumb = int.Parse(dr["id_employee_fingerprint_thumb"].ToString());
                                        emp.fingerprint_thumb = (byte[])dr["fingerprint_thumb"];
                                    }

                                    emp.fingerprint_index = null;
                                    if (dr["fingerprint_index"].ToString() != "")
                                    {
                                        emp.id_employee_fingerprint_index = int.Parse(dr["id_employee_fingerprint_index"].ToString());
                                        emp.fingerprint_index = (byte[])dr["fingerprint_index"];
                                    }


                                    listaEmpleados.Add(emp);

                                }
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocurrió un error al consultar Empleados: " + ex.Message, "Error");
            }

            return listaEmpleados;
        }

        public static string Checar(string dia, string hora, string fecha, string numero)
        {
            string respuesta = "";

            using (var connection = new MySqlConnection(Config.DatabaseService.GetConnectionString("prod")))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "Checar";

                    command.Parameters.AddWithValue("@Dia", dia);
                    command.Parameters.AddWithValue("@Hora", hora);
                    command.Parameters.AddWithValue("@Fecha", fecha);
                    command.Parameters.AddWithValue("@Numero", numero);

                    using (DbDataReader dr = command.ExecuteReader())
                    {
                        if (dr.HasRows == true)
                        {
                            while (dr.Read())
                            {
                                respuesta = dr["Res"].ToString();
                            }
                        }
                    }
                }
            }

            return respuesta;
        }

        public static int SaveEmployeeFingerprint(Registry registry)
        {
            int res = 0;

            try
            {
                using (var conn = new MySqlConnection(Config.DatabaseService.GetConnectionString("prod")))
                {
                    conn.Open();
                    string query = @"INSERT INTO registry (fk_employee, fk_employee_fingerprint, checkin) 
                        VALUES (@fk_employee, @fk_employee_fingerprint, @checkin)";

                    using (MySqlCommand command = new MySqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@fk_employee", registry.fk_employee);
                        command.Parameters.AddWithValue("@fk_employee_fingerprint", registry.fk_employee_fingerprint);
                        command.Parameters.AddWithValue("@checkin", registry.checkin);

                        res = command.ExecuteNonQuery();

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar huellas del empleado: " + ex.Message, "Error en Alta");
            }

            return res;
        }

        public static int UpdateEmployee(int idEmployee)
        {
            int rowsAffected = 0;

            try
            {
                using (var conn = new MySqlConnection(Config.DatabaseService.GetConnectionString("prod")))
                {
                    conn.Open();

                    string query = @"UPDATE employee SET 
                                updated_at = now()
                            WHERE id_employee = @id_employee";

                    using (MySqlCommand command = new MySqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@id_employee", idEmployee);
                       

                        rowsAffected = command.ExecuteNonQuery();

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar el empleado: " + ex.Message, "Error en Actualización");
            }

            return rowsAffected;

        }
    }
}
