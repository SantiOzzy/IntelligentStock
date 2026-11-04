<%@ Page Title="Registrar factura de proveedor" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="RegistrarFacturaProveedor.aspx.cs" Inherits="RegistrarFacturaProveedor" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Registrar factura de proveedor</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">Elija la orden pendiente que llegó y cargue los datos de la factura. Al registrarla, la orden pasa a Recibida y lo pedido ingresa al stock.</p>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblOrden" runat="server" AssociatedControlID="ddlOrden"
                    CssClass="control-label" Text="Orden de compra (*)"></asp:Label>
                <asp:DropDownList ID="ddlOrden" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnSelectedIndexChanged="ddlOrden_SelectedIndexChanged">
                    <asp:ListItem Text="(seleccione)" Value="" />
                </asp:DropDownList>
                <asp:RequiredFieldValidator ID="rfvOrden" runat="server" ControlToValidate="ddlOrden"
                    ValidationGroup="Factura" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Elija la orden de compra."></asp:RequiredFieldValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblComprobante" runat="server" AssociatedControlID="txtComprobante"
                    CssClass="control-label" Text="Número de comprobante (*)"></asp:Label>
                <asp:TextBox ID="txtComprobante" runat="server" CssClass="form-control" MaxLength="40"
                    placeholder="A-0001-00001234"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvComprobante" runat="server" ControlToValidate="txtComprobante"
                    ValidationGroup="Factura" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Ingrese el número de comprobante."></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="revComprobante" runat="server" ControlToValidate="txtComprobante"
                    ValidationExpression="^[A-Za-z0-9][A-Za-z0-9 \-/]*$" ValidationGroup="Factura"
                    Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El comprobante admite letras, números, espacios, guiones o barras (por ejemplo A-0001-00001234)."></asp:RegularExpressionValidator>
                <span class="campo-ayuda">No se puede repetir para el mismo proveedor.</span>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblFecha" runat="server" AssociatedControlID="txtFecha"
                    CssClass="control-label" Text="Fecha del comprobante (*)"></asp:Label>
                <asp:TextBox ID="txtFecha" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvFecha" runat="server" ControlToValidate="txtFecha"
                    ValidationGroup="Factura" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Ingrese la fecha del comprobante."></asp:RequiredFieldValidator>
                <asp:RangeValidator ID="rvFecha" runat="server" ControlToValidate="txtFecha"
                    Type="Date" ValidationGroup="Factura" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La fecha del comprobante no puede ser posterior a hoy."></asp:RangeValidator>
                <span class="campo-ayuda">Entre la fecha de la orden y hoy.</span>
            </div>
            <div class="form-group">
                <asp:Label ID="lblImporte" runat="server" AssociatedControlID="txtImporte"
                    CssClass="control-label" Text="Importe total (*)"></asp:Label>
                <asp:TextBox ID="txtImporte" runat="server" CssClass="form-control" TextMode="Number" min="0.01" step="0.01" max="9999999999.99"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvImporte" runat="server" ControlToValidate="txtImporte"
                    ValidationGroup="Factura" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Ingrese el importe total."></asp:RequiredFieldValidator>
                <asp:RangeValidator ID="rvImporte" runat="server" ControlToValidate="txtImporte"
                    Type="Double" MinimumValue="0.01" MaximumValue="9999999999.99"
                    CultureInvariantValues="true" ValidationGroup="Factura" Display="Dynamic"
                    CssClass="text-danger"
                    ErrorMessage="El importe total tiene que estar entre 0,01 y 9.999.999.999,99."></asp:RangeValidator>
                <asp:RegularExpressionValidator ID="revDecimalesImporte" runat="server" ControlToValidate="txtImporte"
                    ValidationExpression="^\d+([.,]\d{1,2})?$" ValidationGroup="Factura" Display="Dynamic"
                    CssClass="text-danger" ErrorMessage="El importe total admite hasta dos decimales."></asp:RegularExpressionValidator>
                <span class="campo-ayuda">Se propone el importe estimado de la orden: corríjalo con el total de la factura.</span>
            </div>
        </div>
    </div>

    <asp:Literal ID="litIndicadores" runat="server" Mode="PassThrough"></asp:Literal>

    <h3 class="seccion-titulo">Insumos que ingresan al stock</h3>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="Elija una orden para ver sus insumos.">
        <Columns>
            <asp:BoundField DataField="Insumo" HeaderText="Insumo" />
            <asp:BoundField DataField="Cantidad" HeaderText="Cantidad recibida" />
            <asp:BoundField DataField="StockAntes" HeaderText="Stock antes" />
            <asp:BoundField DataField="StockDespues" HeaderText="Stock después" ItemStyle-Font-Bold="true" />
            <asp:BoundField DataField="Importe" HeaderText="Importe estimado" />
        </Columns>
    </asp:GridView>

    <div class="acciones">
        <asp:Button ID="btnAceptar" runat="server" Text="Registrar factura" CssClass="btn btn-primary"
            OnClick="btnAceptar_Click"
            OnClientClick="return confirm('Se va a registrar la factura, la orden va a quedar Recibida y lo pedido va a ingresar al stock. ¿Continuar?');" ValidationGroup="Factura" />
        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
            OnClick="btnCancelar_Click" CausesValidation="false" />
    </div>

</asp:Content>
