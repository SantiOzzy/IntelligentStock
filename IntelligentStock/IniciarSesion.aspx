<%@ Page Title="Inicio de sesión" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="IniciarSesion.aspx.cs" Inherits="IniciarSesion" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <%-- ------------------ Modo 1: todavía no hay sesión ------------------ --%>
    <asp:Panel ID="pnlLogin" runat="server">
        <div class="row">
            <div class="col-md-4 col-md-offset-4">
                <div class="panel panel-default">
                    <div class="panel-heading">
                        <h3 class="panel-title">Inicio de sesión</h3>
                    </div>
                    <div class="panel-body">

                        <div class="form-group">
                            <asp:Label ID="lblUsuario" runat="server" AssociatedControlID="txtUsuario"
                                CssClass="control-label" Text="Nombre de usuario"></asp:Label>
                            <asp:TextBox ID="txtUsuario" runat="server" CssClass="form-control"
                                MaxLength="50"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="rfvUsuario" runat="server"
                                ControlToValidate="txtUsuario" ValidationGroup="Login" Display="Dynamic"
                                CssClass="text-danger" ErrorMessage="Debe ingresar el nombre de usuario."></asp:RequiredFieldValidator>
                        </div>

                        <div class="form-group">
                            <asp:Label ID="lblClave" runat="server" AssociatedControlID="txtClave"
                                CssClass="control-label" Text="Contraseña"></asp:Label>
                            <asp:TextBox ID="txtClave" runat="server" CssClass="form-control"
                                TextMode="Password" MaxLength="50"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="rfvClave" runat="server"
                                ControlToValidate="txtClave" ValidationGroup="Login" Display="Dynamic"
                                CssClass="text-danger" ErrorMessage="Debe ingresar la contraseña."></asp:RequiredFieldValidator>
                        </div>

                        <asp:Button ID="btnIngresar" runat="server" Text="Ingresar"
                            CssClass="btn btn-primary btn-block" ValidationGroup="Login"
                            OnClick="btnIngresar_Click" />

                        <p class="text-center" style="margin-top: 12px; margin-bottom: 0;">
                            <asp:LinkButton ID="lnkOlvideClave" runat="server" CausesValidation="false"
                                OnClick="lnkOlvideClave_Click" Text="¿Olvidaste tu contraseña?"></asp:LinkButton>
                        </p>
                    </div>
                </div>
            </div>
        </div>
    </asp:Panel>

    <%-- --------- Modo 3: pedir el enlace de recuperación por correo --------- --%>
    <asp:Panel ID="pnlRecuperar" runat="server" Visible="false" DefaultButton="btnEnviarEnlace">
        <div class="row">
            <div class="col-md-4 col-md-offset-4">
                <div class="panel panel-default">
                    <div class="panel-heading">
                        <h3 class="panel-title">Recuperar contraseña</h3>
                    </div>
                    <div class="panel-body">

                        <p class="help-block">
                            Ingrese el correo electrónico de su cuenta. Le vamos a enviar un enlace
                            para definir una contraseña nueva, sin necesidad de que se la restablezca
                            un administrador.
                        </p>

                        <div class="form-group">
                            <asp:Label ID="lblCorreoRecuperar" runat="server" AssociatedControlID="txtCorreoRecuperar"
                                CssClass="control-label" Text="Correo electrónico"></asp:Label>
                            <asp:TextBox ID="txtCorreoRecuperar" runat="server" CssClass="form-control"
                                TextMode="Email" MaxLength="80"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="rfvCorreoRecuperar" runat="server"
                                ControlToValidate="txtCorreoRecuperar" ValidationGroup="Recuperar" Display="Dynamic"
                                CssClass="text-danger" ErrorMessage="Debe ingresar el correo electrónico."></asp:RequiredFieldValidator>
                        </div>

                        <asp:Button ID="btnEnviarEnlace" runat="server" Text="Enviar enlace"
                            CssClass="btn btn-primary btn-block" ValidationGroup="Recuperar"
                            OnClick="btnEnviarEnlace_Click" />

                        <p class="text-center" style="margin-top: 12px; margin-bottom: 0;">
                            <asp:LinkButton ID="lnkVolverAlLogin" runat="server" CausesValidation="false"
                                OnClick="lnkVolverAlLogin_Click" Text="Volver al inicio de sesión"></asp:LinkButton>
                        </p>
                    </div>
                </div>
            </div>
        </div>
    </asp:Panel>

    <%-- ------ Modo 4: definir la contraseña nueva con el enlace recibido ------ --%>
    <asp:Panel ID="pnlNuevaClave" runat="server" Visible="false" DefaultButton="btnGuardarClaveNueva">
        <asp:HiddenField ID="hdnToken" runat="server" />
        <div class="row">
            <div class="col-md-4 col-md-offset-4">
                <div class="panel panel-default">
                    <div class="panel-heading">
                        <h3 class="panel-title">Nueva contraseña</h3>
                    </div>
                    <div class="panel-body">

                        <p class="help-block">
                            <asp:Literal ID="litCuentaRecuperada" runat="server"></asp:Literal>
                            Debe tener al menos 8 caracteres, con una mayúscula, una minúscula y un número.
                        </p>

                        <div class="form-group">
                            <asp:Label ID="lblClaveNueva" runat="server" AssociatedControlID="txtClaveNueva"
                                CssClass="control-label" Text="Nueva contraseña"></asp:Label>
                            <asp:TextBox ID="txtClaveNueva" runat="server" CssClass="form-control"
                                TextMode="Password" MaxLength="50"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="rfvClaveNueva" runat="server"
                                ControlToValidate="txtClaveNueva" ValidationGroup="NuevaClave" Display="Dynamic"
                                CssClass="text-danger" ErrorMessage="Debe ingresar la nueva contraseña."></asp:RequiredFieldValidator>
                            <asp:RegularExpressionValidator ID="revClaveNueva" runat="server"
                                ControlToValidate="txtClaveNueva" ValidationGroup="NuevaClave" Display="Dynamic"
                                ValidationExpression="^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$" CssClass="text-danger"
                                ErrorMessage="La contraseña debe tener al menos 8 caracteres, con una mayúscula, una minúscula y un número."></asp:RegularExpressionValidator>
                        </div>

                        <div class="form-group">
                            <asp:Label ID="lblRepetirClaveNueva" runat="server" AssociatedControlID="txtRepetirClaveNueva"
                                CssClass="control-label" Text="Repetir contraseña"></asp:Label>
                            <asp:TextBox ID="txtRepetirClaveNueva" runat="server" CssClass="form-control"
                                TextMode="Password" MaxLength="50"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="rfvRepetirClaveNueva" runat="server"
                                ControlToValidate="txtRepetirClaveNueva" ValidationGroup="NuevaClave" Display="Dynamic"
                                CssClass="text-danger" ErrorMessage="Debe repetir la nueva contraseña."></asp:RequiredFieldValidator>
                            <asp:CompareValidator ID="cmpClaveNueva" runat="server"
                                ControlToValidate="txtRepetirClaveNueva" ControlToCompare="txtClaveNueva"
                                ValidationGroup="NuevaClave" Display="Dynamic" CssClass="text-danger"
                                ErrorMessage="Las contraseñas no coinciden."></asp:CompareValidator>
                        </div>

                        <asp:Button ID="btnGuardarClaveNueva" runat="server" Text="Guardar contraseña"
                            CssClass="btn btn-primary btn-block" ValidationGroup="NuevaClave"
                            OnClick="btnGuardarClaveNueva_Click" />
                    </div>
                </div>
            </div>
        </div>
    </asp:Panel>

    <%-- --------------- Modo 2: panel principal del usuario --------------- --%>
    <asp:Panel ID="pnlPanel" runat="server" Visible="false">
        <div class="page-header">
            <h2><asp:Literal ID="litBienvenida" runat="server"></asp:Literal></h2>
        </div>
        <asp:Literal ID="litOpciones" runat="server" Mode="PassThrough"></asp:Literal>
    </asp:Panel>

</asp:Content>
