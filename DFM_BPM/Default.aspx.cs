using System;
using System.Data;
using System.Text;
using System.Web.UI;
using DFM_BPM.App_Code.DAL;
using DFM_BPM.App_Code.Helpers;

namespace DFM_BPM
{
    public partial class DefaultPage : Page
    {
        protected bool IsAdmin { get { return AuthHelper.IsAdmin; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) LoadDashboard();
        }

        private void LoadDashboard()
        {
            LoadLastSync();
            LoadKPIs();
            LoadRecentProjects();
            LoadSpendRequests();
            LoadInvoices();
            LoadPendingApprovals();
            LoadBudgetLineSummary();
        }

        private void LoadLastSync()
        {
            try
            {
                DataRow row = Db.QueryRow(
                    "SELECT TOP 1 CONVERT(VARCHAR,EndTime,120) AS T FROM dbo.SyncLog WHERE Status='Success' ORDER BY SyncID DESC");
                litLastSync.Text = row != null ? row["T"].ToString() : "Never";
            }
            catch { litLastSync.Text = "N/A"; }
        }

        private void LoadKPIs()
        {
            try
            {
                DataRow kpi = DashboardDAL.GetDashboardKPIs();
                if (kpi != null)
                {
                    litProjects.Text = Fmt(kpi, "TotalProjects");
                    litPET.Text = Fmt(kpi, "TotalPET");
                    litPending.Text = Fmt(kpi, "TotalPending");
                    litApproved.Text = Fmt(kpi, "TotalApproved");
                    litRejected.Text = Fmt(kpi, "TotalRejected");
                    litCapexBudget.Text = FmtAmt(kpi, "TotalCapexBudget");
                    litOpexBudget.Text = FmtAmt(kpi, "TotalOpexBudget");

                    decimal capex = GetDecimal(kpi, "TotalCapexBudget");
                    decimal opex = GetDecimal(kpi, "TotalOpexBudget");
                    decimal approved = GetDecimal(kpi, "TotalApproved");
                    decimal pending = GetDecimal(kpi, "TotalPending");
                    RenderCapexOpexChart(capex, opex);
                    RenderBudgetChart(capex + opex, approved, pending);
                }
            }
            catch
            {
                RenderCapexOpexChart(0m, 0m);
                RenderBudgetChart(0m, 0m, 0m);
            }
        }

        private void LoadRecentProjects()
        {
            try
            {
                DataTable projects = ProjectDAL.GetProjects();
                gvRecentProjects.DataSource = TakeRows(projects, 6);
                gvRecentProjects.DataBind();

                int active = 0;
                foreach (DataRow row in projects.Rows)
                {
                    if (!projects.Columns.Contains("IsActive") || row["IsActive"] == DBNull.Value || Convert.ToBoolean(row["IsActive"])) active++;
                }
                litActiveProjects.Text = active.ToString("N0");
            }
            catch
            {
                gvRecentProjects.DataSource = null;
                gvRecentProjects.DataBind();
                litActiveProjects.Text = "0";
            }
        }

        private void LoadSpendRequests()
        {
            try
            {
                DataTable requests = WorkflowDAL.GetPetFormsDashboard(null, null, null, null, null, "ALL", AuthHelper.CurrentUserShort);
                gvRecentRequests.DataSource = TakeRows(requests, 6);
                gvRecentRequests.DataBind();
                RenderMonthlySpendChart(requests);
            }
            catch
            {
                gvRecentRequests.DataSource = null;
                gvRecentRequests.DataBind();
                RenderMonthlySpendChart(null);
            }
        }

