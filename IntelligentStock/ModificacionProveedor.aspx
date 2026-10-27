<%@ Page Title="Modificación de proveedor" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ModificacionProveedor.aspx.cs" Inherits="ModificacionProveedor" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Modificación de proveedor</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblProveedor" runat="server" AssociatedControlID="ddlProveedor"
                    CssClass="control-label" Text="Proveedor a modificar"></asp:Label>
                <asp:DropDownList ID="ddlProveedor" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnSelectedIndexChanged="ddlProveedor_SelectedIndexChanged">
                </asp:DropDownList>
                <span class="help-block">El CUIT es la clave del proveedor y no se puede modificar.</span>
            </div>
            <div class="form-group">
                <asp:Label ID="lblNombre" runat="server" AssociatedControlID="txtNombre"
                    CssClass="control-label" Text="Nombre (*)"></asp:Label>
                <asp:TextBox ID="txtNombre" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                    ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El nombre es obligatorio."></asp:RequiredFieldValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblRazonSocial" runat="server" AssociatedControlID="txtRazonSocial"
                    CssClass="control-label" Text="Razón social (*)"></asp:Label>
                <asp:TextBox ID="txtRazonSocial" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvRazonSocial" runat="server" ControlToValidate="txtRazonSocial"
                    ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La razón social es obligatoria."></asp:RequiredFieldValidator>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblCorreo" runat="server" AssociatedControlID="txtCorreo"
                    CssClass="control-label" Text="Correo electrónico (*)"></asp:Label>
                <asp:TextBox ID="txtCorreo" runat="server" CssClass="form-control" TextMode="Email" MaxLength="100"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvCorreo" runat="server" ControlToValidate="txtCorreo"
                    ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El correo electrónico es obligatorio."></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="revCorreo" runat="server" ControlToValidate="txtCorreo"
                    ValidationExpression="[^@\s]+@[^@\s]+\.[^@\s]+" ValidationGroup="Modificacion" Display="Dynamic"
                    CssClass="text-danger"
                    ErrorMessage="El correo debe tener texto antes de la @, y texto con un punto (el dominio) después de la @."></asp:RegularExpressionValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblNumero" runat="server" AssociatedControlID="txtNumero"
                    CssClass="control-label" Text="Teléfono"></asp:Label>
                <asp:TextBox ID="txtNumero" runat="server" CssClass="form-control" MaxLength="20"></asp:TextBox>
                <asp:RegularExpressionValidator ID="revNumero" runat="server" ControlToValidate="txtNumero"
                    ValidationExpression="[0-9 ()+\-]+" ValidationGroup="Modificacion" Display="Dynamic"
                    CssClass="text-danger"
                    ErrorMessage="El teléfono solo puede contener números, espacios, guiones, paréntesis y el signo +."></asp:RegularExpressionValidator>
                <asp:RegularExpressionValidator ID="revDigitosNumero" runat="server" ControlToValidate="txtNumero"
                    ValidationExpression="(\D*\d){6,}\D*" ValidationGroup="Modificacion" Display="Dynamic"
                    CssClass="text-danger"
                    ErrorMessage="El teléfono debe tener al menos 6 números."></asp:RegularExpressionValidator>
            </div>
        </div>
    </div>

    <asp:Button ID="btnAceptar" runat="server" Text="Guardar cambios" CssClass="btn btn-primary"
        ValidationGroup="Modificacion" OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
