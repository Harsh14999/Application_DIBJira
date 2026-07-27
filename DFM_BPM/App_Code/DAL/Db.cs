using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace DFM_BPM.App_Code.DAL
{
    public static class Db
    {
        public static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["DFM_BPMConnection"].ConnectionString; }
        }

        public static DataTable Query(string sql, params SqlParameter[] ps)
        {
            var dt = new DataTable();
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 150;
                if (ps != null) cmd.Parameters.AddRange(ps);
                using (var da = new SqlDataAdapter(cmd)) da.Fill(dt);
            }
            return dt;
        }

        public static DataRow QueryRow(string sql, params SqlParameter[] ps)
        {
            var dt = Query(sql, ps);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static int Exec(string sql, params SqlParameter[] ps)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 150;
                if (ps != null) cmd.Parameters.AddRange(ps);
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        public static object Scalar(string sql, params SqlParameter[] ps)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd  = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 150;
                if (ps != null) cmd.Parameters.AddRange(ps);
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

        public static DataTable QuerySP(string spName, params SqlParameter[] ps)
        {
            var dt = new DataTable();
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd  = new SqlCommand(spName, conn))
            {
                cmd.CommandType    = CommandType.StoredProcedure;
                cmd.CommandTimeout = 150;
                if (ps != null) cmd.Parameters.AddRange(ps);
                using (var da = new SqlDataAdapter(cmd)) da.Fill(dt);
            }
            return dt;
        }

        public static DataSet QuerySPMulti(string spName, params SqlParameter[] ps)
        {
            var ds = new DataSet();
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd  = new SqlCommand(spName, conn))
            {
                cmd.CommandType    = CommandType.StoredProcedure;
                cmd.CommandTimeout = 150;
                if (ps != null) cmd.Parameters.AddRange(ps);
                using (var da = new SqlDataAdapter(cmd)) da.Fill(ds);
            }
            return ds;
        }

        public static int ExecSP(string spName, params SqlParameter[] ps)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd  = new SqlCommand(spName, conn))
            {
                cmd.CommandType    = CommandType.StoredProcedure;
                cmd.CommandTimeout = 150;
                if (ps != null) cmd.Parameters.AddRange(ps);
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        public static SqlParameter P(string name, object value)
        {
            return new SqlParameter(name, value ?? System.DBNull.Value);
        }
    }
}
