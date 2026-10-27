<%@ Page Title="Alta de usuario" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="AltaUsuario.aspx.cs" Inherits="AltaUsuario" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Alta de usuario</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-6">

            <div class="form-group">
                <asp:Label ID="lblDni" runat="server" AssociatedControlID="txtDni"
                    CssClass="control-label" Text="DNI (*)"></asp:Label>
                <asp:TextBox ID="txtDni" runat="server" CssClass="form-control" MaxLength="8"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvDni" runat="server" ControlToValidate="txtDni"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El DNI es obligatorio."></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="revDni" runat="server" ControlToValidate="txtDni"
                    ValidationExpression="\d{7,8}" ValidationGroup="Alta" Display="Dynamic"
                    CssClass="text-danger" ErrorMessage="El DNI debe tener 7 u 8 dígitos, sin puntos."></asp:RegularExpressionValidator>
            </div>

            <div class="form-group">
                <asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre"
                    CssClass="control-label" Text="Nombre (*)"></asp:Label>
                <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control" MaxLength="50"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El nombre es obligatorio."></asp:RequiredFieldValidator>
            </div>

            <div class="form-group">
                <asp:Label ID="lblApellido" runat="server" AssociatedControlID="txtApellido"
                    CssClass="control-label" Text="Apellido (*)"></asp:Label>
                <asp:TextBox ID="txtApellido" runat="server" CssClass="form-control" MaxLength="50"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvApellido" runat="server" ControlToValidate="txtApellido"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El apellido es obligatorio."></asp:RequiredFieldValidator>
            </div>

            <div class="form-group">
                <asp:Label ID="lblFechaNacimiento" runat="server" AssociatedControlID="txtFechaNacimiento"
                    CssClass="control-label" Text="Fecha de nacimiento (*)"></asp:Label>
                <asp:TextBox ID="txtFechaNacimiento" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvFechaNacimiento" runat="server" ControlToValidate="txtFechaNacimiento"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La fecha de nacimiento es obligatoria."></asp:RequiredFieldValidator>
            </div>

        </div>
        <div class="col-md-6">

            <div class="form-group">
                <asp:Label ID="lblCorreo" runat="server" AssociatedControlID="txtCorreo"
                    CssClass="control-label" Text="Correo electrónico (*)"></asp:Label>
                <asp:TextBox ID="txtCorreo" runat="server" CssClass="form-control"
                    TextMode="Email" MaxLength="100"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvCorreo" runat="server" ControlToValidate="txtCorreo"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El correo electrónico es obligatorio."></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="revCorreo" runat="server" ControlToValidate="txtCorreo"
                    ValidationExpression="[^@\s]+@[^@\s]+\.[^@\s]+" ValidationGroup="Alta" Display="Dynamic"
                    CssClass="text-danger"
                    ErrorMessage="El correo debe tener texto antes de la @, y texto con un punto (el dominio) después de la @."></asp:RegularExpressionValidator>
            </div>

            <div class="form-group">
                <asp:Label ID="lblTelefono" runat="server" AssociatedControlID="txtTelefono"
                    CssClass="control-label" Text="Teléfono"></asp:Label>
                <asp:TextBox ID="txtTelefono" runat="server" CssClass="form-control" MaxLength="20"></asp:TextBox>
            </div>

            <div class="form-group">
                <asp:Label ID="lblRol" runat="server" AssociatedControlID="ddlRol"
                    CssClass="control-label" Text="Rol (*)"></asp:Label>
                <asp:DropDownList ID="ddlRol" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnSelectedIndexChanged="ddlRol_SelectedIndexChanged"></asp:DropDownList>
                <span class="help-block">El rol define las familias y patentes que va a tener el usuario.</span>
            </div>

            <div class="form-group">
                <asp:Label ID="lblUsuario" runat="server" AssociatedControlID="txtUsuario"
                    CssClass="control-label" Text="Nombre de usuario (*)"></asp:Label>
                <asp:TextBox ID="txtUsuario" runat="server" CssClass="form-control" MaxLength="50"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvUsuario" runat="server" ControlToValidate="txtUsuario"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El nombre de usuario es obligatorio."></asp:RequiredFieldValidator>
            </div>

            <div class="form-group">
                <asp:Label ID="lblClave" runat="server" AssociatedControlID="txtClave"
                    CssClass="control-label" Text="Contraseña (*)"></asp:Label>
                <asp:TextBox ID="txtClave" runat="server" CssClass="form-control"
                    TextMode="Password" MaxLength="50" EnableViewState="false"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvClave" runat="server" ControlToValidate="txtClave"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La contraseña es obligatoria."></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="revClave" runat="server" ControlToValidate="txtClave"
                    ValidationExpression="^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$" ValidationGroup="Alta"
                    Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La contraseña debe tener al menos 8 caracteres, con una mayúscula, una minúscula y un número."></asp:RegularExpressionValidator>
                <span class="help-block">Mínimo 8 caracteres, con mayúscula, minúscula y número.</span>
            </div>

            <div class="form-group">
                <asp:Label ID="lblConfirmacion" runat="server" AssociatedControlID="txtConfirmacion"
                    CssClass="control-label" Text="Repetir contraseña (*)"></asp:Label>
                <asp:TextBox ID="txtConfirmacion" runat="server" CssClass="form-control"
                    TextMode="Password" MaxLength="50" EnableViewState="false"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvConfirmacion" runat="server" ControlToValidate="txtConfirmacion"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Debe repetir la contraseña."></asp:RequiredFieldValidator>
                <asp:CompareValidator ID="cvClave" runat="server" ControlToValidate="txtConfirmacion"
                    ControlToCompare="txtClave" ValidationGroup="Alta" Display="Dynamic"
                    CssClass="text-danger" ErrorMessage="Las contraseñas no coinciden."></asp:CompareValidator>
            </div>

        </div>
    </div>

    <div class="panel panel-default">
        <div class="panel-heading"><strong>Permisos que otorga el rol seleccionado</strong></div>
        <div class="panel-body">
            <asp:Literal ID="litPermisos" runat="server" Mode="PassThrough"></asp:Literal>
        </div>
    </div>

    <asp:Button ID="btnAceptar" runat="server" Text="Aceptar" CssClass="btn btn-primary"
        ValidationGroup="Alta" OnClick="btnAceptar_Click" />
    <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar" CssClass="btn btn-default"
        OnClick="btnLimpiar_Click" CausesValidation="false" />

</asp:Content>
