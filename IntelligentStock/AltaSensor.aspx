<%@ Page Title="Alta de sensor IoT" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="AltaSensor.aspx.cs" Inherits="AltaSensor" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Alta de sensor IoT</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">La dirección MAC no se puede repetir entre dispositivos activos.</p>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblMac" runat="server" AssociatedControlID="txtMac"
                    CssClass="control-label" Text="Dirección MAC (*)"></asp:Label>
                <asp:TextBox ID="txtMac" runat="server" CssClass="form-control" MaxLength="50"
                    placeholder="00:1A:2B:3C:4D:5E"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvMac" runat="server" ControlToValidate="txtMac"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La dirección MAC es obligatoria."></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="revMac" runat="server" ControlToValidate="txtMac"
                    ValidationExpression="\s*([0-9A-Fa-f]{2}[:\-\.]?){5}[0-9A-Fa-f]{2}\s*" ValidationGroup="Alta" Display="Dynamic"
                    CssClass="text-danger"
                    ErrorMessage="La dirección MAC tiene que tener 12 dígitos hexadecimales, por ejemplo 00:1A:2B:3C:4D:5E."></asp:RegularExpressionValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblModelo" runat="server" AssociatedControlID="txtModelo"
                    CssClass="control-label" Text="Modelo"></asp:Label>
                <asp:TextBox ID="txtModelo" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblTemperatura" runat="server" AssociatedControlID="txtTemperatura"
                    CssClass="control-label" Text="Temperatura máxima permitida (°C) (*)"></asp:Label>
                <asp:TextBox ID="txtTemperatura" runat="server" CssClass="form-control" TextMode="Number"
                    min="-60" max="60" step="0.1"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvTemperatura" runat="server" ControlToValidate="txtTemperatura"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La temperatura máxima permitida es obligatoria."></asp:RequiredFieldValidator>
                <span class="help-block">Por encima de este valor el sensor genera una alerta. En un freezer es negativa (por ejemplo, -15).</span>
            </div>
        </div>
    </div>

    <asp:Button ID="btnAceptar" runat="server" Text="Aceptar" CssClass="btn btn-primary"
        ValidationGroup="Alta" OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
