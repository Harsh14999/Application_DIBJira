<%@ Page Title="JIRA Sync" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="JiraIntegration.aspx.cs" Inherits="DFM_BPM.Forms.JiraIntegration"
    Async="true" %>

<asp:Content ID="HeadCt" ContentPlaceHolderID="HeadContent" runat="server">
<style>
.log-box { background:#0f172a; color:#e2e8f0; font-family:monospace; font-size:.82em;
           padding:12px; border-radius:8px; max-height:450px; overflow-y:auto; white-space:pre-wrap;
           line-height:1.5; }
.log-ok   { color:#4ade80; }
.log-warn { color:#fbbf24; }
.log-err  { color:#f87171; }
.sync-stat { display:flex; gap:12px; flex-wrap:wrap; margin:10px 0; }
.sbox { background:#1e293b; color:#e2e8f0; border-radius:8px; padding:10px 16px; min-width:120px; text-align:center; }
.sbox .sv { font-size:1.8em; font-weight:900; color:#38bdf8; }
.sbox .sl { font-size:.72em; text-transform:uppercase; color:#94a3b8; }
</style>
<script>
var _syncTimer = null;
function startSyncPolling() {
    // This is called OnClientClick before postback. We just show the progress panel;
    // the actual key is written by server-side after postback starts.
    // Allow postback to proceed (return true), then poll after postback completes.
    // We use a flag to start polling after the page loads.
    window._pendingSync = true;
    document.getElementById('syncLog') && (document.getElementById('syncLog').innerHTML = '<span class="log-warn">Starting sync...</span>\n');
    return true;
}
function beginSyncPoll(key) {
    if (!key) return;
    clearInterval(_syncTimer);
    var bar = document.getElementById('syncProgressBar');
    var statusLabel = document.getElementById('<%= lblSyncStatus == null ? "lblSyncStatus" : lblSyncStatus.ClientID %>');
    _syncTimer = setInterval(function() {
        var xhr = new XMLHttpRequest();
        xhr.open('GET', '<%= ResolveUrl("~/Forms/JiraSyncProgress.ashx") %>?key=' + key + '&t=' + Date.now(), true);
        xhr.onreadystatechange = function() {
            if (xhr.readyState !== 4) return;
            try {
                var d = JSON.parse(xhr.responseText);
                if (bar) { bar.style.width = d.percent + '%'; bar.textContent = d.percent + '%'; }
                if (statusLabel) statusLabel.innerHTML = d.status;
                // Update stat boxes live
                if (d.pulled   !== undefined) setStatBox('litPulled',   d.pulled);
                if (d.inserted !== undefined) setStatBox('litInserted', d.inserted);
                if (d.updated  !== undefined) setStatBox('litUpdated',  d.updated);
                if (d.failed   !== undefined) setStatBox('litFailed',   d.failed);
                if (d.done) {
                    clearInterval(_syncTimer);
                    document.getElementById('<%= btnRunSync.ClientID %>').disabled = false;
                    if (d.error) {
                        var log = document.getElementById('syncLog');
                        if (log) log.innerHTML += '\n<span class="log-err">ERROR: ' + d.error + '</span>';
                    } else {
                        // Reload page to show updated history and final stats
                        setTimeout(function() { window.location.reload(); }, 1200);
                    }
                }
            } catch(ex) {}
        };
        xhr.send();
    }, 1000);
}
function setStatBox(litId, val) {
    var el = document.getElementById(litId);
    if (el) el.innerText = val;
}
</script>
</asp:Content>

<asp:Content ID="MainCt" ContentPlaceHolderID="MainContent" runat="server">
<h1 class="page-title"><i class="bi bi-arrow-repeat"></i> JIRA Synchronisation
    <small style="font-size:.5em;color:#64748b;font-weight:400;">Pull issues from JIRA into local database</small>
</h1>

<asp:Label ID="lblMsg" runat="server" CssClass="alert-info" Visible="false" />

<!-- ── Configuration ── -->
<div class="card-panel">
    <div class="dfm-panel-hdr" onclick="DFM.togglePanel('jiraCfgBody','jiraCfgChev')">
        <i id="jiraCfgChev" class="bi bi-chevron-right dfm-panel-chev open"></i>
        <i class="bi bi-gear"></i> JIRA Connection Settings
    </div>
    <div id="jiraCfgBody" class="dfm-panel-body" style="display:block;">
        <div class="form-grid-4" style="padding:12px;">
            <div class="form-group col-span-2">
                <label>JIRA Base URL *</label>
                <asp:TextBox ID="txtBaseUrl" runat="server" CssClass="form-control"
                    placeholder="https://your-jira.atlassian.net" />
            </div>
            <div class="form-group">
                <label>JIRA Username / Email *</label>
                <asp:TextBox ID="txtJiraUser" runat="server" CssClass="form-control"
                    placeholder="user@domain.com" />
            </div>
            <div class="form-group">
                <label>JIRA API Token / Password *</label>
                <asp:TextBox ID="txtJiraPass" runat="server" CssClass="form-control"
                    TextMode="Password" placeholder="API token or password" />
            </div>
            <div class="form-group col-span-2">
                <label>Projects (comma-separated) *</label>
                <asp:TextBox ID="txtProjects" runat="server" CssClass="form-control"
                    placeholder="DMGT,DIBITP" />
            </div>
            <div class="form-group">
                <label>Batch Size</label>
                <asp:TextBox ID="txtBatchSize" runat="server" CssClass="form-control" Text="100" />
            </div>
            <div class="form-group">
                <label>Min Batch Size</label>
                <asp:TextBox ID="txtMinBatch" runat="server" CssClass="form-control" Text="10" />
            </div>
            <div class="form-group">
                <label>HTTP Timeout (sec)</label>
                <asp:TextBox ID="txtTimeout" runat="server" CssClass="form-control" Text="120" />
            </div>
            <div class="form-group">
                <label>Max Retry Attempts</label>
                <asp:TextBox ID="txtMaxAttempts" runat="server" CssClass="form-control" Text="3" />
            </div>
            <div class="form-group col-span-4">
                <label>Custom Fields Override <small style="color:#94a3b8;">(leave blank = use default field list; enter fields=&lt;list&gt; for custom)</small></label>
                <asp:TextBox ID="txtFields" runat="server" CssClass="form-control"
                    placeholder="summary,status,issuetype,... (blank = use default heavy field list)" />
            </div>
        </div>
        <div style="padding:0 12px 12px;display:flex;gap:8px;flex-wrap:wrap;">
            <asp:Button ID="btnSaveConfig" runat="server" CssClass="btn btn-default"
                Text="Save to Web.config" OnClick="btnSaveConfig_Click" CausesValidation="false" />
            <asp:Button ID="btnRunSync" runat="server" CssClass="btn btn-primary"
                Text="&#9654;  Run JIRA Sync Now" OnClick="btnRunSync_Click"
                OnClientClick="return startSyncPolling();" />
            <asp:Button ID="btnApplyHierarchy" runat="server" CssClass="btn btn-secondary"
                Text="Apply Hierarchy Only" OnClick="btnApplyHierarchy_Click" CausesValidation="false" />
            <asp:Button ID="btnRefreshDash" runat="server" CssClass="btn btn-secondary"
                Text="Refresh Dashboard Only" OnClick="btnRefreshDash_Click" CausesValidation="false" />
        </div>

        <!-- Sync progress (shown while async sync runs) -->
        <asp:HiddenField ID="hfSyncKey" runat="server" />
        <asp:Panel ID="pnlSyncProgress" runat="server" Visible="false">
        <div style="padding:0 12px 12px;">
            <div style="font-size:.82em;color:#94a3b8;margin-bottom:6px;">
                <asp:Label ID="lblSyncStatus" runat="server" Text="Starting..." />
            </div>
            <div class="progress" style="height:22px;border-radius:6px;background:#1e293b;">
                <div id="syncProgressBar" class="progress-bar progress-bar-striped active"
                     role="progressbar" style="width:0%;min-width:30px;transition:width .4s ease;">0%</div>
            </div>
        </div>
        </asp:Panel>
        </div>
    </div>
</div>

<!-- ── Stats ── -->
<div class="sync-stat">
    <div class="sbox"><div class="sv"><asp:Literal ID="litPulled"   runat="server" Text="–" /></div><div class="sl">Pulled</div></div>
    <div class="sbox"><div class="sv"><asp:Literal ID="litInserted" runat="server" Text="–" /></div><div class="sl">Inserted</div></div>
    <div class="sbox"><div class="sv"><asp:Literal ID="litUpdated"  runat="server" Text="–" /></div><div class="sl">Updated</div></div>
    <div class="sbox"><div class="sv"><asp:Literal ID="litFailed"   runat="server" Text="–" /></div><div class="sl">Failed</div></div>
    <div class="sbox" style="flex:1;"><div class="sv" style="font-size:1em;"><asp:Literal ID="litDuration" runat="server" Text="–" /></div><div class="sl">Duration</div></div>
    <div class="sbox" style="flex:2;"><div class="sv" style="font-size:1em;"><asp:Literal ID="litLastStatus" runat="server" Text="–" /></div><div class="sl">Last Run Status</div></div>
</div>

<!-- ── Log Output ── -->
<div class="card-panel">
    <div class="card-panel-hdr"><i class="bi bi-terminal"></i> Sync Log
        <asp:Button ID="btnClearLog" runat="server" CssClass="btn btn-xs btn-default" Text="Clear"
            CausesValidation="false" OnClientClick="document.getElementById('syncLog').innerHTML=''; return false;"
            style="margin-left:auto;" />
    </div>
    <div class="card-panel-body" style="padding:0;">
        <div id="syncLog" class="log-box">
            <asp:Literal ID="litLog" runat="server" />
        </div>
    </div>
</div>

<!-- ── Sync History ── -->
<div class="card-panel">
    <div class="card-panel-hdr"><i class="bi bi-clock-history"></i> Recent Sync History</div>
    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
        <asp:GridView ID="gvSyncHistory" runat="server" AutoGenerateColumns="false"
            CssClass="dfm-table" GridLines="None" EmptyDataText="No sync history.">
            <Columns>
                <asp:BoundField DataField="SyncID"    HeaderText="ID" />
                <asp:BoundField DataField="StartTime" HeaderText="Start" DataFormatString="{0:dd-MMM-yyyy HH:mm}" />
                <asp:BoundField DataField="EndTime"   HeaderText="End"   DataFormatString="{0:dd-MMM-yyyy HH:mm}" />
                <asp:BoundField DataField="Status"    HeaderText="Status" />
                <asp:BoundField DataField="PulledCount"   HeaderText="Pulled" />
                <asp:BoundField DataField="InsertedCount" HeaderText="Inserted" />
                <asp:BoundField DataField="UpdatedCount"  HeaderText="Updated" />
                <asp:BoundField DataField="FailedCount"   HeaderText="Failed" />
                <asp:BoundField DataField="TriggeredBy"   HeaderText="By" />
            </Columns>
        </asp:GridView>
    </div>
</div>
</asp:Content>
