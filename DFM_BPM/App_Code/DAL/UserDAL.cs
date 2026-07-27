using System;
using System.Data;
using System.Web;
using System.Web.Security;

namespace DFM_BPM.App_Code.DAL
{
    /// <summary>User and role data access.</summary>
    public static class UserDAL
    {
        // ===== Authentication =====
        public static DataRow ValidateUser(string username, string password)
        {
            DataRow row = Db.QueryRow(
                "SELECT u.UserID, u.Username, u.FullName, u.Email, u.PasswordHash, u.PasswordSalt, r.RoleName, u.IsEnabled " +
                "FROM dbo.AppUsers u INNER JOIN dbo.UserRoles r ON r.RoleID = u.RoleID " +
                "WHERE u.Username = @u",
                Db.P("@u", username));

            if (row == null || row["IsEnabled"].ToString() != "True") return null;
            string hash = Helpers.PasswordHelper.Hash(password, row["PasswordSalt"].ToString());
            return hash == row["PasswordHash"].ToString() ? row : null;
        }

        public static void UpdateLastLogin(string username)
        {
            Db.Exec("UPDATE dbo.AppUsers SET LastLoginDate=GETDATE() WHERE Username=@u", Db.P("@u", username));
        }

        // ===== Users =====
        public static DataTable GetUsers()
        {
            return Db.Query(@"SELECT u.UserID, u.Username, u.FullName, u.Email, u.Department,
                                     r.RoleName, u.IsEnabled, u.CreatedDate, u.LastLoginDate
                              FROM dbo.AppUsers u
                              INNER JOIN dbo.UserRoles r ON r.RoleID = u.RoleID
                              ORDER BY u.Username");
        }

        public static DataRow GetUser(string username)
        {
            return Db.QueryRow(
                "SELECT u.*, r.RoleName FROM dbo.AppUsers u INNER JOIN dbo.UserRoles r ON r.RoleID=u.RoleID WHERE u.Username=@u",
                Db.P("@u", username));
        }

        public static void CreateUser(string username, string fullName, string email, string dept,
                                      int roleId, string password, string createdBy)
        {
            string salt = Helpers.PasswordHelper.GenerateSalt();
            string hash = Helpers.PasswordHelper.Hash(password, salt);
            Db.Exec(@"INSERT INTO dbo.AppUsers(Username, PasswordHash, PasswordSalt, FullName, Email, Department, RoleID, CreatedBy)
                      VALUES(@u, @h, @s, @n, @e, @d, @r, @cb)",
                Db.P("@u", username), Db.P("@h", hash), Db.P("@s", salt),
                Db.P("@n", fullName), Db.P("@e", email ?? ""),
                Db.P("@d", dept ?? ""), Db.P("@r", roleId), Db.P("@cb", createdBy));
            // Default assignment
            Db.Exec("INSERT INTO dbo.UserRoleAssignments(Username, RoleType, CreatedBy) VALUES(@u, 'Requestor', @cb)",
                Db.P("@u", username), Db.P("@cb", createdBy));
        }

        public static void ToggleEnabled(int userId)
        {
            Db.Exec("UPDATE dbo.AppUsers SET IsEnabled = 1 - IsEnabled WHERE UserID=@id", Db.P("@id", userId));
        }

        public static void ResetPassword(int userId, string newPassword)
        {
            string salt = Helpers.PasswordHelper.GenerateSalt();
            string hash = Helpers.PasswordHelper.Hash(newPassword, salt);
            Db.Exec("UPDATE dbo.AppUsers SET PasswordHash=@h, PasswordSalt=@s WHERE UserID=@id",
                Db.P("@h", hash), Db.P("@s", salt), Db.P("@id", userId));
        }

        public static void DeleteUser(int userId)
        {
            // Delete role assignments first (FK constraint)
            DataRow u = Db.QueryRow("SELECT Username FROM dbo.AppUsers WHERE UserID=@id", Db.P("@id", userId));
            if (u != null)
                Db.Exec("DELETE FROM dbo.UserRoleAssignments WHERE Username=@u", Db.P("@u", u["Username"]));
            Db.Exec("DELETE FROM dbo.AppUsers WHERE UserID=@id", Db.P("@id", userId));
        }

        // ===== Roles =====
        public static DataTable GetRoles()
        {
            return Db.Query("SELECT RoleID, RoleName, Description, IsActive FROM dbo.UserRoles ORDER BY RoleName");
        }

        public static void CreateRole(string roleName, string description)
        {
            Db.Exec("INSERT INTO dbo.UserRoles(RoleName, Description) VALUES(@n, @d)",
                Db.P("@n", roleName), Db.P("@d", description ?? ""));
        }

        // ===== Role assignments (Reviewer / Approver) =====
        public static DataTable GetUserRoleAssignments(string username)
        {
            return Db.Query(
                "SELECT RoleType FROM dbo.UserRoleAssignments WHERE Username=@u",
                Db.P("@u", username));
        }

        public static void SetRoleAssignment(string username, string roleType, bool active, string changedBy)
        {
            int cnt = Convert.ToInt32(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.UserRoleAssignments WHERE Username=@u AND RoleType=@r",
                Db.P("@u", username), Db.P("@r", roleType)));
            if (active && cnt == 0)
                Db.Exec("INSERT INTO dbo.UserRoleAssignments(Username, RoleType, CreatedBy) VALUES(@u, @r, @cb)",
                    Db.P("@u", username), Db.P("@r", roleType), Db.P("@cb", changedBy));
            else if (!active && cnt > 0)
                Db.Exec("DELETE FROM dbo.UserRoleAssignments WHERE Username=@u AND RoleType=@r",
                    Db.P("@u", username), Db.P("@r", roleType));
        }

