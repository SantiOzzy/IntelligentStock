<%@ Page Title="Consulta de órdenes de compra" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ConsultarOrdenCompra.aspx.cs" Inherits="ConsultarOrdenCompra" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Consulta de órdenes de compra</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">Busque las órdenes por proveedor, estado o fechas. Los filtros vacíos no se tienen en cuenta. Con "Ver detalle" se muestran los insumos de la orden.</p>

    <div class="filtros">
        <div class="row">
            <div class="col-sm-6 col-md-3">
                <div class="form-group">
                    <asp:Label ID="lblProveedor" runat="server" AssociatedControlID="ddlProveedor"
                        CssClass="control-label" Text="Proveedor"></asp:Label>
                    <asp:DropDownList ID="ddlProveedor" runat="server" CssClass="form-control">
                        <asp:ListItem Text="(todos)" Value="" />
                    </asp:DropDownList>
                </div>
            </div>
            <div class="col-sm-6 col-md-3">
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
            <div class="col-sm-6 col-md-3">
                <div class="form-group">
                    <asp:Label ID="lblDesde" runat="server" AssociatedControlID="txtDesde"
                        CssClass="control-label" Text="Fecha desde"></asp:Label>
                    <asp:TextBox ID="txtDesde" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                    <asp:RangeValidator ID="rvDesde" runat="server" ControlToValidate="txtDesde"
                        Type="Date" MinimumValue="2000-01-01" MaximumValue="2099-12-31" ValidationGroup="Filtro"
                        Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="Ingrese una fecha entre el 01/01/2000 y el 31/12/2099."></asp:RangeValidator>
                </div>
            </div>
            <div class="col-sm-6 col-md-3">
                <div class="form-group">
                    <asp:Label ID="lblHasta" runat="server" AssociatedControlID="txtHasta"
                        CssClass="control-label" Text="Fecha hasta"></asp:Label>
                    <asp:TextBox ID="txtHasta" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                    <asp:CompareValidator ID="cvHasta" runat="server" ControlToValidate="txtHasta"
                        ControlToCompare="txtDesde" Operator="GreaterThanEqual" Type="Date"
                        ValidationGroup="Filtro" Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="La fecha hasta no puede ser anterior a la fecha desde."></asp:CompareValidator>
                    <asp:RangeValidator ID="rvHasta" runat="server" ControlToValidate="txtHasta"
                        Type="Date" MinimumValue="2000-01-01" MaximumValue="2099-12-31" ValidationGroup="Filtro"
                        Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="Ingrese una fecha entre el 01/01/2000 y el 31/12/2099."></asp:RangeValidator>
                </div>
            </div>
        </div>
        <div class="filtros-acciones">
            <asp:Button ID="btnAceptar" runat="server" Text="Buscar" CssClass="btn btn-primary"
                OnClick="btnAceptar_Click" ValidationGroup="Filtro" />
            <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
                OnClick="btnCancelar_Click" CausesValidation="false" />
        </div>
    </div>

    <asp:Literal ID="litIndicadores" runat="server" Mode="PassThrough"></asp:Literal>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true" DataKeyNames="IdOrdenCompra" OnRowCommand="gvDetalle_RowCommand"
        EmptyDataText="No hay órdenes de compra para mostrar.">
        <SelectedRowStyle CssClass="info" />
        <Columns>
            <asp:BoundField DataField="Numero" HeaderText="Número" />
            <asp:BoundField DataField="ProveedorTexto" HeaderText="Proveedor" />
            <asp:BoundField DataField="FechaTexto" HeaderText="Fecha" />
            <asp:BoundField DataField="EstadoBase" HeaderText="Estado" />
            <asp:BoundField DataField="DetalleEstado" HeaderText="Comprobante / motivo" />
            <asp:BoundField DataField="CantidadItems" HeaderText="Ítems" />
            <asp:ButtonField CommandName="Ver" Text="Ver detalle" ButtonType="Button"
                ControlStyle-CssClass="btn btn-default btn-xs" />
        </Columns>
    </asp:GridView>

    <asp:Panel ID="pnlItems" runat="server" Visible="false">
        <h3 class="seccion-titulo"><asp:Literal ID="litTituloOrden" runat="server"></asp:Literal>
            <small><asp:Literal ID="litDatosOrden" runat="server"></asp:Literal></small></h3>
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

</asp:Content>
