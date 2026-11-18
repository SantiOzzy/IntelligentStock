<%@ Page Title="Descontar stock por venta" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="DescontarStockVenta.aspx.cs" Inherits="DescontarStockVenta" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Descontar stock por venta</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">Acá se cargan las ventas para descontar del stock lo que lleva cada receta. Se pueden cargar por pedido, con una o varias recetas (por ejemplo, todo lo que pidió una mesa), o todas las del día juntas al cierre. Si el stock del sistema no alcanza, la venta igual se registra (ya ocurrió): el insumo queda en cero y conviene corregirlo con un ajuste de stock. En "Ventas registradas" se consultan las ventas cargadas y lo que descontó cada una.</p>

    <ul class="nav nav-tabs" style="margin-bottom: 15px;">
        <li id="tabPedido" runat="server" class="active">
            <asp:LinkButton ID="lnkPedido" runat="server" CommandArgument="0" OnClick="lnkModo_Click" CausesValidation="false">Venta</asp:LinkButton>
        </li>
        <li id="tabCierre" runat="server">
            <asp:LinkButton ID="lnkCierre" runat="server" CommandArgument="1" OnClick="lnkModo_Click" CausesValidation="false">Cierre del día</asp:LinkButton>
        </li>
        <li id="tabVentas" runat="server">
            <asp:LinkButton ID="lnkVentas" runat="server" CommandArgument="2" OnClick="lnkModo_Click" CausesValidation="false">Ventas registradas</asp:LinkButton>
        </li>
    </ul>

    <asp:MultiView ID="mvModo" runat="server" ActiveViewIndex="0">

        <asp:View ID="vwPedido" runat="server">
            <div class="row">
                <div class="col-sm-6 col-md-4">
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

            <asp:Panel ID="pnlAgregar" runat="server" DefaultButton="btnAgregar">
                <div class="row">
                    <div class="col-sm-6 col-md-5">
                        <div class="form-group">
                            <asp:Label ID="lblReceta" runat="server" AssociatedControlID="ddlReceta"
                                CssClass="control-label" Text="Receta vendida (*)"></asp:Label>
                            <asp:DropDownList ID="ddlReceta" runat="server" CssClass="form-control">
                                <asp:ListItem Text="(seleccione)" Value="" />
                            </asp:DropDownList>
                            <asp:RequiredFieldValidator ID="rfvReceta" runat="server" ControlToValidate="ddlReceta"
                                ValidationGroup="Agregar" Display="Dynamic" CssClass="text-danger"
                                ErrorMessage="Elija la receta vendida."></asp:RequiredFieldValidator>
                        </div>
                    </div>
                    <div class="col-sm-3 col-md-3">
                        <div class="form-group">
                            <asp:Label ID="lblCantidad" runat="server" AssociatedControlID="txtCantidad"
                                CssClass="control-label" Text="Unidades (*)"></asp:Label>
                            <asp:TextBox ID="txtCantidad" runat="server" CssClass="form-control" TextMode="Number"
                                min="0.01" step="0.01" max="10000"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="rfvCantidad" runat="server" ControlToValidate="txtCantidad"
                                ValidationGroup="Agregar" Display="Dynamic" CssClass="text-danger"
                                ErrorMessage="Ingrese la cantidad vendida."></asp:RequiredFieldValidator>
                            <asp:RangeValidator ID="rvCantidad" runat="server" ControlToValidate="txtCantidad"
                                Type="Double" MinimumValue="0.01" MaximumValue="10000" CultureInvariantValues="true"
                                ValidationGroup="Agregar" Display="Dynamic" CssClass="text-danger"
                                ErrorMessage="La cantidad tiene que estar entre 0,01 y 10.000."></asp:RangeValidator>
                            <asp:RegularExpressionValidator ID="revDecimalesCantidad" runat="server" ControlToValidate="txtCantidad"
                                ValidationExpression="^\d+([.,]\d{1,2})?$" ValidationGroup="Agregar" Display="Dynamic"
                                CssClass="text-danger" ErrorMessage="La cantidad admite hasta dos decimales."></asp:RegularExpressionValidator>
                        </div>
                    </div>
                    <div class="col-sm-3 col-md-4">
                        <div class="form-group">
                            <span class="control-label hidden-xs" style="display: block;">&nbsp;</span>
                            <asp:Button ID="btnAgregar" runat="server" Text="Agregar al pedido" CssClass="btn btn-default"
                                OnClick="btnAgregar_Click" ValidationGroup="Agregar" />
                        </div>
                    </div>
                </div>
                <span class="campo-ayuda">Elija cada receta, la cantidad y presione Agregar al pedido (o Enter). Si agrega una receta que ya está, se suman las unidades. Media porción: 0,5.</span>
            </asp:Panel>

            <h3 class="seccion-titulo">Pedido</h3>
            <asp:GridView ID="gvPedido" runat="server" AutoGenerateColumns="false"
                CssClass="table table-bordered table-condensed table-striped" GridLines="None"
                ShowHeaderWhenEmpty="true" OnRowCommand="gvPedido_RowCommand"
                EmptyDataText="Todavía no hay recetas en el pedido.">
                <Columns>
                    <asp:BoundField DataField="NombreReceta" HeaderText="Receta" />
                    <asp:BoundField DataField="CantidadTexto" HeaderText="Unidades" ItemStyle-Width="150px" />
                    <asp:ButtonField CommandName="Quitar" Text="Quitar" ButtonType="Button"
                        ControlStyle-CssClass="btn btn-default btn-xs" ItemStyle-Width="90px" />
                </Columns>
            </asp:GridView>
        </asp:View>

        <asp:View ID="vwCierre" runat="server">
            <p class="help-block">Al cerrar el día, cargue cuántas unidades se vendieron de cada receta (el total que informa el punto de venta) y registre todo junto. Las recetas sin cantidad no se registran. Si ese día ya se cargaron pedidos, cargue solo lo que falta: si no, se contarían dos veces.</p>
            <div class="row">
                <div class="col-md-6">
                    <div class="form-group">
                        <asp:Label ID="lblFechaCierre" runat="server" AssociatedControlID="txtFechaCierre"
                            CssClass="control-label" Text="Día vendido (*)"></asp:Label>
                        <asp:TextBox ID="txtFechaCierre" runat="server" CssClass="form-control" TextMode="Date"
                            AutoPostBack="true" OnTextChanged="txtFechaCierre_TextChanged"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvFechaCierre" runat="server" ControlToValidate="txtFechaCierre"
                            ValidationGroup="Cierre" Display="Dynamic" CssClass="text-danger"
                            ErrorMessage="Ingrese el día de las ventas."></asp:RequiredFieldValidator>
                        <asp:RangeValidator ID="rvFechaCierre" runat="server" ControlToValidate="txtFechaCierre"
                            Type="Date" ValidationGroup="Cierre" Display="Dynamic" CssClass="text-danger"
                            ErrorMessage="El día tiene que ser hoy o de hasta un año atrás."></asp:RangeValidator>
                        <span class="campo-ayuda">Hoy o hasta un año atrás.</span>
                    </div>
                </div>
            </div>

            <asp:Label ID="lblAvisoCierre" runat="server" CssClass="alert alert-warning" Visible="false" style="display: block;"></asp:Label>

            <asp:GridView ID="gvCierre" runat="server" AutoGenerateColumns="false" DataKeyNames="IdReceta"
                CssClass="table table-bordered table-condensed table-striped" GridLines="None"
                ShowHeaderWhenEmpty="true" EmptyDataText="No hay recetas activas con insumos cargados.">
                <Columns>
                    <asp:BoundField DataField="Nombre" HeaderText="Receta" />
                    <asp:TemplateField HeaderText="Unidades vendidas" ItemStyle-Width="200px">
                        <ItemTemplate>
                            <asp:TextBox ID="txtCantidadReceta" runat="server" CssClass="form-control input-sm"
                                TextMode="Number" min="0" step="0.01" max="10000"></asp:TextBox>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>

            <asp:Button ID="btnPrevisualizarCierre" runat="server" Text="Ver cómo queda el stock"
                CssClass="btn btn-default" OnClick="btnPrevisualizarCierre_Click" ValidationGroup="Cierre" />
        </asp:View>

        <asp:View ID="vwVentas" runat="server">
            <div class="row">
                <div class="col-sm-6 col-md-3">
                    <div class="form-group">
                        <asp:Label ID="lblVentasDesde" runat="server" AssociatedControlID="txtVentasDesde"
                            CssClass="control-label" Text="Fecha desde (*)"></asp:Label>
                        <asp:TextBox ID="txtVentasDesde" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvVentasDesde" runat="server" ControlToValidate="txtVentasDesde"
                            ValidationGroup="Ventas" Display="Dynamic" CssClass="text-danger"
                            ErrorMessage="Ingrese la fecha desde."></asp:RequiredFieldValidator>
                    </div>
                </div>
                <div class="col-sm-6 col-md-3">
                    <div class="form-group">
                        <asp:Label ID="lblVentasHasta" runat="server" AssociatedControlID="txtVentasHasta"
                            CssClass="control-label" Text="Fecha hasta (*)"></asp:Label>
                        <asp:TextBox ID="txtVentasHasta" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvVentasHasta" runat="server" ControlToValidate="txtVentasHasta"
                            ValidationGroup="Ventas" Display="Dynamic" CssClass="text-danger"
                            ErrorMessage="Ingrese la fecha hasta."></asp:RequiredFieldValidator>
                        <asp:CompareValidator ID="cvVentasHasta" runat="server" ControlToValidate="txtVentasHasta"
                            ControlToCompare="txtVentasDesde" Operator="GreaterThanEqual" Type="Date"
                            ValidationGroup="Ventas" Display="Dynamic" CssClass="text-danger"
                            ErrorMessage="La fecha hasta no puede ser anterior a la fecha desde."></asp:CompareValidator>
                    </div>
                </div>
                <div class="col-sm-6 col-md-4">
                    <div class="form-group">
                        <asp:Label ID="lblRecetaVentas" runat="server" AssociatedControlID="ddlRecetaVentas"
                            CssClass="control-label" Text="Receta"></asp:Label>
                        <asp:DropDownList ID="ddlRecetaVentas" runat="server" CssClass="form-control">
                            <asp:ListItem Text="(todas)" Value="" />
                        </asp:DropDownList>
                    </div>
                </div>
                <div class="col-sm-6 col-md-2">
                    <div class="form-group">
                        <span class="control-label hidden-xs hidden-sm" style="display: block;">&nbsp;</span>
                        <asp:Button ID="btnBuscarVentas" runat="server" Text="Buscar" CssClass="btn btn-default"
                            OnClick="btnBuscarVentas_Click" ValidationGroup="Ventas" />
                    </div>
                </div>
            </div>
            <span class="campo-ayuda">Hasta un año. Con "Ver consumo" se muestra lo que descontó la venta de cada insumo.</span>

            <asp:Literal ID="litIndicadoresVentas" runat="server" Mode="PassThrough"></asp:Literal>

            <asp:GridView ID="gvVentas" runat="server" AutoGenerateColumns="false" DataKeyNames="IdVenta"
                CssClass="table table-bordered table-condensed table-striped" GridLines="None"
                ShowHeaderWhenEmpty="true" OnRowCommand="gvVentas_RowCommand"
                AllowPaging="true" PageSize="25" OnPageIndexChanging="gvVentas_PageIndexChanging"
                EmptyDataText="No hay ventas registradas en ese período.">
                <SelectedRowStyle CssClass="info" />
                <PagerStyle CssClass="text-center" />
                <Columns>
                    <asp:BoundField DataField="FechaTexto" HeaderText="Fecha" />
                    <asp:BoundField DataField="NombreReceta" HeaderText="Receta" />
                    <asp:BoundField DataField="CantidadTexto" HeaderText="Unidades" />
                    <asp:BoundField DataField="CantidadInsumos" HeaderText="Insumos descontados" />
                    <asp:ButtonField CommandName="Consumo" Text="Ver consumo" ButtonType="Button"
                        ControlStyle-CssClass="btn btn-default btn-xs" />
                </Columns>
            </asp:GridView>

            <asp:Panel ID="pnlConsumoVenta" runat="server" Visible="false">
                <h3 class="seccion-titulo"><asp:Literal ID="litTituloConsumo" runat="server"></asp:Literal></h3>
                <asp:GridView ID="gvConsumoVenta" runat="server" AutoGenerateColumns="false"
                    CssClass="table table-bordered table-condensed table-striped" GridLines="None"
                    ShowHeaderWhenEmpty="true"
                    EmptyDataText="La venta no descontó insumos.">
                    <Columns>
                        <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
                        <asp:BoundField DataField="ConsumoTotalTexto" HeaderText="Se descontó" />
                    </Columns>
                </asp:GridView>
            </asp:Panel>
        </asp:View>

    </asp:MultiView>

    <asp:Panel ID="pnlStock" runat="server">
    <asp:Literal ID="litIndicadores" runat="server" Mode="PassThrough"></asp:Literal>

    <h3 class="seccion-titulo">Cómo queda el stock</h3>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="Cargue las ventas para ver cuánto se descuenta de cada insumo.">
        <Columns>
            <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
            <asp:BoundField DataField="ConsumoTotalTexto" HeaderText="Se descuenta" />
            <asp:BoundField DataField="StockAnteriorTexto" HeaderText="Stock actual" />
            <asp:BoundField DataField="StockResultanteTexto" HeaderText="Stock resultante" ItemStyle-Font-Bold="true" />
            <asp:BoundField DataField="FaltanteTexto" HeaderText="Faltó" />
        </Columns>
    </asp:GridView>
    </asp:Panel>

    <div class="acciones">
        <asp:Button ID="btnAceptar" runat="server" Text="Registrar la venta" CssClass="btn btn-primary"
            OnClick="btnAceptar_Click"
            OnClientClick="return confirm('Se van a registrar las ventas y descontar del stock los insumos de cada receta. ¿Continuar?');" ValidationGroup="Venta" />
        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
            OnClick="btnCancelar_Click" CausesValidation="false" />
    </div>

</asp:Content>
