using System;
using System.Data;
using System.Data.SqlClient;

namespace DFM_BPM.App_Code.DAL
{
    public static class EmailDAL
    {
        // ── Config CRUD ────────────────────────────────────────────────────

        public static DataTable GetAllConfig()
        {
            return Db.Query("SELECT ConfigID, ConfigKey, ConfigValue, IsEncrypted, UpdatedBy, UpdatedDate FROM dbo.EmailConfig ORDER BY ConfigKey");
        }

        public static string GetConfigValue(string key)
        {
            var r = Db.QueryRow("SELECT ConfigValue FROM dbo.EmailConfig WHERE ConfigKey=@k", Db.P("@k", key));
            return r == null || r["ConfigValue"] == DBNull.Value ? "" : r["ConfigValue"].ToString();
        }

        public static void SetConfigValue(string key, string value, bool isEncrypted, string updatedBy)
        {
            int exists = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM dbo.EmailConfig WHERE ConfigKey=@k", Db.P("@k", key)));
            if (exists == 0)
                Db.Exec("INSERT INTO dbo.EmailConfig (ConfigKey,ConfigValue,IsEncrypted,UpdatedBy,UpdatedDate) VALUES(@k,@v,@e,@u,GETDATE())",
                    Db.P("@k", key), Db.P("@v", value ?? ""), Db.P("@e", isEncrypted), Db.P("@u", updatedBy));
            else
                Db.Exec("UPDATE dbo.EmailConfig SET ConfigValue=@v,IsEncrypted=@e,UpdatedBy=@u,UpdatedDate=GETDATE() WHERE ConfigKey=@k",
                    Db.P("@v", value ?? ""), Db.P("@e", isEncrypted), Db.P("@u", updatedBy), Db.P("@k", key));
        }

        // ── Email Log ─────────────────────────────────────────────────────

        public static int LogEmail(string toAddress, string ccAddress, string subject, string body,
            string status, string errorMessage, string triggerEvent, int? petFormId, string sentBy)
        {
            object id = Db.Scalar(@"
                INSERT INTO dbo.EmailLog (ToAddress,CcAddress,Subject,Body,Status,ErrorMessage,TriggerEvent,PetFormID,SentBy)
                OUTPUT INSERTED.LogID
                VALUES (@to,@cc,@sub,@body,@st,@err,@ev,@pid,@sb)",
                Db.P("@to", toAddress ?? ""),
                Db.P("@cc", ccAddress ?? ""),
                Db.P("@sub", subject ?? ""),
                Db.P("@body", body ?? ""),
                Db.P("@st", status ?? "Pending"),
                Db.P("@err", (object)(errorMessage ?? "") ),
                Db.P("@ev", triggerEvent ?? ""),
                Db.P("@pid", petFormId.HasValue ? (object)petFormId.Value : DBNull.Value),
                Db.P("@sb", sentBy ?? ""));
            return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
        }

        public static void UpdateLogStatus(int logId, string status, string errorMessage)
        {
            Db.Exec("UPDATE dbo.EmailLog SET Status=@s, ErrorMessage=@e WHERE LogID=@id",
                Db.P("@s", status), Db.P("@e", errorMessage ?? ""), Db.P("@id", logId));
        }

        public static DataTable GetEmailLog(int? petFormId = null, int topN = 200)
        {
            string sql = "SELECT TOP " + topN + " LogID,SentDate,ToAddress,CcAddress,Subject,Status,ErrorMessage,TriggerEvent,PetFormID,SentBy FROM dbo.EmailLog";
            if (petFormId.HasValue)
                sql += " WHERE PetFormID=@p ORDER BY LogID DESC";
            else
                sql += " ORDER BY LogID DESC";
            return petFormId.HasValue
                ? Db.Query(sql, Db.P("@p", petFormId.Value))
                : Db.Query(sql);
        }

        public static DataRow GetEmailLogDetail(int logId)
        {
            return Db.QueryRow("SELECT * FROM dbo.EmailLog WHERE LogID=@id", Db.P("@id", logId));
        }

        // ── User email lookup ─────────────────────────────────────────────

        public static string GetUserEmail(string username)
        {
            if (string.IsNullOrEmpty(username)) return null;
            var r = Db.QueryRow("SELECT Email FROM dbo.AppUsers WHERE Username=@u", Db.P("@u", username));
            return r == null || r["Email"] == DBNull.Value ? null : r["Email"].ToString();
        }
    }
}
