<%@ Page Title="Registrar factura de proveedor" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="RegistrarFacturaProveedor.aspx.cs" Inherits="RegistrarFacturaProveedor" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Registrar factura de proveedor</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">El número de comprobante no se puede repetir para el mismo proveedor. Al registrar la factura, lo pedido en la orden ingresa al stock.</p>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblOrden" runat="server" AssociatedControlID="ddlOrden"
                    CssClass="control-label" Text="Orden de compra (*)"></asp:Label>
                <asp:DropDownList ID="ddlOrden" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnSelectedIndexChanged="ddlOrden_SelectedIndexChanged">
                    <asp:ListItem Text="(seleccione)" Value="" />
                </asp:DropDownList>
            </div>
            <div class="form-group">
                <asp:Label ID="lblComprobante" runat="server" AssociatedControlID="txtComprobante"
                    CssClass="control-label" Text="Número de comprobante (*)"></asp:Label>
                <asp:TextBox ID="txtComprobante" runat="server" CssClass="form-control" MaxLength="50"></asp:TextBox>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblFecha" runat="server" AssociatedControlID="txtFecha"
                    CssClass="control-label" Text="Fecha del comprobante (*)"></asp:Label>
                <asp:TextBox ID="txtFecha" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
            </div>
            <div class="form-group">
                <asp:Label ID="lblImporte" runat="server" AssociatedControlID="txtImporte"
                    CssClass="control-label" Text="Importe total"></asp:Label>
                <asp:TextBox ID="txtImporte" runat="server" CssClass="form-control" TextMode="Number" min="0" step="0.01"></asp:TextBox>
            </div>
        </div>
    </div>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="Elija una orden para ver sus insumos.">
        <Columns>
            <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
            <asp:BoundField DataField="CantidadTexto" HeaderText="Cantidad pedida" />
            <asp:BoundField DataField="CantidadTexto" HeaderText="Cantidad recibida" />
            <asp:BoundField DataField="ImporteTexto" HeaderText="Importe estimado" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Registrar factura" CssClass="btn btn-primary"
        OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