        private void LoadInvoices()
        {
            try
            {
                DataTable invoices = Db.Query(@"SELECT TOP 6 i.InvoiceID, i.InvoiceNo, i.InvoiceAmount, i.InvoiceStatus, i.PaymentDate,
                                                       bl.VendorName, p.PetRefNo, p.ProjectID
                                                FROM dbo.PetBudgetInvoice i
                                                INNER JOIN dbo.PetBudgetLine bl ON bl.BudgetLineID = i.BudgetLineID
                                                INNER JOIN dbo.PetForm p ON p.PetFormID = bl.PetFormID
                                                WHERE p.Status<>'Deleted'
                                                ORDER BY i.InvoiceID DESC");
                gvRecentInvoices.DataSource = invoices;
                gvRecentInvoices.DataBind();

                DataRow summary = Db.QueryRow(@"SELECT COUNT(1) AS InvoiceCount, ISNULL(SUM(i.InvoiceAmount),0) AS InvoiceAmount
                                                FROM dbo.PetBudgetInvoice i
                                                INNER JOIN dbo.PetBudgetLine bl ON bl.BudgetLineID = i.BudgetLineID
                                                INNER JOIN dbo.PetForm p ON p.PetFormID = bl.PetFormID
                                                WHERE p.Status<>'Deleted'");
                litInvoiceCount.Text = summary == null ? "0" : Fmt(summary, "InvoiceCount");
                litInvoiceAmount.Text = summary == null ? "AED 0" : FmtAmt(summary, "InvoiceAmount");

                  DataTable trend = Db.Query(@"SELECT PeriodKey, Amount FROM (
                                      SELECT TOP 6 CONVERT(CHAR(7), ISNULL(i.PaymentDate, i.CreatedDate), 120) AS PeriodKey,
                                          ISNULL(SUM(i.InvoiceAmount),0) AS Amount
                                      FROM dbo.PetBudgetInvoice i
                                      INNER JOIN dbo.PetBudgetLine bl ON bl.BudgetLineID = i.BudgetLineID
                                      INNER JOIN dbo.PetForm p ON p.PetFormID = bl.PetFormID
                                      WHERE p.Status<>'Deleted'
                                      GROUP BY CONVERT(CHAR(7), ISNULL(i.PaymentDate, i.CreatedDate), 120)
                                      ORDER BY PeriodKey DESC
                                   ) x ORDER BY PeriodKey ASC");
                RenderTrendChart(trend, litInvoiceTrendChart, "slate");
            }
            catch
            {
                gvRecentInvoices.DataSource = null;
                gvRecentInvoices.DataBind();
                litInvoiceCount.Text = "0";
                litInvoiceAmount.Text = "AED 0";
                RenderTrendChart(null, litInvoiceTrendChart, "slate");
            }
        }

        private void LoadPendingApprovals()
        {
            try
            {
                DataTable approvals = WorkflowDAL.GetPetFormsDashboard(null, null, null, null, null,
                    "MYAPPROVAL", AuthHelper.CurrentUserShort);
                StringBuilder sb = new StringBuilder();
                sb.Append("<div class='approval-list'>");
                int count = Math.Min(approvals.Rows.Count, 5);
                for (int i = 0; i < count; i++)
                {
                    DataRow row = approvals.Rows[i];
                    string status = Val(row, "Status");
                    string css = status == "PendingReview" ? "status-review" : "status-pending";
                    string refNo = string.IsNullOrEmpty(Val(row, "PetRefNo")) ? "#" + Val(row, "PetFormID") : Val(row, "PetRefNo");
                    string url = ResolveUrl("~/Forms/PetWorkflow.aspx") + "?id=" + Server.UrlEncode(Val(row, "PetFormID"));
                    sb.Append("<div class='approval-item'>");
                    sb.Append("<div class='approval-icon'><i class='bi bi-lightning-charge'></i></div>");
                    sb.Append("<div><div class='approval-title'><a href='" + url + "'>" + Html(refNo) + "</a></div>");
                    sb.Append("<div class='approval-meta'>" + Html(Val(row, "ProjectID")) + " | " + Html(Val(row, "CreatedBy")) + "</div></div>");
                    sb.Append("<span class='status-pill " + css + "'>" + Html(status) + "</span>");
                    sb.Append("</div>");
                }
                if (count == 0) sb.Append("<div style='color:#94a3b8;padding:10px;'>No pending approvals.</div>");
                sb.Append("</div>");
                litPendingApprovals.Text = sb.ToString();
            }
            catch { litPendingApprovals.Text = "<div style='color:#94a3b8;padding:10px;'>No pending approvals.</div>"; }
        }

        private void LoadBudgetLineSummary()
        {
            try
            {
                DataTable lines = WorkflowDAL.GetBudgetLinesByUser(AuthHelper.CurrentUserShort);
                litMyBudgetLines.Text = lines.Rows.Count.ToString("N0");
            }
            catch { litMyBudgetLines.Text = "0"; }
        }

        protected void btnExportDashboard_Click(object sender, EventArgs e)
        {
            DataTable data = WorkflowDAL.GetPetFormsDashboard(null, null, null, null, null, "ALL", AuthHelper.CurrentUserShort);
            ExcelHelper.ExportDataTable(data, "Finance_Dashboard", Response);
        }

        private void RenderCapexOpexChart(decimal capex, decimal opex)
        {
            decimal total = capex + opex;
            int capexPct = Percent(capex, total);
            int opexPct = Percent(opex, total);
            litCapexOpexChart.Text = "<div class='viz-bars'>" +
                Bar("CAPEX", capexPct, FmtAmt(capex), "") +
                Bar("OPEX", opexPct, FmtAmt(opex), "green") +
                "</div>";
        }

        private void RenderBudgetChart(decimal budget, decimal approved, decimal pending)
        {
            decimal used = approved + pending;
            litBudgetChart.Text = "<div class='viz-bars'>" +
                Bar("Approved", Percent(approved, used), approved.ToString("N0"), "green") +
                Bar("Pending", Percent(pending, used), pending.ToString("N0"), "orange") +
                Bar("Capacity", Percent(used, budget), FmtAmt(used), "") +
                "</div>";
        }

        private void RenderMonthlySpendChart(DataTable requests)
        {
            DataTable chart = new DataTable();
            chart.Columns.Add("PeriodKey", typeof(string));
            chart.Columns.Add("Amount", typeof(decimal));

            for (int i = 5; i >= 0; i--)
            {
                DateTime month = DateTime.Today.AddMonths(-i);
                chart.Rows.Add(month.ToString("MMM"), 0m);
            }

            if (requests != null)
            {
                foreach (DataRow row in requests.Rows)
                {
                    if (!requests.Columns.Contains("CreatedDate") || row["CreatedDate"] == DBNull.Value) continue;
                    DateTime created = Convert.ToDateTime(row["CreatedDate"]);
                    string key = created.ToString("MMM");
                    for (int i = 0; i < chart.Rows.Count; i++)
                    {
                        if (chart.Rows[i]["PeriodKey"].ToString() == key)
                        {
                            chart.Rows[i]["Amount"] = Convert.ToDecimal(chart.Rows[i]["Amount"]) + GetDecimal(row, "TotalRequestedAED");
                            break;
                        }
                    }
                }
            }

            RenderTrendChart(chart, litMonthlySpendChart, "");
        }

        private void RenderTrendChart(DataTable data, System.Web.UI.WebControls.Literal target, string colorClass)
        {
            if (data == null || data.Rows.Count == 0)
            {
                target.Text = "<div class='trend-strip'><div class='trend-bar' style='height:32px;'><span>--</span></div></div>";
                return;
            }

            decimal max = 0m;
            for (int i = 0; i < data.Rows.Count; i++) max = Math.Max(max, GetDecimal(data.Rows[i], "Amount"));
            StringBuilder sb = new StringBuilder();
            sb.Append("<div class='trend-strip'>");
            for (int i = 0; i < data.Rows.Count; i++)
            {
                DataRow row = data.Rows[i];
                int height = Math.Max(24, Percent(GetDecimal(row, "Amount"), max) + 24);
                string key = Val(row, "PeriodKey");
                if (key.Length == 7) key = key.Substring(5, 2) + "/" + key.Substring(2, 2);
                string style = string.IsNullOrEmpty(colorClass) ? "" : "background:linear-gradient(180deg,#94a3b8,#475569);";
                sb.Append("<div class='trend-bar' style='height:" + height + "px;" + style + "'><span>" + Html(key) + "</span></div>");
            }
            sb.Append("</div>");
            target.Text = sb.ToString();
        }

        private static string Bar(string label, int pct, string value, string css)
        {
            return "<div class='viz-row'><div class='viz-label'>" + Html(label) + "</div>" +
                   "<div class='viz-track'><div class='viz-fill " + css + "' style='width:" + Math.Max(4, pct) + "%;'></div></div>" +
                   "<div class='viz-value'>" + Html(value) + "</div></div>";
        }

        private static int Percent(decimal value, decimal total)
        {
            if (total <= 0m) return 0;
            return Math.Min(100, Math.Max(0, (int)Math.Round((value / total) * 100m)));
        }

        private static DataTable TakeRows(DataTable source, int maxRows)
        {
            DataTable result = source.Clone();
            for (int i = 0; i < Math.Min(maxRows, source.Rows.Count); i++) result.ImportRow(source.Rows[i]);
            return result;
        }

        private static string Val(DataRow row, string col)
        {
            return row != null && row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? row[col].ToString() : "";
        }

        private static string Html(string value)
        {
            return System.Web.HttpUtility.HtmlEncode(value ?? "");
        }

        private static string Fmt(DataRow row, string col)
        {
            if (row == null || !row.Table.Columns.Contains(col) || row[col] == DBNull.Value) return "0";
            return Convert.ToInt32(row[col]).ToString("N0");
        }

        private static string FmtAmt(DataRow row, string col)
        {
            return FmtAmt(GetDecimal(row, col));
        }

        private static string FmtAmt(decimal value)
        {
            if (value >= 1000000m) return "AED " + (value / 1000000m).ToString("N2") + "M";
            if (value >= 1000m) return "AED " + (value / 1000m).ToString("N1") + "K";
            return "AED " + value.ToString("N0");
        }

        private static decimal GetDecimal(DataRow row, string col)
        {
            if (row == null || !row.Table.Columns.Contains(col) || row[col] == DBNull.Value) return 0m;
            return Convert.ToDecimal(row[col]);
        }
    }
}
