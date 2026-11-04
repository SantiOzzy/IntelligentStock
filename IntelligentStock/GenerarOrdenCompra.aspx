<%@ Page Title="Generar orden de compra" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="GenerarOrdenCompra.aspx.cs" Inherits="GenerarOrdenCompra" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Generar orden de compra</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">El sistema estima con ML.NET cuánto se va a consumir de cada insumo en los próximos días, a partir de las ventas registradas, y propone lo que hace falta pedir. Elija el proveedor y la fecha, revise la propuesta y genere la orden.</p>

    <asp:Literal ID="litIndicadores" runat="server" Mode="PassThrough"></asp:Literal>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblProveedor" runat="server" AssociatedControlID="ddlProveedor"
                    CssClass="control-label" Text="Proveedor (*)"></asp:Label>
                <asp:DropDownList ID="ddlProveedor" runat="server" CssClass="form-control">
                    <asp:ListItem Text="(seleccione)" Value="" />
                </asp:DropDownList>
                <asp:RequiredFieldValidator ID="rfvProveedor" runat="server" ControlToValidate="ddlProveedor"
                    ValidationGroup="Orden" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Elija el proveedor."></asp:RequiredFieldValidator>
                <span class="campo-ayuda">Solo figuran los proveedores activos.</span>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblFecha" runat="server" AssociatedControlID="txtFecha"
                    CssClass="control-label" Text="Fecha de la orden (*)"></asp:Label>
                <asp:TextBox ID="txtFecha" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvFecha" runat="server" ControlToValidate="txtFecha"
                    ValidationGroup="Orden" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Ingrese la fecha de la orden."></asp:RequiredFieldValidator>
                <asp:RangeValidator ID="rvFecha" runat="server" ControlToValidate="txtFecha"
                    Type="Date" ValidationGroup="Orden" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La fecha de la orden tiene que estar entre hoy y 60 días adelante."></asp:RangeValidator>
                <span class="campo-ayuda">Desde hoy y hasta 60 días adelante.</span>
            </div>
        </div>
    </div>

    <h3 class="seccion-titulo">Propuesta de compra <small><asp:Literal ID="litPeriodo" runat="server"></asp:Literal></small></h3>

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
            <asp:BoundField DataField="CantidadSugeridaTexto" HeaderText="Cantidad sugerida" ItemStyle-Font-Bold="true" />
            <asp:BoundField DataField="ImporteEstimadoTexto" HeaderText="Importe estimado" />
        </Columns>
    </asp:GridView>

    <asp:Label ID="lblPrediccion" runat="server" CssClass="help-block" Visible="false"></asp:Label>

    <div class="acciones">
        <asp:Button ID="btnAceptar" runat="server" Text="Generar orden" CssClass="btn btn-primary"
            OnClick="btnAceptar_Click"
            OnClientClick="return confirm('Se va a generar una orden de compra Pendiente con las cantidades sugeridas. ¿Continuar?');" ValidationGroup="Orden" />
        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
            OnClick="btnCancelar_Click" CausesValidation="false" />
    </div>

</asp:Content>
