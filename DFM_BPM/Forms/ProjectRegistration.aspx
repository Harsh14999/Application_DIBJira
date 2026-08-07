<%@ Page Title="Project Portfolio" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="ProjectRegistration.aspx.cs" Inherits="DFM_BPM.Forms.ProjectRegistration" %>

<asp:Content ID="HeadCt" ContentPlaceHolderID="HeadContent" runat="server">
<link href="<%= ResolveUrl("~/Content/bootstrap-icons.css") %>" rel="stylesheet" />
<link href="<%= ResolveUrl("~/Content/select2.min.css") %>" rel="stylesheet" />
<style>
.jira-detail-tbl td { border:1px solid #e5e7eb; padding:7px 12px; vertical-align:middle; font-size:.87em; }
.jira-detail-tbl td.lbl { background:#f8f9fa; font-weight:700; color:#374151; width:18%; white-space:nowrap; }
.jira-detail-tbl td.lbl i.bi { color:#2563eb; margin-right:5px; }
.jira-detail-tbl td.val { background:#fff; width:32%; }
.card-panel { border-radius:8px; overflow:hidden; border:1px solid #e2e8f0; margin-bottom:14px; }
.card-panel-hdr { padding:12px 14px; font-weight:700; font-size:.95em; display:flex; align-items:center; gap:8px; }
.card-panel-body { padding:14px; }
.card-panel.panel-spend-request { border-color:#B4C7E7; background:#F5F9FF; }
.card-panel.panel-spend-request .card-panel-hdr { background:#F5F9FF; color:#2F5597; border-bottom:2px solid #2F5597; }
.card-panel.panel-spend-request .dfm-table th { background:#2F5597 !important; color:#fff; }
.dfm-table th { background:#1e3a5f !important; color:#fff !important; font-size:.8em; padding:6px 8px; white-space:nowrap; }
.dfm-table td { font-size:.82em; padding:5px 8px; vertical-align:middle; }
.dfm-table tr:nth-child(odd)  td { background:#fafbff; }
.dfm-table tr:nth-child(even) td { background:#ffffff; }
.dfm-table tr:hover td { background:#eff6ff; }
.project-modal .modal-dialog { width:95%; max-width:1100px; }
.project-modal .modal-body { max-height:76vh; overflow-y:auto; padding:16px; }
.portfolio-toolbar { display:flex; justify-content:space-between; align-items:center; gap:10px; flex-wrap:wrap; margin-bottom:12px; }
</style>
</asp:Content>

<asp:Content ID="MainCt" ContentPlaceHolderID="MainContent" runat="server">
<h1 class="page-title"><i class="bi bi-folder-plus"></i> Project Portfolio
    <% if (IsExistingProject) { %>
        &nbsp;<span style="font-size:.6em;color:#2563eb;"><%= Server.HtmlEncode(CurrentProjectId) %></span>
    <% } %>
    <% if (CanDeleteProject) { %>
    <span style="float:right;margin-top:4px;">
        <asp:Button ID="btnDeleteProject" runat="server" CssClass="btn btn-sm btn-danger"
            Text="Delete This Project" OnClientClick="$('#projectDelModal').modal('show');return false;"
            CausesValidation="false" />
    </span>
    <% } %>
</h1>

<asp:Label ID="lblMsg" runat="server" CssClass="alert alert-info" Visible="false" />
<asp:HiddenField ID="hfProjectId" runat="server" Value="" />

<div class="portfolio-toolbar">
    <div style="font-size:.9em;color:#64748b;">Registered Projects (<asp:Literal ID="litProjectPortfolioCount" runat="server" Text="0" />)</div>
    <asp:Button ID="btnNewProject" runat="server" CssClass="btn btn-primary" Text="New Project" OnClick="btnNewProject_Click" CausesValidation="false" />
</div>

<div class="card-panel panel-spend-request">
    <div class="card-panel-hdr"><i class="bi bi-table"></i> Project Portfolio</div>
    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
        <asp:GridView ID="gvProjectPortfolio" runat="server" AutoGenerateColumns="false"
            CssClass="dfm-table" GridLines="None" DataKeyNames="ProjectID" OnRowCommand="gvProjectPortfolio_RowCommand"
            EmptyDataText="No projects registered yet.">
            <Columns>
                <asp:BoundField DataField="ProjectID" HeaderText="Project ID" />
                <asp:BoundField DataField="ProjectName" HeaderText="Project Name" />
                <asp:TemplateField HeaderText="Project Type">
                    <ItemTemplate><%# Convert.ToBoolean(Eval("IsNonJiraProject")) ? "Non-JIRA" : "JIRA" %></ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="AccountableExecLead" HeaderText="Accountable Exec Lead" />
                <asp:BoundField DataField="SmeLead" HeaderText="SME Lead" />
                <asp:BoundField DataField="ProjectManager" HeaderText="Project Manager" />
                <asp:BoundField DataField="CreatedBy" HeaderText="Requestor" />
                <asp:TemplateField HeaderText="Status">
                    <ItemTemplate><%# Convert.ToBoolean(Eval("IsActive")) ? "<span class='badge-success'>Active</span>" : "<span class='badge-danger'>Inactive</span>" %></ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="CreatedDate" HeaderText="Created Date" DataFormatString="{0:dd-MMM-yyyy}" />
                <asp:TemplateField HeaderText="Action">
                    <ItemTemplate>
                        <asp:LinkButton runat="server" CssClass="btn btn-xs btn-primary" CommandName="EditProject"
                            CommandArgument='<%# Eval("ProjectID") %>' CausesValidation="false">
                            <i class="bi bi-pencil"></i> Edit
                        </asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>
</div>

<div class="modal fade project-modal" id="projectRegistrationModal" tabindex="-1" role="dialog">
    <div class="modal-dialog" role="document">
        <div class="modal-content">
            <div class="modal-header" style="background:#1a3c5e;color:#fff;">
                <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                <h4 class="modal-title"><i class="bi bi-pencil-square"></i> <%= IsExistingProject ? "Edit Project" : "New Project" %></h4>
            </div>
            <div class="modal-body">
        <asp:Panel ID="pnlCreatedInfo" runat="server" Visible="false" style="margin-bottom:10px;font-size:.85em;color:#64748b;">
            <asp:Literal ID="litCreatedInfo" runat="server" />
        </asp:Panel>

        <table class="table jira-detail-tbl">
            <tbody>
                <tr>
                    <td class="lbl"><i class="bi bi-diagram-2"></i>Project Type</td>
                    <td class="val" colspan="3">
                        <asp:RadioButtonList ID="rblProjectMode" runat="server" RepeatDirection="Horizontal"
                            AutoPostBack="true" OnSelectedIndexChanged="rblProjectMode_Changed">
                            <asp:ListItem Text="JIRA Project" Value="JIRA" Selected="True" />
                            <asp:ListItem Text="Non-JIRA Project" Value="NONJIRA" />
                        </asp:RadioButtonList>
                        <small style="color:#64748b;display:block;">For Non-JIRA projects, Project Name/ID are entered manually.</small>
                    </td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-key"></i>JIRA ID / Project ID <span style="color:#dc2626;">*</span></td>
                    <td class="val">
                        <asp:PlaceHolder ID="phJiraSelect" runat="server">
                            <asp:DropDownList ID="ddlProject" runat="server" CssClass="form-control select2-enable"
                                AutoPostBack="true" OnSelectedIndexChanged="ddlProject_Changed" />
                        </asp:PlaceHolder>
                        <asp:PlaceHolder ID="phNonJiraSelect" runat="server" Visible="false">
                            <asp:TextBox ID="txtNonJiraProjectId" runat="server" CssClass="form-control"
                                placeholder="Enter a free-text Project ID" />
                        </asp:PlaceHolder>
                    </td>
                    <td class="lbl"><i class="bi bi-file-earmark-text"></i>Project Name <span style="color:#dc2626;">*</span></td>
                    <td class="val">
                        <asp:TextBox ID="txtProjectName" runat="server" CssClass="form-control" ReadOnly="true" />
                    </td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-person-vcard"></i>Project Manager</td>
                    <td class="val" colspan="3">
                        <asp:TextBox ID="txtProjectManager" runat="server" CssClass="form-control" ReadOnly="true" />
                    </td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-toggle-on"></i>Status</td>
                    <td class="val">
                        <asp:DropDownList ID="ddlActive" runat="server" CssClass="form-control">
                            <asp:ListItem Text="Active" Value="Yes" />
                            <asp:ListItem Text="Inactive" Value="No" />
                        </asp:DropDownList>
                    </td>
                    <td class="lbl" colspan="2"></td>
                </tr>
            </tbody>
        </table>
            </div>
            <div class="modal-footer">
        <asp:Button ID="btnSave" runat="server" CssClass="btn btn-primary" Text="Save Project" OnClick="btnSave_Click" />
        <% if (IsExistingProject) { %>
        <a href='<%= ResolveUrl("~/Forms/PetWorkflow.aspx") %>?project=<%= Server.UrlEncode(CurrentProjectId) %>' class="btn btn-success">
            <i class="bi bi-plus-circle"></i> Create Spend Request for this Project
        </a>
        <% } %>
                <button type="button" class="btn btn-default" data-dismiss="modal"><i class="bi bi-x-lg"></i> Close</button>
            </div>
        </div>
    </div>
</div>

<asp:Panel ID="pnlProjectDetails" runat="server" Visible="false">
<div class="card-panel panel-spend-request">
    <div class="card-panel-hdr"><i class="bi bi-folder2-open"></i> JIRA Project Snapshot</div>
    <div class="card-panel-body">
        <table class="table jira-detail-tbl mb-0">
            <tbody>
                <tr>
                    <td class="lbl"><i class="bi bi-lightning"></i>Project Type</td>
                    <td class="val"><asp:Literal ID="litJProjectType" runat="server" /></td>
                    <td class="lbl"><i class="bi bi-graph-up"></i>Stage</td>
                    <td class="val"><asp:Literal ID="litJStage" runat="server" /></td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-circle-fill"></i>RAG Status</td>
                    <td class="val"><asp:Literal ID="litJRag" runat="server" /></td>
                    <td class="lbl"><i class="bi bi-building"></i>Department</td>
                    <td class="val"><asp:Literal ID="litJDept" runat="server" /></td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-cpu"></i>Platform</td>
                    <td class="val"><asp:Literal ID="litJPlatform" runat="server" /></td>
                    <td class="lbl"><i class="bi bi-tools"></i>Tech Lead</td>
                    <td class="val"><asp:Literal ID="litJTech" runat="server" /></td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-person-gear"></i>Accountable Exec</td>
                    <td class="val"><asp:Literal ID="litJAccExec" runat="server" /></td>
                    <td class="lbl"><i class="bi bi-person-check"></i>Accountable Exec Lead</td>
                    <td class="val"><asp:Literal ID="litJAccExecLead" runat="server" /></td>
                </tr>
                <tr>
                    <td class="lbl"><i class="bi bi-person-workspace"></i>SME Lead</td>
                    <td class="val"><asp:Literal ID="litJSmeLead" runat="server" /></td>
                    <td class="lbl"><i class="bi bi-check2-all"></i>Project Overall Status</td>
                    <td class="val"><asp:Literal ID="litJOverallStatus" runat="server" /></td>
                </tr>
            </tbody>
        </table>
    </div>
</div>
</asp:Panel>
<asp:Panel ID="pnlNoProject" runat="server">
    <div class="alert alert-info"><asp:Literal ID="litNoProjectMsg" runat="server" Text="Select a JIRA project (or enter a Non-JIRA Project ID) above to see its details here." /></div>
</asp:Panel>

<asp:Panel ID="pnlProjectPets" runat="server" Visible="false">
<div class="card-panel panel-spend-request">
    <div class="card-panel-hdr"><i class="bi bi-file-earmark-text"></i> Spend Requests for this Project</div>
    <div class="card-panel-body" style="padding:0;overflow-x:auto;">
        <asp:GridView ID="gvProjectPets" runat="server" AutoGenerateColumns="false"
            CssClass="dfm-table" GridLines="None" EmptyDataText="No Spend Requests raised yet for this project.">
            <Columns>
                <asp:BoundField DataField="PetRefNo"      HeaderText="Ref No" />
                <asp:BoundField DataField="CapexOpexType" HeaderText="Type" />
                <asp:BoundField DataField="Title"         HeaderText="Title" />
                <asp:BoundField DataField="Status"        HeaderText="Status" />
                <asp:BoundField DataField="CreatedBy"     HeaderText="Requestor" />
                <asp:BoundField DataField="CreatedDate"   HeaderText="Created" DataFormatString="{0:dd-MMM-yyyy}" />
                <asp:TemplateField HeaderText="Action">
                    <ItemTemplate>
                        <a href='<%# ResolveUrl("~/Forms/PetWorkflow.aspx") %>?id=<%# Eval("PetFormID") %>' class="btn btn-xs btn-primary"><i class="bi bi-arrow-right-circle"></i> Open</a>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>
</div>
</asp:Panel>

<!-- ── PROJECT SIZING (1 per project, editable any time) ── -->
<asp:Panel ID="pnlSizing" runat="server" Visible="false">
<div class="card-panel panel-spend-request">
    <div class="card-panel-hdr"><i class="bi bi-rulers"></i> Project Sizing
        <asp:Literal ID="litSizingBadge" runat="server" />
    </div>
    <div class="card-panel-body">
        <asp:Literal ID="litSizingSavedInfo" runat="server" />
        <div style="background:#f0f9ff;border:1px solid #bae6fd;border-radius:8px;padding:12px 16px;margin-bottom:14px;font-size:.84em;color:#0c4a6e;">
            <strong>Scoring Guide:</strong> Select Low (1), Medium (3) or High (5) for each criterion.
            Weighted total &rarr; Size: <strong>XS</strong> (&le;1.5) | <strong>S</strong> (1.5&ndash;2.3) | <strong>M</strong> (2.3&ndash;3.2) | <strong>L</strong> (3.2&ndash;4.1) | <strong>XL</strong> (&gt;4.1)
        </div>

        <div class="ps-card">
            <div class="ps-card-title">1. Technical / Service Complexity <span class="label label-default" style="font-size:.7em;">Weight: 20%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q1" id="prsz_q1_1" value="1" onchange="prszScore()" /><label for="prsz_q1_1" class="low">&#10003; Low (1)<br/><small>Existing platforms, proven tech</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q1" id="prsz_q1_3" value="3" onchange="prszScore()" /><label for="prsz_q1_3" class="medium">&#9888; Medium (3)<br/><small>Some custom design, limited novelty</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q1" id="prsz_q1_5" value="5" onchange="prszScore()" /><label for="prsz_q1_5" class="high">&#9940; High (5)<br/><small>New/unproven tech, complex integrations</small></label></div>
            </div>
        </div>
        <div class="ps-card">
            <div class="ps-card-title">2. Regulatory / Compliance / Security <span class="label label-default" style="font-size:.7em;">Weight: 20%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q2" id="prsz_q2_1" value="1" onchange="prszScore()" /><label for="prsz_q2_1" class="low">&#10003; Low (1)<br/><small>No regulated data, standard security</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q2" id="prsz_q2_3" value="3" onchange="prszScore()" /><label for="prsz_q2_3" class="medium">&#9888; Medium (3)<br/><small>Compliance / privacy requirements</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q2" id="prsz_q2_5" value="5" onchange="prszScore()" /><label for="prsz_q2_5" class="high">&#9940; High (5)<br/><small>High regulatory/audit exposure</small></label></div>
            </div>
        </div>
        <div class="ps-card">
            <div class="ps-card-title">3. Stakeholder Complexity <span class="label label-default" style="font-size:.7em;">Weight: 15%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q3" id="prsz_q3_1" value="1" onchange="prszScore()" /><label for="prsz_q3_1" class="low">&#10003; Low (1)<br/><small>Single business owner, aligned</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q3" id="prsz_q3_3" value="3" onchange="prszScore()" /><label for="prsz_q3_3" class="medium">&#9888; Medium (3)<br/><small>Multiple BUs, competing priorities</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q3" id="prsz_q3_5" value="5" onchange="prszScore()" /><label for="prsz_q3_5" class="high">&#9940; High (5)<br/><small>Many stakeholders, divergent interests</small></label></div>
            </div>
        </div>
        <div class="ps-card">
            <div class="ps-card-title">4. Resource / Capability Dependency <span class="label label-default" style="font-size:.7em;">Weight: 15%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q4" id="prsz_q4_1" value="1" onchange="prszScore()" /><label for="prsz_q4_1" class="low">&#10003; Low (1)<br/><small>Skills available internally</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q4" id="prsz_q4_3" value="3" onchange="prszScore()" /><label for="prsz_q4_3" class="medium">&#9888; Medium (3)<br/><small>Some external specialists</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q4" id="prsz_q4_5" value="5" onchange="prszScore()" /><label for="prsz_q4_5" class="high">&#9940; High (5)<br/><small>Critical niche skills, major hiring</small></label></div>
            </div>
        </div>
        <div class="ps-card">
            <div class="ps-card-title">5. Scale / Reliability / Performance <span class="label label-default" style="font-size:.7em;">Weight: 15%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q5" id="prsz_q5_1" value="1" onchange="prszScore()" /><label for="prsz_q5_1" class="low">&#10003; Low (1)<br/><small>Non-critical, degradation acceptable</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q5" id="prsz_q5_3" value="3" onchange="prszScore()" /><label for="prsz_q5_3" class="medium">&#9888; Medium (3)<br/><small>Normal production SLAs</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q5" id="prsz_q5_5" value="5" onchange="prszScore()" /><label for="prsz_q5_5" class="high">&#9940; High (5)<br/><small>Mission-critical, strict HA/SLA</small></label></div>
            </div>
        </div>
        <div class="ps-card">
            <div class="ps-card-title">6. Interdependencies / Portfolio <span class="label label-default" style="font-size:.7em;">Weight: 10%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q6" id="prsz_q6_1" value="1" onchange="prszScore()" /><label for="prsz_q6_1" class="low">&#10003; Low (1)<br/><small>Standalone, few dependencies</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q6" id="prsz_q6_3" value="3" onchange="prszScore()" /><label for="prsz_q6_3" class="medium">&#9888; Medium (3)<br/><small>Some upstream/downstream deps</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q6" id="prsz_q6_5" value="5" onchange="prszScore()" /><label for="prsz_q6_5" class="high">&#9940; High (5)<br/><small>Foundational, impacts many initiatives</small></label></div>
            </div>
        </div>
        <div class="ps-card">
            <div class="ps-card-title">7. Budget / Contract Complexity <span class="label label-default" style="font-size:.7em;">Weight: 5%</span></div>
            <div class="ps-radio-group">
                <div class="ps-radio-btn"><input type="radio" name="prsz_q7" id="prsz_q7_1" value="1" onchange="prszScore()" /><label for="prsz_q7_1" class="low">&#10003; Low (1)<br/><small>Small budget, simple procurement</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q7" id="prsz_q7_3" value="3" onchange="prszScore()" /><label for="prsz_q7_3" class="medium">&#9888; Medium (3)<br/><small>Multi-phase funding, complex terms</small></label></div>
                <div class="ps-radio-btn"><input type="radio" name="prsz_q7" id="prsz_q7_5" value="5" onchange="prszScore()" /><label for="prsz_q7_5" class="high">&#9940; High (5)<br/><small>Large capital, strategic supplier</small></label></div>
            </div>
        </div>

        <div id="prszLiveResult" class="ps-result-panel">
            <div class="ps-result-badge" id="prszLiveBadge">--</div>
            <div id="prszLiveLabel" style="font-size:1em;font-weight:600;margin-bottom:6px;"></div>
            <div class="ps-score-bar"><div class="ps-score-fill" id="prszScoreFill" style="width:0%;background:#facc15;"></div></div>
            <div style="font-size:.8em;opacity:.8;" id="prszScoreText">Select all 7 criteria to see score</div>
        </div>

        <asp:HiddenField ID="hfSzQ1" runat="server" /><asp:HiddenField ID="hfSzQ2" runat="server" />
        <asp:HiddenField ID="hfSzQ3" runat="server" /><asp:HiddenField ID="hfSzQ4" runat="server" />
        <asp:HiddenField ID="hfSzQ5" runat="server" /><asp:HiddenField ID="hfSzQ6" runat="server" />
        <asp:HiddenField ID="hfSzQ7" runat="server" />

        <div style="margin-top:16px;display:flex;gap:10px;flex-wrap:wrap;align-items:center;">
            <asp:Button ID="btnSizingSave" runat="server" Text="Save Sizing" CssClass="btn btn-primary"
                OnClientClick="return prSzPreSave();" OnClick="btnSizingSave_Click" CausesValidation="false" />
            <button type="button" class="btn btn-default" onclick="prszClear(); return false;">
                <i class="bi bi-arrow-clockwise"></i> Reset
            </button>
        </div>
    </div>
</div>
</asp:Panel>

<script>
window.addEventListener('load', function() {
    // Pre-select from hidden fields (edit mode) so the previously-saved answers show when re-opening.
    for (var q = 1; q <= 7; q++) {
        var hf = document.getElementById('<%= hfSzQ1.ClientID %>'.replace('hfSzQ1', 'hfSzQ' + q));
        if (!hf || !hf.value) continue;
        var radios = document.getElementsByName('prsz_q' + q);
        for (var i = 0; i < radios.length; i++) { if (radios[i].value === hf.value) { radios[i].checked = true; break; } }
    }
    if (typeof prszScore === 'function') prszScore();

    // select2 for all dropdowns marked (JIRA picker + hierarchy cascading pickers)
    if (typeof jQuery !== 'undefined' && jQuery.fn && jQuery.fn.select2) {
        jQuery('.select2-enable').select2({ width: '100%' });
    }
});

var prszWeights = [0.20, 0.20, 0.15, 0.15, 0.15, 0.10, 0.05];
function prszScore() {
    var total = 0;
    for (var q = 1; q <= 7; q++) {
        var radios = document.getElementsByName('prsz_q' + q);
        var val = 0;
        for (var i = 0; i < radios.length; i++) { if (radios[i].checked) { val = parseFloat(radios[i].value); break; } }
        total += val * prszWeights[q - 1];
    }
    var res = document.getElementById('prszLiveResult');
    var badge = document.getElementById('prszLiveBadge');
    var lbl   = document.getElementById('prszLiveLabel');
    var fill  = document.getElementById('prszScoreFill');
    var txt   = document.getElementById('prszScoreText');
    if (!res || total === 0) return;
    res.className = 'ps-result-panel visible';
    txt.textContent = 'Total Weighted Score: ' + total.toFixed(2);
    fill.style.width = Math.min(100, Math.max(0, ((total - 1) / 4) * 100)) + '%';
    var size, cls, color;
    if (total <= 1.5)      { size='XS'; cls='ps-result-xs'; color='#22c55e'; }
    else if (total <= 2.3) { size='S';  cls='ps-result-s';  color='#4ade80'; }
    else if (total <= 3.2) { size='M';  cls='ps-result-m';  color='#facc15'; }
    else if (total <= 4.1) { size='L';  cls='ps-result-l';  color='#fb923c'; }
    else                   { size='XL'; cls='ps-result-xl'; color='#f87171'; }
    badge.textContent = size;
    lbl.textContent   = 'Project Size: ' + size;
    fill.style.background = color;
    res.className = 'ps-result-panel visible ' + cls;
}
function prszClear() {
    for (var q = 1; q <= 7; q++) {
        var radios = document.getElementsByName('prsz_q' + q);
        for (var i = 0; i < radios.length; i++) radios[i].checked = false;
    }
    var res = document.getElementById('prszLiveResult');
    if (res) res.className = 'ps-result-panel';
}
function prSzPreSave() {
    for (var q = 1; q <= 7; q++) {
        var radios = document.getElementsByName('prsz_q' + q); var val = '';
        for (var i = 0; i < radios.length; i++) { if (radios[i].checked) { val = radios[i].value; break; } }
        if (!val) { alert('Please select a rating (Low / Medium / High) for all 7 criteria before saving.'); return false; }
        var hf = document.getElementById('<%= hfSzQ1.ClientID %>'.replace('hfSzQ1', 'hfSzQ' + q));
        if (hf) hf.value = val;
    }
    return true;
}
</script>

<!-- Project Delete Confirmation Modal -->
<div class="modal fade" id="projectDelModal" tabindex="-1" role="dialog">
    <div class="modal-dialog" role="document" style="max-width:460px;">
        <div class="modal-content">
            <div class="modal-header" style="background:#b91c1c;color:#fff;">
                <button type="button" class="close" data-dismiss="modal" style="color:#fff;opacity:.8;">&times;</button>
                <h4 class="modal-title"><i class="bi bi-exclamation-triangle-fill"></i> Confirm Delete Project</h4>
            </div>
            <div class="modal-body" style="padding:24px;text-align:center;">
                <div style="font-size:2.8em;color:#dc2626;margin-bottom:10px;"><i class="bi bi-trash3-fill"></i></div>
                <p style="font-size:.94em;font-weight:600;color:#1e293b;margin-bottom:4px;">Are you sure you want to delete</p>
                <p style="font-size:1.1em;font-weight:800;color:#dc2626;"><%= Server.HtmlEncode(CurrentProjectId) %></p>
                <p style="font-size:.82em;color:#64748b;margin-top:8px;">This permanently removes the project record and its Sizing record. This cannot be undone.</p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-default" data-dismiss="modal"><i class="bi bi-x-lg"></i> Cancel</button>
                <asp:Button ID="btnConfirmDeleteProject" runat="server" CssClass="btn btn-danger"
                    Text="Yes, Delete" OnClick="btnDeleteProject_Click" CausesValidation="false" />
            </div>
        </div>
    </div>
</div>

</asp:Content>
