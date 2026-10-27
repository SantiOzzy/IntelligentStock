<%@ Page Title="Modificación de familia" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ModificacionFamilia.aspx.cs" Inherits="ModificacionFamilia" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Modificación de familia</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <ul class="nav nav-tabs" role="tablist">
        <li role="presentation" class="active"><a href="#tabFamilias" aria-controls="tabFamilias" role="tab" data-toggle="tab">Familias</a></li>
        <li role="presentation"><a href="#tabRoles" aria-controls="tabRoles" role="tab" data-toggle="tab">Roles</a></li>
    </ul>

    <div class="tab-content">
    <div role="tabpanel" class="tab-pane active" id="tabFamilias" style="padding-top: 15px">

    <div class="panel panel-default">
        <div class="panel-heading"><strong>Familia a modificar</strong></div>
        <div class="panel-body">
            <div class="row">
                <div class="col-md-6 form-group">
                    <asp:DropDownList ID="ddlFamilias" runat="server" CssClass="form-control"
                        AutoPostBack="true" OnSelectedIndexChanged="ddlFamilias_SelectedIndexChanged"></asp:DropDownList>
                </div>
            </div>
        </div>
    </div>

    <asp:Panel ID="pnlEdicion" runat="server" Visible="false">

        <div class="row">
            <div class="col-md-6 form-group">
                <asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre"
                    CssClass="control-label" Text="Nombre (*)"></asp:Label>
                <div class="input-group">
                    <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                    <span class="input-group-btn">
                        <asp:Button ID="btnRenombrar" runat="server" Text="Guardar nombre"
                            CssClass="btn btn-primary" ValidationGroup="Nombre" OnClick="btnRenombrar_Click" />
                    </span>
                </div>
                <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                    ValidationGroup="Nombre" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El nombre es obligatorio."></asp:RequiredFieldValidator>
            </div>
        </div>

        <div class="panel panel-default">
            <div class="panel-heading"><strong>Composición de la familia</strong></div>
            <div class="panel-body">
                <p class="help-block">
                    Los cambios de composición se guardan al instante: no hace falta
                    volver a presionar "Guardar nombre". Si un permiso ya está incluido
                    a través de otra familia, el sistema lo rechaza para no repetirlo.
                </p>
                <div class="row">

                    <div class="col-md-5">
                        <label class="control-label">Disponibles</label>
                        <asp:ListBox ID="lstDisponibles" runat="server" CssClass="form-control"
                            Rows="14" SelectionMode="Multiple"></asp:ListBox>
                    </div>

                    <div class="col-md-2 text-center" style="padding-top: 90px">
                        <asp:Button ID="btnAgregar" runat="server" Text="Agregar &gt;&gt;"
                            CssClass="btn btn-success btn-block" OnClick="btnAgregar_Click" CausesValidation="false" />
                        <br />
                        <asp:Button ID="btnQuitar" runat="server" Text="&lt;&lt; Quitar"
                            CssClass="btn btn-warning btn-block" OnClick="btnQuitar_Click" CausesValidation="false" />
                    </div>

                    <div class="col-md-5">
                        <label class="control-label">Contenidos en la familia</label>
                        <asp:ListBox ID="lstAsignados" runat="server" CssClass="form-control"
                            Rows="14" SelectionMode="Multiple"></asp:ListBox>
                    </div>

                </div>
            </div>
        </div>

        <div class="panel panel-default">
            <div class="panel-heading"><strong>Árbol resultante</strong></div>
            <div class="panel-body">
                <asp:Literal ID="litArbol" runat="server" Mode="PassThrough"></asp:Literal>
            </div>
        </div>

    </asp:Panel>

    </div>
    <%-- ClientIDMode="Static" para que el id que sale al HTML sea "tabRoles" y no
     "MainContent_tabRoles": si no, el href="#tabRoles" de la pestaña no encuentra
     el panel y Bootstrap no cambia de pestaña. --%>
    <asp:Panel ID="tabRoles" runat="server" CssClass="tab-pane" role="tabpanel"
        ClientIDMode="Static">

    <div class="panel panel-default">
        <div class="panel-heading"><strong>Rol a modificar</strong></div>
        <div class="panel-body">
            <div class="row">
                <div class="col-md-6 form-group">
                    <asp:DropDownList ID="ddlRolMod" runat="server" CssClass="form-control"
                        AutoPostBack="true" OnSelectedIndexChanged="ddlRolMod_SelectedIndexChanged"></asp:DropDownList>
                </div>
            </div>
        </div>
    </div>

    <asp:Panel ID="pnlEdicionRolMod" runat="server" Visible="false">

        <asp:Label ID="lblAvisoRolMod" runat="server" CssClass="alert alert-warning" Visible="false"></asp:Label>

        <div class="row">
            <div class="col-md-6 form-group">
                <asp:Label ID="lblNombreRolMod" runat="server" AssociatedControlID="txtNombreRolMod"
                    CssClass="control-label" Text="Nombre (*)"></asp:Label>
                <div class="input-group">
                    <asp:TextBox ID="txtNombreRolMod" runat="server" CssClass="form-control" MaxLength="50"></asp:TextBox>
                    <span class="input-group-btn">
                        <asp:Button ID="btnRenombrarRolMod" runat="server" Text="Guardar nombre"
                            CssClass="btn btn-primary" ValidationGroup="NombreRol" OnClick="btnRenombrarRolMod_Click" />
                    </span>
                </div>
                <asp:RequiredFieldValidator ID="rfvNombreRolMod" runat="server" ControlToValidate="txtNombreRolMod"
                    ValidationGroup="NombreRol" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El nombre es obligatorio."></asp:RequiredFieldValidator>
            </div>
        </div>

        <div class="panel panel-default">
            <div class="panel-heading"><strong>Composición del rol</strong></div>
            <div class="panel-body">
                <p class="help-block">
                    Los cambios de composición se guardan al instante. No se puede
                    editar la composición del rol de su propia cuenta.
                </p>
                <div class="row">

                    <div class="col-md-5">
                        <label class="control-label">Disponibles</label>
                        <asp:ListBox ID="lstDisponiblesRolMod" runat="server" CssClass="form-control"
                            Rows="14" SelectionMode="Multiple"></asp:ListBox>
                    </div>

                    <div class="col-md-2 text-center" style="padding-top: 90px">
                        <asp:Button ID="btnAgregarRolMod" runat="server" Text="Agregar &gt;&gt;"
                            CssClass="btn btn-success btn-block" OnClick="btnAgregarRolMod_Click" CausesValidation="false" />
                        <br />
                        <asp:Button ID="btnQuitarRolMod" runat="server" Text="&lt;&lt; Quitar"
                            CssClass="btn btn-warning btn-block" OnClick="btnQuitarRolMod_Click" CausesValidation="false" />
                    </div>

                    <div class="col-md-5">
                        <label class="control-label">Otorgados por el rol</label>
                        <asp:ListBox ID="lstAsignadosRolMod" runat="server" CssClass="form-control"
                            Rows="14" SelectionMode="Multiple"></asp:ListBox>
                    </div>

                </div>
            </div>
        </div>

        <div class="panel panel-default">
            <div class="panel-heading"><strong>Árbol resultante</strong></div>
            <div class="panel-body">
                <asp:Literal ID="litArbolRolMod" runat="server" Mode="PassThrough"></asp:Literal>
            </div>
        </div>

    </asp:Panel>

    </asp:Panel>
    </div>

</asp:Content>
