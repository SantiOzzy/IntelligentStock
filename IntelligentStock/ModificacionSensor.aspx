<%@ Page Title="Modificación de sensor IoT" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ModificacionSensor.aspx.cs" Inherits="ModificacionSensor" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Modificación de sensor IoT</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblSensor" runat="server" AssociatedControlID="ddlSensor"
                    CssClass="control-label" Text="Sensor a modificar"></asp:Label>
                <asp:DropDownList ID="ddlSensor" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnSelectedIndexChanged="ddlSensor_SelectedIndexChanged">
                </asp:DropDownList>
            </div>
            <div class="form-group">
                <asp:Label ID="lblMac" runat="server" AssociatedControlID="txtMac"
                    CssClass="control-label" Text="Dirección MAC (*)"></asp:Label>
                <asp:TextBox ID="txtMac" runat="server" CssClass="form-control" MaxLength="50"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvMac" runat="server" ControlToValidate="txtMac"
                    ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La dirección MAC es obligatoria."></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="revMac" runat="server" ControlToValidate="txtMac"
                    ValidationExpression="\s*([0-9A-Fa-f]{2}[:\-\.]?){5}[0-9A-Fa-f]{2}\s*" ValidationGroup="Modificacion" Display="Dynamic"
                    CssClass="text-danger"
                    ErrorMessage="La dirección MAC tiene que tener 12 dígitos hexadecimales, por ejemplo 00:1A:2B:3C:4D:5E."></asp:RegularExpressionValidator>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblModelo" runat="server" AssociatedControlID="txtModelo"
                    CssClass="control-label" Text="Modelo"></asp:Label>
                <asp:TextBox ID="txtModelo" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
            </div>
            <div class="form-group">
                <asp:Label ID="lblTemperatura" runat="server" AssociatedControlID="txtTemperatura"
                    CssClass="control-label" Text="Temperatura máxima permitida (°C) (*)"></asp:Label>
                <asp:TextBox ID="txtTemperatura" runat="server" CssClass="form-control" TextMode="Number"
                    min="-60" max="60" step="0.1"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvTemperatura" runat="server" ControlToValidate="txtTemperatura"
                    ValidationGroup="Modificacion" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La temperatura máxima permitida es obligatoria."></asp:RequiredFieldValidator>
                <span class="help-block">Por encima de este valor el sensor genera una alerta. En un freezer es negativa (por ejemplo, -15).</span>
            </div>
        </div>
    </div>

    <asp:Button ID="btnAceptar" runat="server" Text="Guardar cambios" CssClass="btn btn-primary"
        ValidationGroup="Modificacion" OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
