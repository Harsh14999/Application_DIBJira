using System;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Text;
using DFM_BPM.App_Code.DAL;

namespace DFM_BPM.App_Code.Helpers
{
    /// <summary>
    /// SMTP email sending + AES-256 encryption for sensitive config values (password, user).
    /// Encryption key is derived from the application's machine key section (auto-generated,
    /// machine-bound) so cipher text cannot be used on a different machine without the same key.
    /// </summary>
    public static class EmailHelper
    {
        // ── Encryption / Decryption ───────────────────────────────────────
        // Uses MachineKey.Protect so the key is stored in Web.config <machineKey>
        // and is isolated per application.

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            byte[] data = Encoding.UTF8.GetBytes(plainText);
            byte[] protected_ = System.Web.Security.MachineKey.Protect(data, "EmailConfig");
            return Convert.ToBase64String(protected_);
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return "";
            try
            {
                byte[] data = Convert.FromBase64String(cipherText);
                byte[] plain = System.Web.Security.MachineKey.Unprotect(data, "EmailConfig");
                return plain == null ? "" : Encoding.UTF8.GetString(plain);
            }
            catch
            {
                return "";
            }
        }

        // ── SMTP Send ─────────────────────────────────────────────────────

        /// <summary>
        /// Send an email using config from dbo.EmailConfig.
        /// Actioner is placed in "To"; audience in "CC".
        /// Returns the EmailLog LogID.
        /// </summary>
        public static int SendPetEmail(
            string triggerEvent,
            int petFormId,
            string toAddress,
            string ccAddress,
            string subject,
            string htmlBody,
            string sentBy)
        {
            bool enabled = string.Equals(EmailDAL.GetConfigValue("SmtpEnabled"), "true", StringComparison.OrdinalIgnoreCase);
            if (!enabled)
            {
                // Log as skipped (system disabled)
                return EmailDAL.LogEmail(toAddress, ccAddress, subject, htmlBody,
                    "Disabled", "Email sending is disabled in configuration.", triggerEvent, petFormId, sentBy);
            }

            string host     = EmailDAL.GetConfigValue("SmtpHost");
            int    port     = 587;
            int.TryParse(EmailDAL.GetConfigValue("SmtpPort"), out port);
            bool   ssl      = string.Equals(EmailDAL.GetConfigValue("SmtpEnableSsl"), "true", StringComparison.OrdinalIgnoreCase);
            string user     = EmailDAL.GetConfigValue("SmtpUser");
            string pwdCiph  = EmailDAL.GetConfigValue("SmtpPassword");
            string pwd      = Decrypt(pwdCiph);
            string fromAddr = EmailDAL.GetConfigValue("SmtpFromAddress");
            string fromName = EmailDAL.GetConfigValue("SmtpFromName");

            int logId = EmailDAL.LogEmail(toAddress, ccAddress, subject, htmlBody,
                "Pending", null, triggerEvent, petFormId, sentBy);

            try
            {
                var msg = new MailMessage();
                msg.From = new MailAddress(fromAddr, fromName);
                msg.Subject = subject;
                msg.Body = htmlBody;
                msg.IsBodyHtml = true;

                foreach (string addr in toAddress.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string a = addr.Trim();
                    if (!string.IsNullOrEmpty(a)) msg.To.Add(a);
                }
                if (!string.IsNullOrEmpty(ccAddress))
                {
                    foreach (string addr in ccAddress.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string a = addr.Trim();
                        if (!string.IsNullOrEmpty(a)) msg.CC.Add(a);
                    }
                }

                using (var smtp = new SmtpClient(host, port))
                {
                    smtp.EnableSsl = ssl;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    if (!string.IsNullOrEmpty(user))
                        smtp.Credentials = new NetworkCredential(user, pwd);
                    smtp.Send(msg);
                }
                EmailDAL.UpdateLogStatus(logId, "Sent", null);
            }
            catch (Exception ex)
            {
                EmailDAL.UpdateLogStatus(logId, "Failed", ex.Message);
            }
            return logId;
        }

