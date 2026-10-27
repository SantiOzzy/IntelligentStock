<%@ Page Title="Alta de suscripción" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="AltaSuscripcion.aspx.cs" Inherits="AltaSuscripcion" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Alta de suscripción</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">El token lo devuelve la pasarela de cobros externa. Los importes están expresados en pesos argentinos (ARS).</p>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblPlan" runat="server" AssociatedControlID="ddlPlan"
                    CssClass="control-label" Text="Tipo de plan (*)"></asp:Label>
                <asp:DropDownList ID="ddlPlan" runat="server" CssClass="form-control">
                </asp:DropDownList>
            </div>
            <div class="form-group">
                <asp:Label ID="lblToken" runat="server" AssociatedControlID="txtToken"
                    CssClass="control-label" Text="Token de pago (*)"></asp:Label>
                <asp:TextBox ID="txtToken" runat="server" CssClass="form-control" MaxLength="64"
                    autocomplete="off"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvToken" runat="server" ControlToValidate="txtToken"
                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El token de pago es obligatorio."></asp:RequiredFieldValidator>
                <span class="help-block">Empieza con tok_ seguido de 16 a 60 letras o números.</span>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblFechaInicio" runat="server" AssociatedControlID="txtFechaInicio"
                    CssClass="control-label" Text="Fecha de inicio"></asp:Label>
                <asp:TextBox ID="txtFechaInicio" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                <span class="help-block">Si se deja vacía, la suscripción empieza hoy.</span>
            </div>
        </div>
    </div>

    <asp:Button ID="btnAceptar" runat="server" Text="Activar suscripción" CssClass="btn btn-primary"
        ValidationGroup="Alta" OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
