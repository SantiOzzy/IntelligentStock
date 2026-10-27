<%@ Page Title="Consulta de órdenes de compra" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ConsultarOrdenCompra.aspx.cs" Inherits="ConsultarOrdenCompra" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Consulta de órdenes de compra</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblProveedor" runat="server" AssociatedControlID="ddlProveedor"
                    CssClass="control-label" Text="Proveedor"></asp:Label>
                <asp:DropDownList ID="ddlProveedor" runat="server" CssClass="form-control">
                    <asp:ListItem Text="(todos)" Value="" />
                </asp:DropDownList>
            </div>
            <div class="form-group">
                <asp:Label ID="lblEstado" runat="server" AssociatedControlID="ddlEstado"
                    CssClass="control-label" Text="Estado"></asp:Label>
                <asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-control">
                    <asp:ListItem Text="(todos)" Value="" />
                    <asp:ListItem Text="Pendiente" Value="Pendiente" />
                    <asp:ListItem Text="Recibida" Value="Recibida" />
                    <asp:ListItem Text="Anulada" Value="Anulada" />
                </asp:DropDownList>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblDesde" runat="server" AssociatedControlID="txtDesde"
                    CssClass="control-label" Text="Fecha desde"></asp:Label>
                <asp:TextBox ID="txtDesde" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
            </div>
            <div class="form-group">
                <asp:Label ID="lblHasta" runat="server" AssociatedControlID="txtHasta"
                    CssClass="control-label" Text="Fecha hasta"></asp:Label>
                <asp:TextBox ID="txtHasta" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
            </div>
        </div>
    </div>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true" DataKeyNames="IdOrdenCompra" OnRowCommand="gvDetalle_RowCommand"
        EmptyDataText="No hay registros para mostrar.">
        <Columns>
            <asp:BoundField DataField="Numero" HeaderText="Número" />
            <asp:BoundField DataField="ProveedorTexto" HeaderText="Proveedor" />
            <asp:BoundField DataField="FechaTexto" HeaderText="Fecha" />
            <asp:BoundField DataField="EstadoBase" HeaderText="Estado" />
            <asp:BoundField DataField="DetalleEstado" HeaderText="Comprobante / motivo" />
            <asp:BoundField DataField="CantidadItems" HeaderText="Ítems" />
            <asp:ButtonField CommandName="Ver" Text="Ver detalle" ButtonType="Link" />
        </Columns>
    </asp:GridView>

    <asp:Panel ID="pnlItems" runat="server" Visible="false">
        <p><strong><asp:Label ID="lblOrden" runat="server"></asp:Label></strong></p>
        <asp:GridView ID="gvItems" runat="server" AutoGenerateColumns="false"
            CssClass="table table-bordered table-condensed table-striped" GridLines="None"
            ShowHeaderWhenEmpty="true"
            EmptyDataText="La orden no tiene insumos.">
            <Columns>
                <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
                <asp:BoundField DataField="CantidadTexto" HeaderText="Cantidad" />
                <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio unitario" DataFormatString="{0:C}" />
                <asp:BoundField DataField="ImporteTexto" HeaderText="Importe estimado" />
            </Columns>
        </asp:GridView>
    </asp:Panel>

    <asp:Button ID="btnAceptar" runat="server" Text="Buscar" CssClass="btn btn-primary"
        OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
