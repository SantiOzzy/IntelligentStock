<%@ Page Title="Modificación de usuario" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ModificacionUsuario.aspx.cs" Inherits="ModificacionUsuario" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Modificación de usuario</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="panel panel-default">
        <div class="panel-heading"><strong>Buscar usuario</strong></div>
        <div class="panel-body">
            <div class="row">
                <div class="col-md-6 form-group">
                    <asp:Label ID="lblBuscar" runat="server" AssociatedControlID="ddlUsuarios"
                        CssClass="control-label" Text="Usuario a modificar"></asp:Label>
                    <asp:DropDownList ID="ddlUsuarios" runat="server" CssClass="form-control"></asp:DropDownList>
                </div>
                <div class="col-md-3 form-group">
                    <label class="control-label">&nbsp;</label><br />
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar" CssClass="btn btn-primary"
                        OnClick="btnBuscar_Click" CausesValidation="false" />
                </div>
            </div>
        </div>
    </div>

    <asp:Panel ID="pnlDatos" runat="server" Visible="false">

        <div class="row">
            <div class="col-md-6">

                <div class="form-group">
                    <asp:Label ID="lblDni" runat="server" AssociatedControlID="txtDni"
                        CssClass="control-label" Text="DNI"></asp:Label>
                    <asp:TextBox ID="txtDni" runat="server" CssClass="form-control" ReadOnly="true"></asp:TextBox>
                    <span class="help-block">El DNI es la clave del registro y no se puede modificar.</span>
                </div>

                <div class="form-group">
                    <asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre"
                        CssClass="control-label" Text="Nombre (*)"></asp:Label>
                    <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control" MaxLength="50"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                        ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="El nombre es obligatorio."></asp:RequiredFieldValidator>
                </div>

                <div class="form-group">
                    <asp:Label ID="lblApellido" runat="server" AssociatedControlID="txtApellido"
                        CssClass="control-label" Text="Apellido (*)"></asp:Label>
                    <asp:TextBox ID="txtApellido" runat="server" CssClass="form-control" MaxLength="50"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="rfvApellido" runat="server" ControlToValidate="txtApellido"
                        ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="El apellido es obligatorio."></asp:RequiredFieldValidator>
                </div>

                <div class="form-group">
                    <asp:Label ID="lblFechaNacimiento" runat="server" AssociatedControlID="txtFechaNacimiento"
                        CssClass="control-label" Text="Fecha de nacimiento (*)"></asp:Label>
                    <asp:TextBox ID="txtFechaNacimiento" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="rfvFechaNacimiento" runat="server" ControlToValidate="txtFechaNacimiento"
                        ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
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
                        ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="El correo electrónico es obligatorio."></asp:RequiredFieldValidator>
                    <asp:RegularExpressionValidator ID="revCorreo" runat="server" ControlToValidate="txtCorreo"
                        ValidationExpression="[^@\s]+@[^@\s]+\.[^@\s]+" ValidationGroup="Modificacion" Display="Dynamic"
                        CssClass="text-danger"
                        ErrorMessage="El correo debe tener texto antes de la @, y texto con un punto (el dominio) después de la @."></asp:RegularExpressionValidator>
                </div>

                <div class="form-group">
                    <asp:Label ID="lblTelefono" runat="server" AssociatedControlID="txtTelefono"
                        CssClass="control-label" Text="Teléfono"></asp:Label>
                    <asp:TextBox ID="txtTelefono" runat="server" CssClass="form-control" MaxLength="20"></asp:TextBox>
                </div>

                <div class="form-group">
                    <asp:Label ID="lblUsuario" runat="server" AssociatedControlID="txtUsuario"
                        CssClass="control-label" Text="Nombre de usuario (*)"></asp:Label>
                    <asp:TextBox ID="txtUsuario" runat="server" CssClass="form-control" MaxLength="50"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="rfvUsuario" runat="server" ControlToValidate="txtUsuario"
                        ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="El nombre de usuario es obligatorio."></asp:RequiredFieldValidator>
                </div>

                <div class="form-group">
                    <asp:Label ID="lblRol" runat="server" AssociatedControlID="ddlRol"
                        CssClass="control-label" Text="Rol (*)"></asp:Label>
                    <asp:DropDownList ID="ddlRol" runat="server" CssClass="form-control"
                        AutoPostBack="true" OnSelectedIndexChanged="ddlRol_SelectedIndexChanged"></asp:DropDownList>
                    <span class="help-block">Cambiar el rol cambia todos los permisos del usuario.</span>
                    <asp:Label ID="lblAvisoRol" runat="server" Visible="false" CssClass="text-danger"
                        Text="No puede cambiar su propio rol. Pídaselo a otro usuario con perfil de administración."></asp:Label>
                </div>

            </div>
        </div>

        <div class="panel panel-default">
            <div class="panel-heading"><strong>Permisos que otorga el rol seleccionado</strong></div>
            <div class="panel-body">
                <asp:Literal ID="litPermisos" runat="server" Mode="PassThrough"></asp:Literal>
            </div>
        </div>

        <asp:Button ID="btnGuardar" runat="server" Text="Guardar cambios" CssClass="btn btn-primary"
            ValidationGroup="Modificacion" OnClick="btnGuardar_Click" />
        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
            OnClick="btnCancelar_Click" CausesValidation="false" />

        <div class="panel panel-default">
            <div class="panel-heading"><strong>Restablecer contraseña</strong></div>
            <div class="panel-body">
                <p class="help-block">
                    Para cuando el usuario olvidó su contraseña y no puede recuperarla desde el
                    inicio de sesión: se le asigna una contraseña nueva acá y se le avisa por correo
                    electrónico. Conviene que la cambie desde Cambiar contraseña al ingresar.
                </p>
                <div class="row">
                    <div class="col-md-4 form-group">
                        <asp:Label ID="lblNuevaClave" runat="server" AssociatedControlID="txtNuevaClave"
                            CssClass="control-label" Text="Contraseña nueva (*)"></asp:Label>
                        <asp:TextBox ID="txtNuevaClave" runat="server" CssClass="form-control"
                            TextMode="Password" MaxLength="50"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvNuevaClave" runat="server" ControlToValidate="txtNuevaClave"
                            ValidationGroup="Restablecer" Display="Dynamic" CssClass="text-danger"
                            ErrorMessage="La contraseña nueva es obligatoria."></asp:RequiredFieldValidator>
                        <asp:RegularExpressionValidator ID="revNuevaClave" runat="server" ControlToValidate="txtNuevaClave"
                            ValidationExpression="^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$" ValidationGroup="Restablecer"
                            Display="Dynamic" CssClass="text-danger"
                            ErrorMessage="Mínimo 8 caracteres, con mayúscula, minúscula y número."></asp:RegularExpressionValidator>
                    </div>
                    <div class="col-md-4 form-group">
                        <asp:Label ID="lblConfirmarClave" runat="server" AssociatedControlID="txtConfirmarClave"
                            CssClass="control-label" Text="Repetir contraseña (*)"></asp:Label>
                        <asp:TextBox ID="txtConfirmarClave" runat="server" CssClass="form-control"
                            TextMode="Password" MaxLength="50"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvConfirmarClave" runat="server" ControlToValidate="txtConfirmarClave"
                            ValidationGroup="Restablecer" Display="Dynamic" CssClass="text-danger"
                            ErrorMessage="Debe repetir la contraseña."></asp:RequiredFieldValidator>
                        <asp:CompareValidator ID="cvNuevaClave" runat="server" ControlToValidate="txtConfirmarClave"
                            ControlToCompare="txtNuevaClave" ValidationGroup="Restablecer" Display="Dynamic"
                            CssClass="text-danger" ErrorMessage="Las contraseñas no coinciden."></asp:CompareValidator>
                    </div>
                    <div class="col-md-4 form-group">
                        <label class="control-label">&nbsp;</label><br />
                        <asp:Button ID="btnRestablecerClave" runat="server" Text="Restablecer contraseña"
                            CssClass="btn btn-warning" ValidationGroup="Restablecer" OnClick="btnRestablecerClave_Click"
                            OnClientClick="if (!Page_ClientValidate('Restablecer')) return false; return confirm('¿Confirma que desea restablecer la contraseña de este usuario?');" />
                    </div>
                </div>
            </div>
        </div>

    </asp:Panel>

</asp:Content>
