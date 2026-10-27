<%@ Page Title="Cambiar contraseña" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="CambiarContraseña.aspx.cs" Inherits="CambiarContraseña" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Cambiar contraseña</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-5">

            <div class="form-group">
                <asp:Label ID="lblActual" runat="server" AssociatedControlID="txtActual"
                    CssClass="control-label" Text="Contraseña actual (*)"></asp:Label>
                <asp:TextBox ID="txtActual" runat="server" CssClass="form-control"
                    TextMode="Password" MaxLength="50"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvActual" runat="server" ControlToValidate="txtActual"
                    ValidationGroup="Clave" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Ingrese la contraseña actual."></asp:RequiredFieldValidator>
            </div>

            <div class="form-group">
                <asp:Label ID="lblNueva" runat="server" AssociatedControlID="txtNueva"
                    CssClass="control-label" Text="Contraseña nueva (*)"></asp:Label>
                <asp:TextBox ID="txtNueva" runat="server" CssClass="form-control"
                    TextMode="Password" MaxLength="50"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvNueva" runat="server" ControlToValidate="txtNueva"
                    ValidationGroup="Clave" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Ingrese la contraseña nueva."></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="revNueva" runat="server" ControlToValidate="txtNueva"
                    ValidationExpression="^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$" ValidationGroup="Clave"
                    Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La contraseña debe tener al menos 8 caracteres, con una mayúscula, una minúscula y un número."></asp:RegularExpressionValidator>
                <span class="help-block">Mínimo 8 caracteres, con mayúscula, minúscula y número.</span>
            </div>

            <div class="form-group">
                <asp:Label ID="lblConfirmacion" runat="server" AssociatedControlID="txtConfirmacion"
                    CssClass="control-label" Text="Repetir contraseña nueva (*)"></asp:Label>
                <asp:TextBox ID="txtConfirmacion" runat="server" CssClass="form-control"
                    TextMode="Password" MaxLength="50"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvConfirmacion" runat="server" ControlToValidate="txtConfirmacion"
                    ValidationGroup="Clave" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Repita la contraseña nueva."></asp:RequiredFieldValidator>
                <asp:CompareValidator ID="cvNueva" runat="server" ControlToValidate="txtConfirmacion"
                    ControlToCompare="txtNueva" ValidationGroup="Clave" Display="Dynamic"
                    CssClass="text-danger" ErrorMessage="Las contraseñas no coinciden."></asp:CompareValidator>
            </div>

            <asp:Button ID="btnAceptar" runat="server" Text="Aceptar" CssClass="btn btn-primary"
                ValidationGroup="Clave" OnClick="btnAceptar_Click" />
            <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
                OnClick="btnCancelar_Click" CausesValidation="false" />

        </div>
    </div>

</asp:Content>