        // ── Email body builder ────────────────────────────────────────────

        public static string BuildPetEmailBody(
            string eventTitle,
            string actionBy,
            string actionComments,
            DataRow pet,
            DataRow[] lineItems,
            DataRow[] history)
        {
            var sb = new StringBuilder();
            string appName = System.Configuration.ConfigurationManager.AppSettings["AppName"] ?? "DFM BPM";
            string appUrl  = System.Configuration.ConfigurationManager.AppSettings["AppBaseUrl"] ?? "";
            int    petId   = pet["PetFormID"] == DBNull.Value ? 0 : Convert.ToInt32(pet["PetFormID"]);
            string refNo   = pet["PetRefNo"]  == DBNull.Value ? "#" + petId : pet["PetRefNo"].ToString();

            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'/>");
            sb.Append("<style>body{font-family:Arial,sans-serif;font-size:13px;color:#1e293b;}");
            sb.Append(".hdr{background:#1a3c5e;color:#fff;padding:16px 24px;border-radius:8px 8px 0 0;}");
            sb.Append(".sec{padding:14px 24px;} .lbl{font-weight:700;color:#475569;font-size:.85em;} ");
            sb.Append(".val{color:#1e293b;} table.det{border-collapse:collapse;width:100%;margin-bottom:12px;} ");
            sb.Append("table.det th{background:#f1f5f9;padding:6px 10px;border:1px solid #e2e8f0;font-size:.82em;} ");
            sb.Append("table.det td{padding:6px 10px;border:1px solid #e2e8f0;font-size:.82em;} ");
            sb.Append(".badge{display:inline-block;padding:2px 8px;border-radius:10px;font-size:.78em;font-weight:700;}");
            sb.Append("</style></head><body>");

            // Header
            sb.AppendFormat("<div class='hdr'><h2 style='margin:0;'>{0}</h2><p style='margin:4px 0 0;opacity:.8;'>{1}</p></div>",
                System.Web.HttpUtility.HtmlEncode(eventTitle),
                System.Web.HttpUtility.HtmlEncode(appName));

            // Action summary
            sb.Append("<div class='sec' style='background:#fef9c3;border-bottom:1px solid #fde047;'>");
            sb.AppendFormat("<p><span class='lbl'>Action By:</span> <span class='val'>{0}</span></p>",
                System.Web.HttpUtility.HtmlEncode(actionBy));
            if (!string.IsNullOrEmpty(actionComments))
                sb.AppendFormat("<p><span class='lbl'>Comments:</span> <span class='val'>{0}</span></p>",
                    System.Web.HttpUtility.HtmlEncode(actionComments));
            sb.Append("</div>");

            // PET Header details
            sb.Append("<div class='sec'><h3 style='color:#1a3c5e;margin-top:0;'>PET Details</h3>");
            sb.Append("<table class='det'><tbody>");
            AppendRow(sb, "PET Reference", refNo);
            AppendRow(sb, "Status",        S(pet, "Status"));
            AppendRow(sb, "Title",         S(pet, "Title"));
            AppendRow(sb, "Project (JIRA)",S(pet, "ProjectID"));
            AppendRow(sb, "Type",          S(pet, "CapexOpexType"));
            AppendRow(sb, "Budget Source", S(pet, "BudgetSourceID"));
            AppendRow(sb, "Requestor",     S(pet, "CreatedBy"));
            AppendRow(sb, "Reviewer",      S(pet, "ReviewerUsername"));
            AppendRow(sb, "Approver",      S(pet, "ApproverUsername"));
            AppendRow(sb, "Submitted",     pet["SubmittedDate"] == DBNull.Value ? "" : Convert.ToDateTime(pet["SubmittedDate"]).ToString("dd-MMM-yyyy HH:mm"));
            sb.Append("</tbody></table></div>");

            // Line Items
            if (lineItems != null && lineItems.Length > 0)
            {
                sb.Append("<div class='sec'><h3 style='color:#1a3c5e;margin-top:0;'>Line Items</h3>");
                sb.Append("<table class='det'><thead><tr>");
                foreach (string h in new[] { "#","Head","Topic","Vendor","Cost Type","Currency","Units","Unit Price","AED Amt","Cont%","Final AED","GL#" })
                    sb.AppendFormat("<th>{0}</th>", System.Web.HttpUtility.HtmlEncode(h));
                sb.Append("</tr></thead><tbody>");
                decimal grandTotal = 0m;
                foreach (DataRow lr in lineItems)
                {
                    decimal final = lr["FinalAmtLCY"] == DBNull.Value ? 0m : Convert.ToDecimal(lr["FinalAmtLCY"]);
                    grandTotal += final;
                    sb.Append("<tr>");
                    foreach (string c in new[] {"SerialNo","ExpHead","Topic","VendorName","CostType","BaseCurrency","Units","UnitPrice","AmtLCY","ContingencyPct","FinalAmtLCY","GLNumber"})
                        sb.AppendFormat("<td>{0}</td>", System.Web.HttpUtility.HtmlEncode(lr[c] == DBNull.Value ? "" : lr[c].ToString()));
                    sb.Append("</tr>");
                }
                sb.AppendFormat("<tr><td colspan='10' style='text-align:right;font-weight:700;'>Grand Total (AED)</td><td colspan='2' style='font-weight:700;color:#1a3c5e;'>{0}</td></tr>",
                    grandTotal.ToString("N2"));
                sb.Append("</tbody></table></div>");
            }

            // Workflow History
            if (history != null && history.Length > 0)
            {
                sb.Append("<div class='sec'><h3 style='color:#1a3c5e;margin-top:0;'>Workflow History</h3>");
                sb.Append("<table class='det'><thead><tr><th>Date</th><th>Action</th><th>By</th><th>From</th><th>To</th><th>Comments</th></tr></thead><tbody>");
                foreach (DataRow hr in history)
                {
                    sb.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td></tr>",
                        hr["ActionDate"] == DBNull.Value ? "" : Convert.ToDateTime(hr["ActionDate"]).ToString("dd-MMM-yyyy HH:mm"),
                        Enc(hr, "Action"), Enc(hr, "ActionBy"), Enc(hr, "FromStatus"), Enc(hr, "ToStatus"), Enc(hr, "Comments"));
                }
                sb.Append("</tbody></table></div>");
            }

