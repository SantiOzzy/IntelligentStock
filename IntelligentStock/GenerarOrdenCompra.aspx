<%@ Page Title="Generar orden de compra" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="GenerarOrdenCompra.aspx.cs" Inherits="GenerarOrdenCompra" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Generar orden de compra</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">La sugerencia de cantidades sale del módulo de predicción de demanda.</p>
    <asp:Label ID="lblPrediccion" runat="server" CssClass="help-block" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblProveedor" runat="server" AssociatedControlID="ddlProveedor"
                    CssClass="control-label" Text="Proveedor (*)"></asp:Label>
                <asp:DropDownList ID="ddlProveedor" runat="server" CssClass="form-control">
                    <asp:ListItem Text="(seleccione)" Value="" />
                </asp:DropDownList>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblFecha" runat="server" AssociatedControlID="txtFecha"
                    CssClass="control-label" Text="Fecha de la orden"></asp:Label>
                <asp:TextBox ID="txtFecha" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
            </div>
        </div>
    </div>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="No hay cantidades sugeridas para mostrar.">
        <Columns>
            <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
            <asp:BoundField DataField="StockActualTexto" HeaderText="Stock actual" />
            <asp:BoundField DataField="StockMinimoTexto" HeaderText="Stock mínimo" />
            <asp:BoundField DataField="ConsumoPrevistoTexto" HeaderText="Consumo previsto" />
            <asp:BoundField DataField="CantidadPendienteTexto" HeaderText="Ya pedido" />
            <asp:BoundField DataField="CantidadSugeridaTexto" HeaderText="Cantidad sugerida" />
            <asp:BoundField DataField="ImporteEstimadoTexto" HeaderText="Importe estimado" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Generar orden" CssClass="btn btn-primary"
        OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
