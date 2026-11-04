<%@ Page Title="Descontar stock por venta" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="DescontarStockVenta.aspx.cs" Inherits="DescontarStockVenta" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Descontar stock por venta</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">Elija la receta vendida y la cantidad: la grilla muestra cuánto se descuenta de cada insumo y cómo queda el stock antes de confirmar. Si a algún insumo no le alcanza, no se descuenta nada.</p>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblReceta" runat="server" AssociatedControlID="ddlReceta"
                    CssClass="control-label" Text="Receta vendida (*)"></asp:Label>
                <asp:DropDownList ID="ddlReceta" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnSelectedIndexChanged="ddlReceta_SelectedIndexChanged">
                    <asp:ListItem Text="(seleccione)" Value="" />
                </asp:DropDownList>
                <asp:RequiredFieldValidator ID="rfvReceta" runat="server" ControlToValidate="ddlReceta"
                    ValidationGroup="Venta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Elija la receta vendida."></asp:RequiredFieldValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblCantidad" runat="server" AssociatedControlID="txtCantidad"
                    CssClass="control-label" Text="Cantidad vendida (*)"></asp:Label>
                <asp:TextBox ID="txtCantidad" runat="server" CssClass="form-control" TextMode="Number" min="0.01" step="0.01"
                    AutoPostBack="true" OnTextChanged="txtCantidad_TextChanged" max="10000"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvCantidad" runat="server" ControlToValidate="txtCantidad"
                    ValidationGroup="Venta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Ingrese la cantidad vendida."></asp:RequiredFieldValidator>
                <asp:RangeValidator ID="rvCantidad" runat="server" ControlToValidate="txtCantidad"
                    Type="Double" MinimumValue="0.01" MaximumValue="10000" CultureInvariantValues="true"
                    ValidationGroup="Venta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La cantidad vendida tiene que estar entre 0,01 y 10.000."></asp:RangeValidator>
                <asp:RegularExpressionValidator ID="revDecimalesCantidad" runat="server" ControlToValidate="txtCantidad"
                    ValidationExpression="^\d+([.,]\d{1,2})?$" ValidationGroup="Venta" Display="Dynamic"
                    CssClass="text-danger" ErrorMessage="La cantidad vendida admite hasta dos decimales."></asp:RegularExpressionValidator>
                <span class="campo-ayuda">Hasta 10000 unidades, con hasta dos decimales (media porción: 0,5).</span>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblFecha" runat="server" AssociatedControlID="txtFecha"
                    CssClass="control-label" Text="Fecha de la venta (*)"></asp:Label>
                <asp:TextBox ID="txtFecha" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvFecha" runat="server" ControlToValidate="txtFecha"
                    ValidationGroup="Venta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Ingrese la fecha de la venta."></asp:RequiredFieldValidator>
                <asp:RangeValidator ID="rvFecha" runat="server" ControlToValidate="txtFecha"
                    Type="Date" ValidationGroup="Venta" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La fecha de la venta tiene que ser de hoy o de hasta un año atrás."></asp:RangeValidator>
                <span class="campo-ayuda">Hoy o hasta un año atrás.</span>
            </div>
        </div>
    </div>

    <asp:Literal ID="litIndicadores" runat="server" Mode="PassThrough"></asp:Literal>

    <h3 class="seccion-titulo">Cómo queda el stock</h3>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="Elija la receta y la cantidad vendida.">
        <Columns>
            <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
            <asp:BoundField DataField="ConsumoUnitarioTexto" HeaderText="Consumo unitario" />
            <asp:BoundField DataField="ConsumoTotalTexto" HeaderText="Consumo total" />
            <asp:BoundField DataField="StockAnteriorTexto" HeaderText="Stock actual" />
            <asp:BoundField DataField="StockResultanteTexto" HeaderText="Stock resultante" ItemStyle-Font-Bold="true" />
        </Columns>
    </asp:GridView>

    <div class="acciones">
        <asp:Button ID="btnAceptar" runat="server" Text="Descontar stock" CssClass="btn btn-primary"
            OnClick="btnAceptar_Click"
            OnClientClick="return confirm('Se va a registrar la venta y descontar del stock los insumos de la receta. ¿Continuar?');" ValidationGroup="Venta" />
        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
            OnClick="btnCancelar_Click" CausesValidation="false" />
    </div>

</asp:Content>
