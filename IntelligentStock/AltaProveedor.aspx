<%@ Page Title="Alta de proveedor" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="AltaProveedor.aspx.cs" Inherits="AltaProveedor" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Alta de proveedor</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblCuit" runat="server" AssociatedControlID="txtCuit"
                    CssClass="control-label" Text="CUIT (*)"></asp:Label>
                <asp:TextBox ID="txtCuit" runat="server" CssClass="form-control" MaxLength="13"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvCuit" runat="server" ControlToValidate="txtCuit"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El CUIT es obligatorio."></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="revCuit" runat="server" ControlToValidate="txtCuit"
                    ValidationExpression="\d{2}-?\d{8}-?\d" ValidationGroup="Alta" Display="Dynamic"
                    CssClass="text-danger" ErrorMessage="El CUIT debe tener 11 dígitos, con o sin guiones."></asp:RegularExpressionValidator>
                <span class="help-block">Con o sin guiones, por ejemplo 20-12345678-6.</span>
            </div>
            <div class="form-group">
                <asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre"
                    CssClass="control-label" Text="Nombre (*)"></asp:Label>
                <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El nombre es obligatorio."></asp:RequiredFieldValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblRazonSocial" runat="server" AssociatedControlID="txtRazonSocial"
                    CssClass="control-label" Text="Razón social (*)"></asp:Label>
                <asp:TextBox ID="txtRazonSocial" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvRazonSocial" runat="server" ControlToValidate="txtRazonSocial"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La razón social es obligatoria."></asp:RequiredFieldValidator>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblCorreo" runat="server" AssociatedControlID="txtCorreo"
                    CssClass="control-label" Text="Correo electrónico (*)"></asp:Label>
                <asp:TextBox ID="txtCorreo" runat="server" CssClass="form-control" TextMode="Email" MaxLength="100"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvCorreo" runat="server" ControlToValidate="txtCorreo"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El correo electrónico es obligatorio."></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="revCorreo" runat="server" ControlToValidate="txtCorreo"
                    ValidationExpression="[^@\s]+@[^@\s]+\.[^@\s]+" ValidationGroup="Alta" Display="Dynamic"
                    CssClass="text-danger"
                    ErrorMessage="El correo debe tener texto antes de la @, y texto con un punto (el dominio) después de la @."></asp:RegularExpressionValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblNumero" runat="server" AssociatedControlID="txtNumero"
                    CssClass="control-label" Text="Teléfono"></asp:Label>
                <asp:TextBox ID="txtNumero" runat="server" CssClass="form-control" MaxLength="20"></asp:TextBox>
                <asp:RegularExpressionValidator ID="revNumero" runat="server" ControlToValidate="txtNumero"
                    ValidationExpression="[0-9 ()+\-]+" ValidationGroup="Alta" Display="Dynamic"
                    CssClass="text-danger"
                    ErrorMessage="El teléfono solo puede contener números, espacios, guiones, paréntesis y el signo +."></asp:RegularExpressionValidator>
                <asp:RegularExpressionValidator ID="revDigitosNumero" runat="server" ControlToValidate="txtNumero"
                    ValidationExpression="(\D*\d){6,}\D*" ValidationGroup="Alta" Display="Dynamic"
                    CssClass="text-danger"
                    ErrorMessage="El teléfono debe tener al menos 6 números."></asp:RegularExpressionValidator>
            </div>
        </div>
    </div>

    <asp:Button ID="btnAceptar" runat="server" Text="Aceptar" CssClass="btn btn-primary"
        ValidationGroup="Alta" OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
