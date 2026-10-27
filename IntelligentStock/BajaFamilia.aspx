<%@ Page Title="Baja de familia" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="BajaFamilia.aspx.cs" Inherits="BajaFamilia" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Baja de familia</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <ul class="nav nav-tabs" role="tablist">
        <li role="presentation" class="active"><a href="#tabFamilias" aria-controls="tabFamilias" role="tab" data-toggle="tab">Familias</a></li>
        <li role="presentation"><a href="#tabRoles" aria-controls="tabRoles" role="tab" data-toggle="tab">Roles</a></li>
    </ul>

    <div class="tab-content">
    <div role="tabpanel" class="tab-pane active" id="tabFamilias" style="padding-top: 15px">

    <p>
        Solo se dan de baja familias. Las patentes representan funcionalidades del
        sistema y no se eliminan.
    </p>

    <div class="row">
        <div class="col-md-6 form-group">
            <asp:Label ID="lblFamilia" runat="server" AssociatedControlID="ddlFamilias"
                CssClass="control-label" Text="Familia a eliminar"></asp:Label>
            <asp:DropDownList ID="ddlFamilias" runat="server" CssClass="form-control"
                AutoPostBack="true" OnSelectedIndexChanged="ddlFamilias_SelectedIndexChanged"></asp:DropDownList>
        </div>
    </div>

    <asp:Panel ID="pnlDetalle" runat="server" Visible="false">

        <div class="alert alert-warning">
            <asp:Literal ID="litUso" runat="server"></asp:Literal>
        </div>

        <div class="panel panel-default">
            <div class="panel-heading"><strong>Contenido de la familia</strong></div>
            <div class="panel-body">
                <asp:Literal ID="litArbol" runat="server" Mode="PassThrough"></asp:Literal>
            </div>
        </div>

        <asp:Button ID="btnEliminar" runat="server" Text="Eliminar familia" CssClass="btn btn-danger"
            OnClick="btnEliminar_Click" CausesValidation="false"
            OnClientClick="return confirm('Se va a eliminar la familia y sus asignaciones. ¿Continuar?');" />
        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
            OnClick="btnCancelar_Click" CausesValidation="false" />

    </asp:Panel>

    </div>
    <%-- ClientIDMode="Static" para que el id que sale al HTML sea "tabRoles" y no
     "MainContent_tabRoles": si no, el href="#tabRoles" de la pestaña no encuentra
     el panel y Bootstrap no cambia de pestaña. --%>
    <asp:Panel ID="tabRoles" runat="server" CssClass="tab-pane" role="tabpanel"
        ClientIDMode="Static">

    <p>
        El rol <strong>WebMaster</strong> es parte del sistema y no se puede eliminar.
        Tampoco se puede eliminar el rol de la propia cuenta, ni un rol que todavía
        tenga usuarios asignados.
    </p>

    <div class="row">
        <div class="col-md-6 form-group">
            <asp:Label ID="lblRolBaja" runat="server" AssociatedControlID="ddlRolesBaja"
                CssClass="control-label" Text="Rol a eliminar"></asp:Label>
            <asp:DropDownList ID="ddlRolesBaja" runat="server" CssClass="form-control"
                AutoPostBack="true" OnSelectedIndexChanged="ddlRolesBaja_SelectedIndexChanged"></asp:DropDownList>
        </div>
    </div>

    <asp:Panel ID="pnlDetalleRolBaja" runat="server" Visible="false">

        <div class="alert alert-warning">
            <asp:Literal ID="litUsoRolBaja" runat="server"></asp:Literal>
        </div>

        <div class="panel panel-default">
            <div class="panel-heading"><strong>Composición del rol</strong></div>
            <div class="panel-body">
                <asp:Literal ID="litArbolRolBaja" runat="server" Mode="PassThrough"></asp:Literal>
            </div>
        </div>

        <asp:Button ID="btnEliminarRolBaja" runat="server" Text="Eliminar rol" CssClass="btn btn-danger"
            OnClick="btnEliminarRolBaja_Click" CausesValidation="false"
            OnClientClick="return confirm('Se va a eliminar el rol. ¿Continuar?');" />
        <asp:Button ID="btnCancelarRolBaja" runat="server" Text="Cancelar" CssClass="btn btn-default"
            OnClick="btnCancelarRolBaja_Click" CausesValidation="false" />

    </asp:Panel>

    </asp:Panel>
    </div>

</asp:Content>
