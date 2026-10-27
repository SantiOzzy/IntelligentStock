<%@ Page Title="Alta de familia" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="AltaFamilia.aspx.cs" Inherits="AltaFamilia" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Alta de familia</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <ul class="nav nav-tabs" role="tablist">
        <li role="presentation" class="active"><a href="#tabFamilias" aria-controls="tabFamilias" role="tab" data-toggle="tab">Familias</a></li>
        <li role="presentation"><a href="#tabRoles" aria-controls="tabRoles" role="tab" data-toggle="tab">Roles</a></li>
    </ul>

    <div class="tab-content">
    <div role="tabpanel" class="tab-pane active" id="tabFamilias" style="padding-top: 15px">

    <p class="help-block">
        Una <strong>familia</strong> agrupa patentes y otras familias. Las
        <strong>patentes</strong> son los permisos individuales sobre cada funcionalidad
        del sistema: vienen definidas con la instalación y no se dan de alta desde acá.
    </p>

    <div class="row">
        <div class="col-md-4">

            <div class="form-group">
                <asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre"
                    CssClass="control-label" Text="Nombre de la familia (*)"></asp:Label>
                <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El nombre es obligatorio."></asp:RequiredFieldValidator>
            </div>

            <div class="form-group">
                <asp:Label ID="lblComponentes" runat="server" CssClass="control-label"
                    Text="Componentes que va a contener"></asp:Label>
                <div style="max-height: 340px; overflow-y: auto; border: 1px solid #ddd; padding: 8px">
                    <asp:CheckBoxList ID="cblComponentes" runat="server" CssClass="list-unstyled"
                        AutoPostBack="true" OnSelectedIndexChanged="cblComponentes_SelectedIndexChanged"
                        CausesValidation="false"></asp:CheckBoxList>
                </div>
            </div>

        </div>
        <div class="col-md-8">

            <div class="panel panel-default">
                <div class="panel-heading"><strong>Composición de la familia</strong></div>
                <div class="panel-body">
                    <asp:Literal ID="litComposicion" runat="server" Mode="PassThrough"></asp:Literal>
                </div>
            </div>

            <div class="panel panel-default">
                <div class="panel-heading"><strong>Permisos efectivos</strong></div>
                <div class="panel-body">
                    <asp:Literal ID="litEfectivos" runat="server" Mode="PassThrough"></asp:Literal>
                </div>
            </div>

        </div>
    </div>

    <asp:Button ID="btnAceptar" runat="server" Text="Aceptar" CssClass="btn btn-primary"
        ValidationGroup="Alta" OnClick="btnAceptar_Click" />
    <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar" CssClass="btn btn-default"
        OnClick="btnLimpiar_Click" CausesValidation="false" />

    </div>
    <%-- ClientIDMode="Static" para que el id que sale al HTML sea "tabRoles" y no
     "MainContent_tabRoles": si no, el href="#tabRoles" de la pestaña no encuentra
     el panel y Bootstrap no cambia de pestaña. --%>
    <asp:Panel ID="tabRoles" runat="server" CssClass="tab-pane" role="tabpanel"
        ClientIDMode="Static">

    <p class="help-block">
        Un <strong>rol</strong> es un perfil del sistema (por ejemplo Encargado o Auditor).
        Se le asignan familias y patentes exactamente igual que a una familia: de ahí en
        más, cualquier usuario con ese rol tiene esos permisos.
    </p>

    <div class="row">
        <div class="col-md-4">

            <div class="form-group">
                <asp:Label ID="lblNombreRol" runat="server" AssociatedControlID="txtNombreRol"
                    CssClass="control-label" Text="Nombre del rol (*)"></asp:Label>
                <asp:TextBox ID="txtNombreRol" runat="server" CssClass="form-control" MaxLength="50"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvNombreRol" runat="server" ControlToValidate="txtNombreRol"
                    ValidationGroup="AltaRol" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El nombre es obligatorio."></asp:RequiredFieldValidator>
            </div>

            <div class="form-group">
                <asp:Label ID="lblComponentesRol" runat="server" CssClass="control-label"
                    Text="Familias y patentes que va a otorgar"></asp:Label>
                <div style="max-height: 340px; overflow-y: auto; border: 1px solid #ddd; padding: 8px">
                    <asp:CheckBoxList ID="cblComponentesRol" runat="server" CssClass="list-unstyled"
                        AutoPostBack="true" OnSelectedIndexChanged="cblComponentesRol_SelectedIndexChanged"
                        CausesValidation="false"></asp:CheckBoxList>
                </div>
            </div>

        </div>
        <div class="col-md-8">

            <div class="panel panel-default">
                <div class="panel-heading"><strong>Composición del rol</strong></div>
                <div class="panel-body">
                    <asp:Literal ID="litComposicionRol" runat="server" Mode="PassThrough"></asp:Literal>
                </div>
            </div>

            <div class="panel panel-default">
                <div class="panel-heading"><strong>Permisos efectivos</strong></div>
                <div class="panel-body">
                    <asp:Literal ID="litEfectivosRol" runat="server" Mode="PassThrough"></asp:Literal>
                </div>
            </div>

        </div>
    </div>

    <asp:Button ID="btnAceptarRol" runat="server" Text="Aceptar" CssClass="btn btn-primary"
        ValidationGroup="AltaRol" OnClick="btnAceptarRol_Click" />
    <asp:Button ID="btnLimpiarRol" runat="server" Text="Limpiar" CssClass="btn btn-default"
        OnClick="btnLimpiarRol_Click" CausesValidation="false" />

    </asp:Panel>
    </div>

</asp:Content>
