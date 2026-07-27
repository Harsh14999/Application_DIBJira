using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace DFM_BPM.App_Code.DAL
{
    public static class OracleDb
    {
        public static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["OracleConnection"].ConnectionString; }
        }

        public static DataTable Query(string sql, params OracleParameter[] ps)
        {
            var dt = new DataTable();
            using (var conn = new OracleConnection(ConnectionString))
            using (var cmd  = new OracleCommand(sql, conn))
            {
                cmd.CommandTimeout = 180;
                cmd.BindByName     = true;
                if (ps != null) cmd.Parameters.AddRange(ps);
                using (var da = new OracleDataAdapter(cmd)) da.Fill(dt);
            }
            return dt;
        }

        public static DataRow QueryRow(string sql, params OracleParameter[] ps)
        {
            var dt = Query(sql, ps);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static object Scalar(string sql, params OracleParameter[] ps)
        {
            using (var conn = new OracleConnection(ConnectionString))
            using (var cmd  = new OracleCommand(sql, conn))
            {
                cmd.CommandTimeout = 120;
                cmd.BindByName     = true;
                if (ps != null) cmd.Parameters.AddRange(ps);
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

        public static OracleParameter P(string name, object value)
        {
            return new OracleParameter(name, value ?? System.DBNull.Value);
        }
    }
}
