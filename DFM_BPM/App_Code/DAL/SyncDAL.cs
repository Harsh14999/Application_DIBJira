using System;
using System.Data;
using System.Data.SqlClient;

namespace DFM_BPM.App_Code.DAL
{
    /// <summary>
    /// Handles Oracle → SQL Server sync for masters and BPM fact data.
    /// Called from Admin/OracleSync.aspx.cs.
    /// </summary>
    public static class SyncDAL
    {
        // ===================================================================
        // CAPEX master (Q5 / Q11: MEMO_CAPEX_OPEX where TYPE='Capex')
        // GL numbers from Q7 subquery
        // ===================================================================
        private const string SQL_CAPEX =
            @"SELECT CAPEX_OPEX_ID, BUDGETED_AMOUNT, UTILIGED_AMOUNT, AVAILABLE_AMOUNT, LOCKED_AMOUNT,
                     (SELECT LISTAGG(GL_NUMBER, ',') WITHIN GROUP (ORDER BY GL_NUMBER)
                      FROM dibprod1.CAPEX_OPEX_IDS C2, dibprod1.MEMO_EXTTABLE M2,
                           dibprod1.CMPLX_MEMODIB_NEWGL1_DETAILS N2
                      WHERE M2.WORKITEM_NAME = N2.WINAME_NEWGL AND M2.WORKITEM_NAME = C2.WINAME
                        AND M2.TYPEOFMEMO = 'Cost Approval-Capex' AND C2.CAPEX_OPEX_IDS = O.CAPEX_OPEX_ID) AS GL_NUMBER
              FROM dibprod1.MEMO_CAPEX_OPEX O WHERE TYPE = 'Capex'";

        // ===================================================================
        // OPEX master (Q10: MEMO_CAPEX_OPEX where TYPE='Opex')
        // Contracts from Q8 join
        // ===================================================================
        private const string SQL_OPEX =
            @"SELECT O.CAPEX_OPEX_ID, O.BUDGETED_AMOUNT, O.UTILIGED_AMOUNT, O.AVAILABLE_AMOUNT, O.LOCKED_AMOUNT,
                     N.PO_REQ AS OPEX_CONTRACTS
              FROM dibprod1.MEMO_CAPEX_OPEX O
              LEFT JOIN dibprod1.MEMO_CA_CONTRACT_DETAILS N
                   ON N.BUDGET_ID = O.CAPEX_OPEX_ID AND N.BUDGET_ID IS NOT NULL AND N.CLOSED_STATUS = 'OPEN'
              WHERE UPPER(O.TYPE) = 'OPEX'";

        // ===================================================================
        // GL master (Q2: MEMO_GL_DETAILS)
        // ===================================================================
        private const string SQL_GL =
            @"SELECT GL_NUMBER, GL_DESCRIPTON, GL_OPENED_DATE, GL_BUDGETED_AMOUNT,
                     GL_LOCKED_AMOUNT, AMS_LOCKED_AMT, COMMITTED_AMT,
                     G.GL_BUDGETED_AMOUNT
                       - NVL(G.GL_LOCKED_AMOUNT,0)
                       - NVL(G.AMS_LOCKED_AMT,0)
                       - NVL(G.COMMITTED_AMT,0) AS GL_BALANCE_AMOUNT,
                     CAPITALIZED_AMOUNT, INVOICE_AMT_PROCESSED
              FROM dibprod1.MEMO_GL_DETAILS G
              WHERE GL_STATUS = 'A' AND GL_BUDGETED_AMOUNT != 0";

        // ===================================================================
        // Vendor master (Q16: MEMO_LPO_VENDOR_MASTER)
        // ===================================================================
        private const string SQL_VENDOR =
            @"SELECT DISTINCT VENDOR_NAME, VENDOR_CODE FROM dibprod1.MEMO_LPO_VENDOR_MASTER";

        // ===================================================================
        // Projects (Q7-3: MEMO_PROJECT_DETAILS)
        // ===================================================================
        private const string SQL_PROJECTS =
            @"SELECT PROJECT_ID, PROJECT_NAME, PROJECT_MANAGER, PROJECT_MANAGER_EMAILID,
                     PROJECT_AMOUNT, PROJECT_START_DATE, PROJECT_END_DATE, PROJECT_DESCRIPTON,
                     UTILIZED_AMT, BALANCE_AMT, PROJECT_BPM_LOCK_AMT, PROJECT_AMS_LOCK_AMT,
                     CAPEX_ID, BUSINESS_AREA, PROJECT_STATUS, PROJECT_EXECUTION_END_DATE
              FROM DIBPROD1.MEMO_PROJECT_DETAILS";

        // ===================================================================
        // PET management (Q7-2: PET_MANAGEMENT)
        // ===================================================================
        private const string SQL_PET =
            @"SELECT PET_REFERENCE_NO, DESCRIPTION, PET_APPROVED_AMOUNT,
                     BPM_LOCKED_AMOUNT, UTILIZED, BALANCE, PROJECT_ID
              FROM DIBPROD1.PET_MANAGEMENT";

        // ===================================================================
        // Contract (Q1: MEMO_CA_CONTRACT_DETAILS)
        // ===================================================================
        private const string SQL_CONTRACT =
            @"SELECT G.WI_NAME, G.CONT_DESCRIPTION, M.INITIATOR_DEPARTMENT, W.INTRODUCTIONDATETIME,
                     M.INI_USER, M.EFORMNO, W.ACTIVITYNAME, G.CONTRACT_CURRENCY,
                     G.CONT_NEW_TOTAL_AMOUNT, G.BPM_LOCKED_AMT, G.AMS_LOCKED_AMT, G.UTILIZED_AMT,
                     G.CONT_NEW_TOTAL_AMOUNT-(NVL(G.BPM_LOCKED_AMT,0)+NVL(G.AMS_LOCKED_AMT,0)+NVL(G.UTILIZED_AMT,0)) AS CONTRACT_BALANCE,
                     G.BUDGET_ID, G.CONT_VENDOR_NAME, G.PO_REQ,
                     G.CONT_NEW_CNTRCT_STRT_DATE, G.CONT_NEW_CNTRCT_END_DATE,
                     G.CONTRACT_STATUS, M.DECISION, W.PROCESSEDBY, W.ASSIGNEDUSER,
                     W.ENTRYDATETIME, G.CONT_REQ_TYP_AMC, G.CONT_REQ_TYP_RESOURCE,
                     G.CONT_REQ_TYP_MANAGED_SRVC, G.CONT_REQ_TYP_POD, G.CONT_REQ_TYP_VENDOR_SPRT,
                     G.CONT_REQ_MODE
              FROM dibprod1.MEMO_CA_CONTRACT_DETAILS G,
                   dibprod1.WFINSTRUMENTTABLE W,
                   dibprod1.MEMO_EXTTABLE M
              WHERE W.PROCESSNAME = 'MemoDIB'
                AND G.WI_NAME = W.PROCESSINSTANCEID
                AND G.WI_NAME = M.WORKITEM_NAME
                AND M.TYPEOFMEMO = 'Cost Approval-Capex'
                AND W.ACTIVITYNAME NOT IN ('Initiation','Initiation_Return')";