            // Footer
            if (!string.IsNullOrEmpty(appUrl))
                sb.AppendFormat("<div class='sec'><a href='{0}/Forms/PetWorkflow.aspx?id={1}' style='background:#2563eb;color:#fff;padding:8px 18px;border-radius:6px;text-decoration:none;font-weight:700;'>Open in {2}</a></div>",
                    appUrl.TrimEnd('/'), petId, System.Web.HttpUtility.HtmlEncode(appName));
            sb.Append("<div style='padding:10px 24px;font-size:.78em;color:#94a3b8;border-top:1px solid #e2e8f0;'>This is an automated notification from " + System.Web.HttpUtility.HtmlEncode(appName) + ". Please do not reply.</div>");
            sb.Append("</body></html>");
            return sb.ToString();
        }

        private static void AppendRow(StringBuilder sb, string label, string value)
        {
            sb.AppendFormat("<tr><td class='lbl'>{0}</td><td class='val'>{1}</td></tr>",
                System.Web.HttpUtility.HtmlEncode(label),
                System.Web.HttpUtility.HtmlEncode(value ?? ""));
        }

        private static string S(DataRow r, string col) { return r[col] == DBNull.Value ? "" : r[col].ToString(); }
        private static string Enc(DataRow r, string col) { return System.Web.HttpUtility.HtmlEncode(r[col] == DBNull.Value ? "" : r[col].ToString()); }
    }
}
