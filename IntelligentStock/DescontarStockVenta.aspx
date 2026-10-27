<%@ Page Title="Descontar stock por venta" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="DescontarStockVenta.aspx.cs" Inherits="DescontarStockVenta" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Descontar stock por venta</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">Se descuenta cada insumo de la receta según la cantidad vendida. La grilla muestra cómo queda el stock antes de confirmar.</p>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblReceta" runat="server" AssociatedControlID="ddlReceta"
                    CssClass="control-label" Text="Receta vendida (*)"></asp:Label>
                <asp:DropDownList ID="ddlReceta" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnSelectedIndexChanged="ddlReceta_SelectedIndexChanged">
                    <asp:ListItem Text="(seleccione)" Value="" />
                </asp:DropDownList>
            </div>
            <div class="form-group">
                <asp:Label ID="lblCantidad" runat="server" AssociatedControlID="txtCantidad"
                    CssClass="control-label" Text="Cantidad vendida (*)"></asp:Label>
                <asp:TextBox ID="txtCantidad" runat="server" CssClass="form-control" TextMode="Number" min="0.01" step="0.01"
                    AutoPostBack="true" OnTextChanged="txtCantidad_TextChanged"></asp:TextBox>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblFecha" runat="server" AssociatedControlID="txtFecha"
                    CssClass="control-label" Text="Fecha de la venta"></asp:Label>
                <asp:TextBox ID="txtFecha" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
            </div>
        </div>
    </div>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="Elija la receta y la cantidad vendida.">
        <Columns>
            <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
            <asp:BoundField DataField="ConsumoUnitarioTexto" HeaderText="Consumo unitario" />
            <asp:BoundField DataField="ConsumoTotalTexto" HeaderText="Consumo total" />
            <asp:BoundField DataField="StockAnteriorTexto" HeaderText="Stock actual" />
            <asp:BoundField DataField="StockResultanteTexto" HeaderText="Stock resultante" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Descontar stock" CssClass="btn btn-primary"
        OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