        // ===================================================================
        // LPO (Q3)
        // ===================================================================
        private const string SQL_LPO =
            @"SELECT D.WI_NAME, G.LPO_DESC, G.PO_REQ, M.INITIATOR_DEPARTMENT, W.INTRODUCTIONDATETIME,
                     W.ACTIVITYNAME, M.INI_USER, M.EFORMNO, G.SUPPLIER_NAME, D.AED_TOTAL, D.CURRENCY,
                     D.USD_TOTAL, G.GL_NO, G.LPO_STATUS, M.DECISION, G.AMOUNT, G.LOCKED_AMNT,
                     G.AMS_LOCKED_AMT, G.COMMITTED_AMT,
                     G.AMOUNT - NVL(G.LOCKED_AMNT,0) - NVL(G.AMS_LOCKED_AMT,0) - NVL(G.COMMITTED_AMT,0) AS LPO_BALANCE,
                     W.ENTRYDATETIME
              FROM DIBPROD1.NG_MEMO_LPO_GRID_DETAILS G
              JOIN DIBPROD1.MEMO_LPO_REQUEST_DETAILS D ON D.WI_NAME = G.WI_NAME
              JOIN DIBPROD1.WFINSTRUMENTTABLE W ON W.PROCESSINSTANCEID = D.WI_NAME
              JOIN DIBPROD1.MEMO_EXTTABLE M ON M.WORKITEM_NAME = D.WI_NAME
              WHERE W.PROCESSNAME = 'MemoDIB' AND M.TYPEOFMEMO = 'LPO'
                AND W.ACTIVITYNAME NOT IN ('Initiation','Initiation_Return')";

        // ===================================================================
        // Invoice (Q4)
        // ===================================================================
        private const string SQL_INVOICE =
            @"SELECT G.WI_NAME, G.INVOICE_TYPE, D.IT_VERTICAL_NAME, W.INTRODUCTIONDATETIME,
                     M.INI_USER, M.EFORMNO, D.INVOICE_VENDOR, D.INVOICE_NUMBER,
                     D.INVOICE_GRID_TOTAL_USD_AMT, D.INVOICE_CURRENCY, D.INVOICE_AMOUNT,
                     D.INVOICE_DATE, G.INVOICE_TYPE_REF_NO, G.INVOICE_TYPE_DESC,
                     G.STATUS, M.DECISION, W.ACTIVITYNAME, W.ASSIGNEDUSER, W.ENTRYDATETIME
              FROM dibprod1.NG_MEMO_INVOICE_GRID G
              JOIN dibprod1.MEMO_INVOICE_PROCESS_DETAILS D ON D.WI_NAME = G.WI_NAME
              JOIN dibprod1.WFINSTRUMENTTABLE W ON D.WI_NAME = W.PROCESSINSTANCEID
              JOIN dibprod1.MEMO_EXTTABLE M ON D.WI_NAME = M.WORKITEM_NAME
              WHERE W.PROCESSNAME = 'MemoDIB' AND M.TYPEOFMEMO = 'Invoice Processing'
                AND W.ACTIVITYNAME NOT IN ('Initiation','Initiation_Return')";

        // ===================================================================
        // CAPEX/OPEX transaction details (Q14 Capex, Q13 Opex)
        // ===================================================================
        private const string SQL_CAPEX_OPEX_DETAILS =
            @"SELECT DISTINCT M.ITEM_TYPE, M.ITEM_ID, M.ITEM_DESCRIPTION, M.BUDGETED_AMOUNT,
                     M.UTILIGED_AMOUNT, M.LOCKED_AMOUNT, M.AVAILABLE_AMOUNT, M.WINAME,
                     M.CLAIM_AMOUNT, M.BAL_CLAIM_AMT, M.OLD_CLAIM_AMOUNT,
                     E.PID_CAPEX_ID, E.CAPEX_OPEX_PET_PROJECT_ID,
                     PD.PROJECT_NAME, E.REFERENCE, P.PET_APPROVED_AMOUNT,
                     E.CAP_OP_VENDOR_NAME, E.INITIATOR_DEPARTMENT, E.EFORMDATE
              FROM DIBPROD1.MEMO_CAPEX_OPEX_DETAILS M
              INNER JOIN DIBPROD1.WFINSTRUMENTTABLE W ON M.WINAME = W.PROCESSINSTANCEID
              INNER JOIN DIBPROD1.MEMO_DEC_HIST D ON W.PROCESSINSTANCEID = D.WORKITEM_NAME
              INNER JOIN DIBPROD1.MEMO_EXTTABLE E ON E.WORKITEM_NAME = W.PROCESSINSTANCEID
              INNER JOIN DIBPROD1.PET_REF_IDS R ON E.WORKITEM_NAME = R.WI_NAME
              INNER JOIN DIBPROD1.PET_MANAGEMENT P ON R.PET_REF_IDS = P.PET_REFERENCE_NO
              LEFT JOIN DIBPROD1.MEMO_PROJECT_DETAILS PD ON PD.PROJECT_ID = E.CAPEX_OPEX_PET_PROJECT_ID
              WHERE M.ITEM_TYPE IN ('Capex','Opex')
                AND W.INTRODUCTIONDATETIME BETWEEN '01-JAN-25' AND SYSDATE
                AND (W.ACTIVITYNAME='Exit' OR W.ACTIVITYNAME='Archival'
                     OR W.ACTIVITYNAME='TechFinance_InProgress' OR W.ACTIVITYNAME='TechFinance_InPipeline')
                AND E.TYPEOFMEMO = 'Cost Approval-Capex'
                AND 0 = (SELECT COUNT(1) FROM DIBPROD1.MEMO_DEC_HIST
                         WHERE DECISION='Reject' AND WORKITEM_NAME=W.PROCESSINSTANCEID)
                AND 0 = (SELECT COUNT(1) FROM DIBPROD1.WFCURRENTROUTELOGTABLE
                         WHERE ACTIONID=45 AND PROCESSINSTANCEID=W.PROCESSINSTANCEID)";

        // ===================================================================
        // PUBLIC SYNC METHODS
        // ===================================================================

