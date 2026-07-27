using System;
using System.Data;
using System.Text;
using System.Web;

namespace DFM_BPM.App_Code.Helpers
{
    public static class ExcelHelper
    {
        public static void ExportToExcel(HttpResponse response, DataTable dt, string filename)
        {
            ExportDataTable(dt, filename, response);
        }

        /// <summary>Export a DataTable to Excel (.xls tab-delimited). Alias for ExportToExcel.</summary>
        public static void ExportDataTable(DataTable dt, string filename, HttpResponse response)
        {
            response.Clear();
            response.ContentType = "application/vnd.ms-excel";
            response.AddHeader("Content-Disposition",
                "attachment; filename=" + filename + ".xls");
            response.ContentEncoding = Encoding.UTF8;
            response.BinaryWrite(Encoding.UTF8.GetPreamble());

            var sb = new StringBuilder();
            foreach (DataColumn col in dt.Columns)
            {
                sb.Append(col.ColumnName.Replace("\t", " ").Replace("\r\n", " "));
                sb.Append('\t');
            }
            sb.AppendLine();
            foreach (DataRow row in dt.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    string val = item == null || item == DBNull.Value ? "" : item.ToString();
                    val = val.Replace("\t", " ").Replace("\r\n", " ").Replace("\n", " ");
                    sb.Append(val); sb.Append('\t');
                }
                sb.AppendLine();
            }
            response.Write(sb.ToString());
            response.End();
        }

        /// <summary>Export a GridView to Excel.</summary>
        public static void ExportGridView(System.Web.UI.WebControls.GridView gv, string filename, HttpResponse response)
        {
            var dt = new DataTable();
            foreach (System.Web.UI.WebControls.DataControlField col in gv.Columns)
                dt.Columns.Add(col.HeaderText);
            foreach (System.Web.UI.WebControls.GridViewRow row in gv.Rows)
            {
                if (row.RowType != System.Web.UI.WebControls.DataControlRowType.DataRow) continue;
                var dr = dt.NewRow();
                for (int i = 0; i < gv.Columns.Count; i++)
                    dr[i] = row.Cells[i].Text.Replace("&nbsp;", "");
                dt.Rows.Add(dr);
            }
            ExportDataTable(dt, filename, response);
        }

        /// <summary>Export a DataTable as a true, properly-quoted comma-separated CSV file.</summary>
        public static void ExportCsv(DataTable dt, string filename, HttpResponse response)
        {
            response.Clear();
            response.ContentType = "text/csv";
            response.AddHeader("Content-Disposition", "attachment; filename=" + filename + ".csv");
            response.ContentEncoding = Encoding.UTF8;
            response.BinaryWrite(Encoding.UTF8.GetPreamble());

            var sb = new StringBuilder();
            var headerCells = new string[dt.Columns.Count];
            for (int i = 0; i < dt.Columns.Count; i++) headerCells[i] = CsvEscape(dt.Columns[i].ColumnName);
            sb.AppendLine(string.Join(",", headerCells));

            var rowCells = new string[dt.Columns.Count];
            foreach (DataRow row in dt.Rows)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    object val = row[i];
                    rowCells[i] = CsvEscape(val == null || val == DBNull.Value ? "" : val.ToString());
                }
                sb.AppendLine(string.Join(",", rowCells));
            }
            response.Write(sb.ToString());
            response.End();
        }

        private static string CsvEscape(string val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            // Mitigate CSV/Excel formula injection (OWASP): neutralize leading formula-trigger characters
            // so a value like "=cmd|'/c calc'!A1" isn't executed as a formula when opened in a spreadsheet.
            if (val.Length > 0 && (val[0] == '=' || val[0] == '+' || val[0] == '-' || val[0] == '@'))
                val = "'" + val;
            if (val.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            return val;
        }
    }
}