        // ===== Page access =====
        public static DataTable GetPageRegistry()
        {
            return Db.Query("SELECT PageID, PageName, PageUrl, Category, SortOrder, IsActive FROM dbo.PageRegistry ORDER BY SortOrder, PageName");
        }

        public static DataTable GetPageAccessForRole(int roleId)
        {
            return Db.Query(@"SELECT p.PageID, p.PageName, p.Category,
                                     CASE WHEN a.AccessID IS NOT NULL THEN 1 ELSE 0 END AS CanView
                              FROM dbo.PageRegistry p
                              LEFT JOIN dbo.PageAccess a ON a.PageID = p.PageID AND a.RoleID = @r
                              WHERE p.IsActive = 1 ORDER BY p.SortOrder",
                Db.P("@r", roleId));
        }

        public static void SavePageAccess(int roleId, int pageId, bool canView)
        {
            int cnt = Convert.ToInt32(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.PageAccess WHERE RoleID=@r AND PageID=@p",
                Db.P("@r", roleId), Db.P("@p", pageId)));
            if (canView && cnt == 0)
                Db.Exec("INSERT INTO dbo.PageAccess(RoleID, PageID, CanView) VALUES(@r, @p, 1)",
                    Db.P("@r", roleId), Db.P("@p", pageId));
            else if (!canView && cnt > 0)
                Db.Exec("DELETE FROM dbo.PageAccess WHERE RoleID=@r AND PageID=@p",
                    Db.P("@r", roleId), Db.P("@p", pageId));
        }

        // ===== Notifications =====
        public static DataTable GetNotifications(string username, bool unreadOnly = false)
        {
            string sql = "SELECT TOP 20 NotificationID, Subject, Message, LinkUrl, IsRead, CreatedDate, NotifType " +
                         "FROM dbo.Notifications WHERE Recipient=@u";
            if (unreadOnly) sql += " AND IsRead=0";
            sql += " ORDER BY CreatedDate DESC";
            return Db.Query(sql, Db.P("@u", username));
        }

        public static int GetUnreadCount(string username)
        {
            return Convert.ToInt32(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.Notifications WHERE Recipient=@u AND IsRead=0",
                Db.P("@u", username)));
        }

        public static void MarkRead(int notifId)
        {
            Db.Exec("UPDATE dbo.Notifications SET IsRead=1 WHERE NotificationID=@id", Db.P("@id", notifId));
        }

        public static void MarkAllRead(string username)
        {
            Db.Exec("UPDATE dbo.Notifications SET IsRead=1 WHERE Recipient=@u AND IsRead=0", Db.P("@u", username));
        }

        public static void DeleteNotification(int notifId)
        {
            Db.Exec("DELETE FROM dbo.Notifications WHERE NotificationID=@id", Db.P("@id", notifId));
        }

        public static void DeleteAllNotifications(string username)
        {
            Db.Exec("DELETE FROM dbo.Notifications WHERE Recipient=@u", Db.P("@u", username));
        }

        public static void SendNotification(string recipient, string subject, string message, string url, int? petFormId, string type)
        {
            Db.Exec(@"INSERT INTO dbo.Notifications(Recipient, Subject, Message, LinkUrl, PetFormID, NotifType)
                      VALUES(@r, @s, @m, @u, @p, @t)",
                Db.P("@r", recipient), Db.P("@s", subject), Db.P("@m", message),
                Db.P("@u", url ?? ""), Db.P("@p", (object)petFormId ?? DBNull.Value), Db.P("@t", type ?? ""));
        }
    }
}