        public static SyncResult SyncCapex(int syncId)
        {
            var r = new SyncResult { SyncType = "CapexOnly" };
            try
            {
                DataTable dt = OracleDb.Query(SQL_CAPEX);
                r.RecordsIn = dt.Rows.Count;
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        Db.ExecSP("dbo.sp_SyncCapex",
                            Db.P("@CapexID",        row["CAPEX_OPEX_ID"]),
                            Db.P("@BudgetedAmount",  DecimalVal(row, "BUDGETED_AMOUNT")),
                            Db.P("@UtilizedAmount",  DecimalVal(row, "UTILIGED_AMOUNT")),
                            Db.P("@AvailableAmount", DecimalVal(row, "AVAILABLE_AMOUNT")),
                            Db.P("@LockedAmount",    DecimalVal(row, "LOCKED_AMOUNT")),
                            Db.P("@GLNumbers",       row["GL_NUMBER"]));
                        r.RecordsUp++;
                    }
                    catch { r.Errors++; }
                }
                r.Success = true;
            }
            catch (Exception ex) { r.ErrorMsg = ex.Message; }
            return r;
        }

        public static SyncResult SyncOpex(int syncId)
        {
            var r = new SyncResult { SyncType = "OpexOnly" };
            try
            {
                DataTable dt = OracleDb.Query(SQL_OPEX);
                r.RecordsIn = dt.Rows.Count;
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        Db.ExecSP("dbo.sp_SyncOpex",
                            Db.P("@OpexID",         row["CAPEX_OPEX_ID"]),
                            Db.P("@BudgetedAmount",  DecimalVal(row, "BUDGETED_AMOUNT")),
                            Db.P("@UtilizedAmount",  DecimalVal(row, "UTILIGED_AMOUNT")),
                            Db.P("@AvailableAmount", DecimalVal(row, "AVAILABLE_AMOUNT")),
                            Db.P("@LockedAmount",    DecimalVal(row, "LOCKED_AMOUNT")),
                            Db.P("@Contracts",       row.Table.Columns.Contains("OPEX_CONTRACTS") ? row["OPEX_CONTRACTS"] : (object)DBNull.Value));
                        r.RecordsUp++;
                    }
                    catch { r.Errors++; }
                }
                r.Success = true;
            }
            catch (Exception ex) { r.ErrorMsg = ex.Message; }
            return r;
        }

        public static SyncResult SyncGL(int syncId)
        {
            var r = new SyncResult { SyncType = "GLOnly" };
            try
            {
                DataTable dt = OracleDb.Query(SQL_GL);
                r.RecordsIn = dt.Rows.Count;
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        Db.ExecSP("dbo.sp_SyncGL",
                            Db.P("@GLNumber",          row["GL_NUMBER"]),
                            Db.P("@GLDescription",     row["GL_DESCRIPTON"]),
                            Db.P("@GLOpenedDate",      DateVal(row, "GL_OPENED_DATE")),
                            Db.P("@BudgetedAmount",    DecimalVal(row, "GL_BUDGETED_AMOUNT")),
                            Db.P("@BPMLockedAmount",   DecimalVal(row, "GL_LOCKED_AMOUNT")),
                            Db.P("@AMSLockedAmount",   DecimalVal(row, "AMS_LOCKED_AMT")),
                            Db.P("@UtilizedAmount",    DecimalVal(row, "COMMITTED_AMT")),
                            Db.P("@CapitalizedAmount", DecimalVal(row, "CAPITALIZED_AMOUNT")),
                            Db.P("@InvoiceProcessed",  DecimalVal(row, "INVOICE_AMT_PROCESSED")));
                        r.RecordsUp++;
                    }
                    catch { r.Errors++; }
                }
                r.Success = true;
            }
            catch (Exception ex) { r.ErrorMsg = ex.Message; }
            return r;
        }

        public static SyncResult SyncVendor(int syncId)
        {
            var r = new SyncResult { SyncType = "VendorOnly" };
            try
            {
                DataTable dt = OracleDb.Query(SQL_VENDOR);
                r.RecordsIn = dt.Rows.Count;
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        Db.ExecSP("dbo.sp_SyncVendor",
                            Db.P("@VendorCode", row["VENDOR_CODE"]),
                            Db.P("@VendorName", row["VENDOR_NAME"]));
                        r.RecordsUp++;
                    }
                    catch { r.Errors++; }
                }
                r.Success = true;
            }
            catch (Exception ex) { r.ErrorMsg = ex.Message; }
            return r;
        }

        public static SyncResult SyncBPMData(int syncId)
        {
            var r = new SyncResult { SyncType = "BPMData" };
            try
            {
                // Projects
                var dtProj = OracleDb.Query(SQL_PROJECTS);
                r.RecordsIn += dtProj.Rows.Count;
                UpsertProjects(dtProj, ref r);

                // PET
                var dtPet = OracleDb.Query(SQL_PET);
                r.RecordsIn += dtPet.Rows.Count;
                UpsertPet(dtPet, ref r);

                // Contracts
                var dtContract = OracleDb.Query(SQL_CONTRACT);
                r.RecordsIn += dtContract.Rows.Count;
                UpsertContracts(dtContract, ref r);

                // LPO
                var dtLpo = OracleDb.Query(SQL_LPO);
                r.RecordsIn += dtLpo.Rows.Count;
                UpsertLPO(dtLpo, ref r);

                // Invoice
                var dtInv = OracleDb.Query(SQL_INVOICE);
                r.RecordsIn += dtInv.Rows.Count;
                UpsertInvoice(dtInv, ref r);

                // CAPEX/OPEX details
                var dtCod = OracleDb.Query(SQL_CAPEX_OPEX_DETAILS);
                r.RecordsIn += dtCod.Rows.Count;
                UpsertCapexOpexDetails(dtCod, ref r);

                r.Success = true;
            }
            catch (Exception ex) { r.ErrorMsg = ex.Message; }
            return r;
        }

        // ===================================================================
        // Upsert helpers
        // ===================================================================
        private static void UpsertProjects(DataTable dt, ref SyncResult r)
        {
            foreach (DataRow row in dt.Rows)
            {
                try
                {
                    string id = row["PROJECT_ID"].ToString();
                    int cnt = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM dbo.BPM_Projects WHERE ProjectID=@id", Db.P("@id", id)));
                    if (cnt > 0)
                        Db.Exec(@"UPDATE dbo.BPM_Projects SET ProjectName=@n, ProjectManager=@m, ProjectManagerEmail=@me,
                                  ProjectAmount=@a, ProjectStartDate=@sd, ProjectEndDate=@ed, ProjectDescription=@d,
                                  UtilizedAmt=@u, BalanceAmt=@b, BPMLockedAmt=@bl, AMSLockedAmt=@al,
                                  CapexID=@ci, BusinessArea=@ba, ProjectStatus=@ps, ExecutionEndDate=@xe,
                                  LastSyncDate=GETDATE() WHERE ProjectID=@id",
                            Db.P("@n",row["PROJECT_NAME"]), Db.P("@m",row["PROJECT_MANAGER"]),
                            Db.P("@me",row["PROJECT_MANAGER_EMAILID"]), Db.P("@a",DecimalVal(row,"PROJECT_AMOUNT")),
                            Db.P("@sd",DateVal(row,"PROJECT_START_DATE")), Db.P("@ed",DateVal(row,"PROJECT_END_DATE")),
                            Db.P("@d",row["PROJECT_DESCRIPTON"]), Db.P("@u",DecimalVal(row,"UTILIZED_AMT")),
                            Db.P("@b",DecimalVal(row,"BALANCE_AMT")), Db.P("@bl",DecimalVal(row,"PROJECT_BPM_LOCK_AMT")),
                            Db.P("@al",DecimalVal(row,"PROJECT_AMS_LOCK_AMT")), Db.P("@ci",row["CAPEX_ID"]),
                            Db.P("@ba",row["BUSINESS_AREA"]), Db.P("@ps",row["PROJECT_STATUS"]),
                            Db.P("@xe",DateVal(row,"PROJECT_EXECUTION_END_DATE")), Db.P("@id",id));
                    else
                        Db.Exec(@"INSERT INTO dbo.BPM_Projects(ProjectID,ProjectName,ProjectManager,ProjectManagerEmail,
                                  ProjectAmount,ProjectStartDate,ProjectEndDate,ProjectDescription,UtilizedAmt,BalanceAmt,
                                  BPMLockedAmt,AMSLockedAmt,CapexID,BusinessArea,ProjectStatus,ExecutionEndDate,LastSyncDate)
                                  VALUES(@id,@n,@m,@me,@a,@sd,@ed,@d,@u,@b,@bl,@al,@ci,@ba,@ps,@xe,GETDATE())",
                            Db.P("@id",id), Db.P("@n",row["PROJECT_NAME"]), Db.P("@m",row["PROJECT_MANAGER"]),
                            Db.P("@me",row["PROJECT_MANAGER_EMAILID"]), Db.P("@a",DecimalVal(row,"PROJECT_AMOUNT")),
                            Db.P("@sd",DateVal(row,"PROJECT_START_DATE")), Db.P("@ed",DateVal(row,"PROJECT_END_DATE")),
                            Db.P("@d",row["PROJECT_DESCRIPTON"]), Db.P("@u",DecimalVal(row,"UTILIZED_AMT")),
                            Db.P("@b",DecimalVal(row,"BALANCE_AMT")), Db.P("@bl",DecimalVal(row,"PROJECT_BPM_LOCK_AMT")),
                            Db.P("@al",DecimalVal(row,"PROJECT_AMS_LOCK_AMT")), Db.P("@ci",row["CAPEX_ID"]),
                            Db.P("@ba",row["BUSINESS_AREA"]), Db.P("@ps",row["PROJECT_STATUS"]),
                            Db.P("@xe",DateVal(row,"PROJECT_EXECUTION_END_DATE")));
                    r.RecordsUp++;
                }
                catch { r.Errors++; }
            }
        }

        private static void UpsertPet(DataTable dt, ref SyncResult r)
        {
            foreach (DataRow row in dt.Rows)
            {
                try
                {
                    string id = row["PET_REFERENCE_NO"].ToString();
                    int cnt = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM dbo.BPM_PET WHERE PETReferenceNo=@id", Db.P("@id", id)));
                    if (cnt > 0)
                        Db.Exec(@"UPDATE dbo.BPM_PET SET Description=@d, PETApprovedAmt=@a, BPMLockedAmount=@bl,
                                  Utilized=@u, Balance=@b, ProjectID=@p, LastSyncDate=GETDATE()
                                  WHERE PETReferenceNo=@id",
                            Db.P("@d",row["DESCRIPTION"]), Db.P("@a",DecimalVal(row,"PET_APPROVED_AMOUNT")),
                            Db.P("@bl",DecimalVal(row,"BPM_LOCKED_AMOUNT")), Db.P("@u",DecimalVal(row,"UTILIZED")),
                            Db.P("@b",DecimalVal(row,"BALANCE")), Db.P("@p",row["PROJECT_ID"]), Db.P("@id",id));
                    else
                        Db.Exec(@"INSERT INTO dbo.BPM_PET(PETReferenceNo,Description,PETApprovedAmt,BPMLockedAmount,
                                  Utilized,Balance,ProjectID,LastSyncDate)
                                  VALUES(@id,@d,@a,@bl,@u,@b,@p,GETDATE())",
                            Db.P("@id",id), Db.P("@d",row["DESCRIPTION"]),
                            Db.P("@a",DecimalVal(row,"PET_APPROVED_AMOUNT")),
                            Db.P("@bl",DecimalVal(row,"BPM_LOCKED_AMOUNT")),
                            Db.P("@u",DecimalVal(row,"UTILIZED")), Db.P("@b",DecimalVal(row,"BALANCE")),
                            Db.P("@p",row["PROJECT_ID"]));
                    r.RecordsUp++;
                }
                catch { r.Errors++; }
            }
        }

        private static void UpsertContracts(DataTable dt, ref SyncResult r)
        {
            foreach (DataRow row in dt.Rows)
            {
                try
                {
                    string id = row["WI_NAME"].ToString();
                    string rType = BuildRequestType(row);
                    string status = BuildContractStatus(row);
                    int cnt = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM dbo.BPM_Contract WHERE WiName=@id", Db.P("@id", id)));
                    string upsertSql = cnt > 0
                        ? @"UPDATE dbo.BPM_Contract SET Reference=@ref, Department=@dep, InitiationDate=@id2,
                            InitiatorName=@ini, EFormNo=@ef, CurrentStage=@cs, Currency=@cur,
                            LCAmount=@lca, FCAmount=@fca, BPMLockedAmount=@bpml, AMSLockedAmount=@amsl,
                            UtilizedAmount=@ua, ContractBalance=@cb, OpexID=@oid, VendorName=@vn,
                            RequestType=@rt, RequestMode=@rm, ContractNo=@cn, ContractStartDate=@csd,
                            ContractEndDate=@ced, ContractStatus=@cst, BPMLastStatus=@bls,
                            LastActionBy=@lab, PendingWith=@pw, LastActionDate=@lad,
                            TechFinanceStatus=@tfs, LastSyncDate=GETDATE() WHERE WiName=@id"
                        : @"INSERT INTO dbo.BPM_Contract(WiName,Reference,Department,InitiationDate,InitiatorName,EFormNo,
                            CurrentStage,Currency,LCAmount,FCAmount,BPMLockedAmount,AMSLockedAmount,UtilizedAmount,
                            ContractBalance,OpexID,VendorName,RequestType,RequestMode,ContractNo,ContractStartDate,
                            ContractEndDate,ContractStatus,BPMLastStatus,LastActionBy,PendingWith,LastActionDate,
                            TechFinanceStatus,LastSyncDate)
                            VALUES(@id,@ref,@dep,@id2,@ini,@ef,@cs,@cur,@lca,@fca,@bpml,@amsl,@ua,@cb,@oid,@vn,
                            @rt,@rm,@cn,@csd,@ced,@cst,@bls,@lab,@pw,@lad,@tfs,GETDATE())";
                    Db.Exec(upsertSql,
                        Db.P("@id",id), Db.P("@ref",row["CONT_DESCRIPTION"]), Db.P("@dep",row["INITIATOR_DEPARTMENT"]),
                        Db.P("@id2",DateVal(row,"INTRODUCTIONDATETIME")), Db.P("@ini",row["INI_USER"]),
                        Db.P("@ef",row["EFORMNO"]), Db.P("@cs",row["ACTIVITYNAME"]),
                        Db.P("@cur",row["CONTRACT_CURRENCY"]), Db.P("@lca",DecimalVal(row,"CONT_NEW_TOTAL_AMOUNT")),
                        Db.P("@fca",DecimalVal(row,"CONT_NEW_TOTAL_AMOUNT")),
                        Db.P("@bpml",DecimalVal(row,"BPM_LOCKED_AMT")), Db.P("@amsl",DecimalVal(row,"AMS_LOCKED_AMT")),
                        Db.P("@ua",DecimalVal(row,"UTILIZED_AMT")), Db.P("@cb",DecimalVal(row,"CONTRACT_BALANCE")),
                        Db.P("@oid",row["BUDGET_ID"]), Db.P("@vn",row["CONT_VENDOR_NAME"]),
                        Db.P("@rt",rType), Db.P("@rm",row["CONT_REQ_MODE"]),
                        Db.P("@cn",row["PO_REQ"]), Db.P("@csd",DateVal(row,"CONT_NEW_CNTRCT_STRT_DATE")),
                        Db.P("@ced",DateVal(row,"CONT_NEW_CNTRCT_END_DATE")),
                        Db.P("@cst",status), Db.P("@bls",row["DECISION"]),
                        Db.P("@lab",row["PROCESSEDBY"]), Db.P("@pw",row["ASSIGNEDUSER"]),
                        Db.P("@lad",DateVal(row,"ENTRYDATETIME")), Db.P("@tfs",DBNull.Value));
                    r.RecordsUp++;
                }
                catch { r.Errors++; }
            }
        }

        private static void UpsertLPO(DataTable dt, ref SyncResult r)
        {
            foreach (DataRow row in dt.Rows)
            {
                try
                {
                    string id = row["WI_NAME"].ToString();
                    int cnt = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM dbo.BPM_LPO WHERE WiName=@id", Db.P("@id", id)));
                    string sql = cnt > 0
                        ? @"UPDATE dbo.BPM_LPO SET LPODesc=@d, LPONo=@n, Department=@dep,
                            InitiationDate=@dt, CurrentStage=@cs, InitiatorName=@ini, EFormNo=@ef,
                            VendorName=@vn, LCAmount=@lca, Currency=@cur, FCAmount=@fca,
                            GLNumber=@gl, LPOStatus=@ls, BPMStatus=@bs, BudgetAmount=@ba,
                            BPMLockedAmount=@bpml, AMSLockedAmount=@amsl, UtilizedAmount=@ua,
                            AvailableBalance=@ab, ActionDate=@ad, LastSyncDate=GETDATE() WHERE WiName=@id"
                        : @"INSERT INTO dbo.BPM_LPO(WiName,LPODesc,LPONo,Department,InitiationDate,CurrentStage,
                            InitiatorName,EFormNo,VendorName,LCAmount,Currency,FCAmount,GLNumber,LPOStatus,BPMStatus,
                            BudgetAmount,BPMLockedAmount,AMSLockedAmount,UtilizedAmount,AvailableBalance,ActionDate,LastSyncDate)
                            VALUES(@id,@d,@n,@dep,@dt,@cs,@ini,@ef,@vn,@lca,@cur,@fca,@gl,@ls,@bs,@ba,@bpml,@amsl,@ua,@ab,@ad,GETDATE())";
                    Db.Exec(sql,
                        Db.P("@id",id), Db.P("@d",row["LPO_DESC"]), Db.P("@n",row["PO_REQ"]),
                        Db.P("@dep",row["INITIATOR_DEPARTMENT"]), Db.P("@dt",DateVal(row,"INTRODUCTIONDATETIME")),
                        Db.P("@cs",row["ACTIVITYNAME"]), Db.P("@ini",row["INI_USER"]), Db.P("@ef",row["EFORMNO"]),
                        Db.P("@vn",row["SUPPLIER_NAME"]), Db.P("@lca",DecimalVal(row,"AED_TOTAL")),
                        Db.P("@cur",row["CURRENCY"]), Db.P("@fca",DecimalVal(row,"USD_TOTAL")),
                        Db.P("@gl",row["GL_NO"]), Db.P("@ls",row["LPO_STATUS"]), Db.P("@bs",row["DECISION"]),
                        Db.P("@ba",DecimalVal(row,"AMOUNT")), Db.P("@bpml",DecimalVal(row,"LOCKED_AMNT")),
                        Db.P("@amsl",DecimalVal(row,"AMS_LOCKED_AMT")), Db.P("@ua",DecimalVal(row,"COMMITTED_AMT")),
                        Db.P("@ab",DecimalVal(row,"LPO_BALANCE")), Db.P("@ad",DateVal(row,"ENTRYDATETIME")));
                    r.RecordsUp++;
                }
                catch { r.Errors++; }
            }
        }

        private static void UpsertInvoice(DataTable dt, ref SyncResult r)
        {
            foreach (DataRow row in dt.Rows)
            {
                try
                {
                    string wi = row["WI_NAME"].ToString();
                    string inv = row["INVOICE_NUMBER"].ToString();
                    int cnt = Convert.ToInt32(Db.Scalar(
                        "SELECT COUNT(*) FROM dbo.BPM_Invoice WHERE WiName=@w AND InvoiceNumber=@i",
                        Db.P("@w", wi), Db.P("@i", inv)));
                    if (cnt > 0)
                        Db.Exec(@"UPDATE dbo.BPM_Invoice SET InvoiceType=@it, Department=@dep, InitiationDate=@dt,
                                  InitiatorName=@ini, EFormNo=@ef, VendorName=@vn, LCAmount=@lca, Currency=@cur,
                                  FCAmount=@fca, InvoiceDate=@id2, InvoiceRefNo=@irn, InvoiceRefDesc=@ird,
                                  AMSInvoiceStatus=@ais, BPMLastStatus=@bls, PendingAt=@pa, PendingWith=@pw,
                                  ActionDate=@ad, LastSyncDate=GETDATE() WHERE WiName=@w AND InvoiceNumber=@i",
                            Db.P("@it",row["INVOICE_TYPE"]), Db.P("@dep",row["IT_VERTICAL_NAME"]),
                            Db.P("@dt",DateVal(row,"INTRODUCTIONDATETIME")), Db.P("@ini",row["INI_USER"]),
                            Db.P("@ef",row["EFORMNO"]), Db.P("@vn",row["INVOICE_VENDOR"]),
                            Db.P("@lca",DecimalVal(row,"INVOICE_GRID_TOTAL_USD_AMT")), Db.P("@cur",row["INVOICE_CURRENCY"]),
                            Db.P("@fca",DecimalVal(row,"INVOICE_AMOUNT")), Db.P("@id2",DateVal(row,"INVOICE_DATE")),
                            Db.P("@irn",row["INVOICE_TYPE_REF_NO"]), Db.P("@ird",row["INVOICE_TYPE_DESC"]),
                            Db.P("@ais",row["STATUS"]), Db.P("@bls",row["DECISION"]),
                            Db.P("@pa",row["ACTIVITYNAME"]), Db.P("@pw",row["ASSIGNEDUSER"]),
                            Db.P("@ad",DateVal(row,"ENTRYDATETIME")), Db.P("@w",wi), Db.P("@i",inv));
                    else
                        Db.Exec(@"INSERT INTO dbo.BPM_Invoice(WiName,InvoiceType,Department,InitiationDate,InitiatorName,
                                  EFormNo,VendorName,InvoiceNumber,LCAmount,Currency,FCAmount,InvoiceDate,InvoiceRefNo,
                                  InvoiceRefDesc,AMSInvoiceStatus,BPMLastStatus,PendingAt,PendingWith,ActionDate,LastSyncDate)
                                  VALUES(@w,@it,@dep,@dt,@ini,@ef,@vn,@i,@lca,@cur,@fca,@id2,@irn,@ird,@ais,@bls,@pa,@pw,@ad,GETDATE())",
                            Db.P("@w",wi), Db.P("@it",row["INVOICE_TYPE"]), Db.P("@dep",row["IT_VERTICAL_NAME"]),
                            Db.P("@dt",DateVal(row,"INTRODUCTIONDATETIME")), Db.P("@ini",row["INI_USER"]),
                            Db.P("@ef",row["EFORMNO"]), Db.P("@vn",row["INVOICE_VENDOR"]), Db.P("@i",inv),
                            Db.P("@lca",DecimalVal(row,"INVOICE_GRID_TOTAL_USD_AMT")), Db.P("@cur",row["INVOICE_CURRENCY"]),
                            Db.P("@fca",DecimalVal(row,"INVOICE_AMOUNT")), Db.P("@id2",DateVal(row,"INVOICE_DATE")),
                            Db.P("@irn",row["INVOICE_TYPE_REF_NO"]), Db.P("@ird",row["INVOICE_TYPE_DESC"]),
                            Db.P("@ais",row["STATUS"]), Db.P("@bls",row["DECISION"]),
                            Db.P("@pa",row["ACTIVITYNAME"]), Db.P("@pw",row["ASSIGNEDUSER"]),
                            Db.P("@ad",DateVal(row,"ENTRYDATETIME")));
                    r.RecordsUp++;
                }
                catch { r.Errors++; }
            }
        }

        private static void UpsertCapexOpexDetails(DataTable dt, ref SyncResult r)
        {
            // Truncate and reload for simplicity (delta sync can be added later)
            Db.Exec("TRUNCATE TABLE dbo.BPM_CapexOpexDetails");
            foreach (DataRow row in dt.Rows)
            {
                try
                {
                    Db.Exec(@"INSERT INTO dbo.BPM_CapexOpexDetails(ItemType,ItemID,ItemDescription,BudgetedAmount,
                              UtilizedAmount,LockedAmount,AvailableAmount,WiName,ClaimAmount,BalClaimAmt,
                              OldClaimAmount,PIDCapexID,ProjectID,ProjectName,PetReference,PetApprovedAmt,
                              VendorName,InitiatorDept,EFormDate,LastSyncDate)
                              VALUES(@it,@iid,@idc,@ba,@ua,@la,@aa,@wi,@ca,@bca,@oca,@pid,@prj,@pn,@pr,@pa,@vn,@dept,@efd,GETDATE())",
                        Db.P("@it",row["ITEM_TYPE"]), Db.P("@iid",row["ITEM_ID"]),
                        Db.P("@idc",row["ITEM_DESCRIPTION"]), Db.P("@ba",DecimalVal(row,"BUDGETED_AMOUNT")),
                        Db.P("@ua",DecimalVal(row,"UTILIGED_AMOUNT")), Db.P("@la",DecimalVal(row,"LOCKED_AMOUNT")),
                        Db.P("@aa",DecimalVal(row,"AVAILABLE_AMOUNT")), Db.P("@wi",row["WINAME"]),
                        Db.P("@ca",DecimalVal(row,"CLAIM_AMOUNT")), Db.P("@bca",DecimalVal(row,"BAL_CLAIM_AMT")),
                        Db.P("@oca",DecimalVal(row,"OLD_CLAIM_AMOUNT")), Db.P("@pid",row["PID_CAPEX_ID"]),
                        Db.P("@prj",row["CAPEX_OPEX_PET_PROJECT_ID"]), Db.P("@pn",row["PROJECT_NAME"]),
                        Db.P("@pr",row["REFERENCE"]), Db.P("@pa",DecimalVal(row,"PET_APPROVED_AMOUNT")),
                        Db.P("@vn",row["CAP_OP_VENDOR_NAME"]), Db.P("@dept",row["INITIATOR_DEPARTMENT"]),
                        Db.P("@efd",DateVal(row,"EFORMDATE")));
                    r.RecordsUp++;
                }
                catch { r.Errors++; }
            }
        }

        // ===================================================================
        // Sync log helpers
        // ===================================================================
        public static int StartSyncLog(string syncType, string triggeredBy)
        {
            return Convert.ToInt32(Db.Scalar(
                "INSERT INTO dbo.SyncLog(SyncType, TriggeredBy) OUTPUT INSERTED.SyncID VALUES(@t, @u)",
                Db.P("@t", syncType), Db.P("@u", triggeredBy)));
        }

        public static void EndSyncLog(int syncId, SyncResult r)
        {
            Db.Exec(@"UPDATE dbo.SyncLog SET EndTime=GETDATE(), Status=@s, RecordsIn=@ri, RecordsUp=@ru, ErrorMsg=@e
                      WHERE SyncID=@id",
                Db.P("@s", r.Success ? "Success" : "Failed"),
                Db.P("@ri", r.RecordsIn), Db.P("@ru", r.RecordsUp),
                Db.P("@e", r.ErrorMsg), Db.P("@id", syncId));
        }

        public static DataTable GetSyncLogs(int top = 50)
        {
            return Db.Query(@"SELECT TOP (@t) SyncID, SyncType, StartTime, EndTime, Status,
                                             RecordsIn, RecordsUp, ErrorMsg, TriggeredBy
                              FROM dbo.SyncLog ORDER BY SyncID DESC",
                Db.P("@t", top));
        }

        // ===================================================================
        // Value helpers
        // ===================================================================
        private static decimal DecimalVal(DataRow row, string col)
        {
            if (!row.Table.Columns.Contains(col) || row[col] == DBNull.Value) return 0m;
            decimal v; return decimal.TryParse(row[col].ToString(), out v) ? v : 0m;
        }

        private static object DateVal(DataRow row, string col)
        {
            if (!row.Table.Columns.Contains(col) || row[col] == DBNull.Value) return DBNull.Value;
            return row[col];
        }

        private static string BuildRequestType(DataRow row)
        {
            if (row.Table.Columns.Contains("CONT_REQ_TYP_AMC") && row["CONT_REQ_TYP_AMC"].ToString() == "true") return "AMC";
            if (row.Table.Columns.Contains("CONT_REQ_TYP_RESOURCE") && row["CONT_REQ_TYP_RESOURCE"].ToString() == "true") return "Resource";
            if (row.Table.Columns.Contains("CONT_REQ_TYP_MANAGED_SRVC") && row["CONT_REQ_TYP_MANAGED_SRVC"].ToString() == "true") return "Managed Services";
            if (row.Table.Columns.Contains("CONT_REQ_TYP_POD") && row["CONT_REQ_TYP_POD"].ToString() == "true") return "POD";
            if (row.Table.Columns.Contains("CONT_REQ_TYP_VENDOR_SPRT") && row["CONT_REQ_TYP_VENDOR_SPRT"].ToString() == "true") return "Vendor Support";
            return null;
        }

        private static string BuildContractStatus(DataRow row)
        {
            string cs = row.Table.Columns.Contains("CONTRACT_STATUS") ? row["CONTRACT_STATUS"].ToString() : "";
            string act = row.Table.Columns.Contains("ACTIVITYNAME") ? row["ACTIVITYNAME"].ToString() : "";
            string dec = row.Table.Columns.Contains("DECISION") ? row["DECISION"].ToString() : "";
            if (cs == "Success") return "Approved";
            if (cs == "Cancelled") return "Cancelled";
            if (cs == "Rejected") return "Rejected";
            if (act == "LPOContract_Verification") return "Pending at Tech Finance Queue";
            if (act == "Approve") return "Pending at Approver Queue";
            if (act == "Exit" && dec == "Reject") return "Rejected in BPM";
            return "In Progress";
        }

        // ===================================================================
        // Oracle queries for history tables
        // ===================================================================
        private const string SQL_CAPEX_OPEX_HISTORY =
            @"SELECT HISTORYROWID, CF_STS, CF_CREATEDBY, CF_CREATEDDATETIME,
                     CF_MODIFIEDBY, CF_MODIFIEDDATETIME, DEPARTMENT_NAME,
                     CAPEX_OPEX_DESCRIPTION, ""TYPE"", CAPEX_OPEX_ID,
                     BUDGETED_AMOUNT, UTILIGED_AMOUNT, BALANCE,
                     LOCKED_AMOUNT, AVAILABLE_AMOUNT
              FROM DIBPROD1.H_MEMO_CAPEX_OPEX";

        private const string SQL_CAPEX_OPEX_DETAILS_SIMPLE =
            @"SELECT INSERTIONORDERID, ITEM_TYPE, ITEM_ID, ITEM_DESCRIPTION,
                     BUDGETED_AMOUNT, UTILIGED_AMOUNT, BALANCE,
                     LOCKED_AMOUNT, AVAILABLE_AMOUNT, LOCKED_STATUS,
                     WINAME, CLAIM_AMOUNT, CP_OP_ID, BAL_CLAIM_AMT, OLD_CLAIM_AMOUNT
              FROM DIBPROD1.MEMO_CAPEX_OPEX_DETAILS";

        private const string SQL_GL_HISTORY =
            @"SELECT HISTORYROWID, CF_STS, CF_CREATEDBY, CF_CREATEDDATETIME,
                     CF_MODIFIEDBY, CF_MODIFIEDDATETIME, GL_NUMBER, GL_DESCRIPTON,
                     GL_STATUS, GL_OPENED_DATE, GL_BUDGETED_AMOUNT, GL_UTILIGED_AMOUNT,
                     GL_AVAILABLE_AMOUNT, GL_LOCKED_AMOUNT, GL_TOPUP_AMOUNT,
                     GL_AMOUNT_POST_TOPUP, AMS_LOCKED_AMT, CAPITALIZED_AMOUNT,
                     INVOICE_AMT_PROCESSED, CAPEX_OPEX_ID, CAPEX_BUDGETED_AMOUNT, COMMITTED_AMT
              FROM DIBPROD1.H_MEMO_GL_DETAILS";

        // ===================================================================
        // Public sync methods for history tables (TRUNCATE + re-insert)
        // ===================================================================
        public static SyncResult SyncCapexOpexHistory(int syncId)
        {
            var r = new SyncResult { SyncType = "CapexOpexHistory" };
            try
            {
                DataTable dt = OracleDb.Query(SQL_CAPEX_OPEX_HISTORY);
                r.RecordsIn = dt.Rows.Count;
                Db.Exec("TRUNCATE TABLE dbo.CapexOpexHistory");
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        Db.ExecSP("dbo.sp_SyncCapexOpexHistory",
                            Db.P("@HistoryRowID",        row["HISTORYROWID"]),
                            Db.P("@CF_Sts",              row["CF_STS"]),
                            Db.P("@CF_CreatedBy",        row["CF_CREATEDBY"]),
                            Db.P("@CF_CreatedDateTime",  DateVal(row, "CF_CREATEDDATETIME")),
                            Db.P("@CF_ModifiedBy",       row["CF_MODIFIEDBY"]),
                            Db.P("@CF_ModifiedDateTime", DateVal(row, "CF_MODIFIEDDATETIME")),
                            Db.P("@DepartmentName",      row["DEPARTMENT_NAME"]),
                            Db.P("@CapexOpexDescription",row["CAPEX_OPEX_DESCRIPTION"]),
                            Db.P("@ItemType",            row["TYPE"]),
                            Db.P("@CapexOpexID",         row["CAPEX_OPEX_ID"]),
                            Db.P("@BudgetedAmount",      DecimalVal(row, "BUDGETED_AMOUNT")),
                            Db.P("@UtilizedAmount",      DecimalVal(row, "UTILIGED_AMOUNT")),
                            Db.P("@Balance",             DecimalVal(row, "BALANCE")),
                            Db.P("@LockedAmount",        DecimalVal(row, "LOCKED_AMOUNT")),
                            Db.P("@AvailableAmount",     DecimalVal(row, "AVAILABLE_AMOUNT")));
                        r.RecordsUp++;
                    }
                    catch { r.Errors++; }
                }
                r.Success = true;
            }
            catch (Exception ex) { r.ErrorMsg = ex.Message; }
            return r;
        }

        public static SyncResult SyncCapexOpexDetails(int syncId)
        {
            var r = new SyncResult { SyncType = "CapexOpexDetails" };
            try
            {
                DataTable dt = OracleDb.Query(SQL_CAPEX_OPEX_DETAILS_SIMPLE);
                r.RecordsIn = dt.Rows.Count;
                Db.Exec("TRUNCATE TABLE dbo.CapexOpexDetails");
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        Db.ExecSP("dbo.sp_SyncCapexOpexDetails",
                            Db.P("@InsertionOrderID", row["INSERTIONORDERID"]),
                            Db.P("@ItemType",         row["ITEM_TYPE"]),
                            Db.P("@ItemID",           row["ITEM_ID"]),
                            Db.P("@ItemDescription",  row["ITEM_DESCRIPTION"]),
                            Db.P("@BudgetedAmount",   DecimalVal(row, "BUDGETED_AMOUNT")),
                            Db.P("@UtilizedAmount",   DecimalVal(row, "UTILIGED_AMOUNT")),
                            Db.P("@Balance",          DecimalVal(row, "BALANCE")),
                            Db.P("@LockedAmount",     DecimalVal(row, "LOCKED_AMOUNT")),
                            Db.P("@AvailableAmount",  DecimalVal(row, "AVAILABLE_AMOUNT")),
                            Db.P("@LockedStatus",     row["LOCKED_STATUS"]),
                            Db.P("@WIName",           row["WINAME"]),
                            Db.P("@ClaimAmount",      DecimalVal(row, "CLAIM_AMOUNT")),
                            Db.P("@CpOpID",           row["CP_OP_ID"]),
                            Db.P("@BalClaimAmt",      DecimalVal(row, "BAL_CLAIM_AMT")),
                            Db.P("@OldClaimAmount",   DecimalVal(row, "OLD_CLAIM_AMOUNT")));
                        r.RecordsUp++;
                    }
                    catch { r.Errors++; }
                }
                r.Success = true;
            }
            catch (Exception ex) { r.ErrorMsg = ex.Message; }
            return r;
        }

        public static SyncResult SyncGLHistory(int syncId)
        {
            var r = new SyncResult { SyncType = "GLHistory" };
            try
            {
                DataTable dt = OracleDb.Query(SQL_GL_HISTORY);
                r.RecordsIn = dt.Rows.Count;
                Db.Exec("TRUNCATE TABLE dbo.GLHistory");
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        Db.ExecSP("dbo.sp_SyncGLHistory",
                            Db.P("@HistoryRowID",        row["HISTORYROWID"]),
                            Db.P("@CF_Sts",              row["CF_STS"]),
                            Db.P("@CF_CreatedBy",        row["CF_CREATEDBY"]),
                            Db.P("@CF_CreatedDateTime",  DateVal(row, "CF_CREATEDDATETIME")),
                            Db.P("@CF_ModifiedBy",       row["CF_MODIFIEDBY"]),
                            Db.P("@CF_ModifiedDateTime", DateVal(row, "CF_MODIFIEDDATETIME")),
                            Db.P("@GLNumber",            row["GL_NUMBER"]),
                            Db.P("@GLDescription",       row["GL_DESCRIPTON"]),
                            Db.P("@GLStatus",            row["GL_STATUS"]),
                            Db.P("@GLOpenedDate",        DateVal(row, "GL_OPENED_DATE")),
                            Db.P("@GLBudgetedAmount",    DecimalVal(row, "GL_BUDGETED_AMOUNT")),
                            Db.P("@GLUtilizedAmount",    DecimalVal(row, "GL_UTILIGED_AMOUNT")),
                            Db.P("@GLAvailableAmount",   DecimalVal(row, "GL_AVAILABLE_AMOUNT")),
                            Db.P("@GLLockedAmount",      DecimalVal(row, "GL_LOCKED_AMOUNT")),
                            Db.P("@GLTopupAmount",       DecimalVal(row, "GL_TOPUP_AMOUNT")),
                            Db.P("@GLAmountPostTopup",   DecimalVal(row, "GL_AMOUNT_POST_TOPUP")),
                            Db.P("@AmsLockedAmt",        DecimalVal(row, "AMS_LOCKED_AMT")),
                            Db.P("@CapitalizedAmount",   DecimalVal(row, "CAPITALIZED_AMOUNT")),
                            Db.P("@InvoiceAmtProcessed", DecimalVal(row, "INVOICE_AMT_PROCESSED")),
                            Db.P("@CapexOpexID",         row["CAPEX_OPEX_ID"]),
                            Db.P("@CapexBudgetedAmount", DecimalVal(row, "CAPEX_BUDGETED_AMOUNT")),
                            Db.P("@CommittedAmt",        DecimalVal(row, "COMMITTED_AMT")));
                        r.RecordsUp++;
                    }
                    catch { r.Errors++; }
                }
                r.Success = true;
            }
            catch (Exception ex) { r.ErrorMsg = ex.Message; }
            return r;
        }
    }

    public class SyncResult
    {
        public string SyncType  { get; set; }
        public bool   Success   { get; set; }
        public int    RecordsIn { get; set; }
        public int    RecordsUp { get; set; }
        public int    Errors    { get; set; }
        public string ErrorMsg  { get; set; }
    }
}
