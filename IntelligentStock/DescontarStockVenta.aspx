<%@ Page Title="Descontar stock por venta" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="DescontarStockVenta.aspx.cs" Inherits="DescontarStockVenta" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Descontar stock por venta</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">Los pedidos los toma y los factura el punto de venta del local; acá se cargan las ventas para descontar del stock lo que lleva cada receta. Se pueden cargar de a una, todas las del día juntas al cierre, o importando el archivo que exporta el punto de venta. Si el stock del sistema no alcanza, la venta igual se registra (ya ocurrió): el insumo queda en cero y conviene corregirlo con un ajuste de stock.</p>

    <ul class="nav nav-tabs" style="margin-bottom: 15px;">
        <li id="tabUna" runat="server" class="active">
            <asp:LinkButton ID="lnkUna" runat="server" CommandArgument="0" OnClick="lnkModo_Click" CausesValidation="false">Una venta</asp:LinkButton>
        </li>
        <li id="tabCierre" runat="server">
            <asp:LinkButton ID="lnkCierre" runat="server" CommandArgument="1" OnClick="lnkModo_Click" CausesValidation="false">Cierre del día</asp:LinkButton>
        </li>
        <li id="tabArchivo" runat="server">
            <asp:LinkButton ID="lnkArchivo" runat="server" CommandArgument="2" OnClick="lnkModo_Click" CausesValidation="false">Importar archivo del punto de venta</asp:LinkButton>
        </li>
    </ul>

    <asp:MultiView ID="mvModo" runat="server" ActiveViewIndex="0">

        <asp:View ID="vwUna" runat="server">
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
        </asp:View>

        <asp:View ID="vwCierre" runat="server">
            <p class="help-block">Al cerrar el día, cargue cuántas unidades se vendieron de cada receta (el total que informa el punto de venta) y registre todo junto. Las recetas sin cantidad no se registran.</p>
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

        <asp:View ID="vwArchivo" runat="server">
            <p class="help-block">
                Importe el archivo de ventas que exporta el punto de venta (CSV, separado por punto y coma, coma o tabulación) con tres columnas: <strong>fecha</strong>, <strong>receta</strong> y <strong>cantidad</strong>. La primera línea puede tener los nombres de las columnas (en cualquier orden). La receta se busca por su nombre o por su número; las líneas de una misma receta y un mismo día se suman. Por ejemplo:
            </p>
            <pre>fecha;receta;cantidad
17/11/2026;Pizza muzzarella;24
17/11/2026;Hamburguesa completa;18,5</pre>
            <div class="form-group">
                <asp:Label ID="lblArchivoVentas" runat="server" AssociatedControlID="fuVentas"
                    CssClass="control-label" Text="Archivo de ventas (*)"></asp:Label>
                <asp:FileUpload ID="fuVentas" runat="server" CssClass="form-control" accept=".csv,.txt" />
                <span class="campo-ayuda">Hasta 2 MB y 5000 líneas. Fechas de hoy o de hasta un año atrás.</span>
            </div>
            <asp:Button ID="btnLeerArchivo" runat="server" Text="Leer archivo" CssClass="btn btn-default"
                OnClick="btnLeerArchivo_Click" CausesValidation="false" />

            <asp:Literal ID="litAvisosArchivo" runat="server" Mode="PassThrough"></asp:Literal>

            <asp:Panel ID="pnlArchivo" runat="server" Visible="false">
                <h3 class="seccion-titulo">Ventas del archivo</h3>
                <asp:GridView ID="gvArchivo" runat="server" AutoGenerateColumns="false"
                    CssClass="table table-bordered table-condensed table-striped" GridLines="None"
                    ShowHeaderWhenEmpty="true" EmptyDataText="El archivo no tiene ventas válidas.">
                    <Columns>
                        <asp:BoundField DataField="FechaTexto" HeaderText="Fecha" />
                        <asp:BoundField DataField="NombreReceta" HeaderText="Receta" />
                        <asp:BoundField DataField="CantidadTexto" HeaderText="Unidades" />
                    </Columns>
                </asp:GridView>

                <asp:Panel ID="pnlErrores" runat="server" Visible="false">
                    <h3 class="seccion-titulo">Líneas con errores <small>(no se registran)</small></h3>
                    <asp:GridView ID="gvErrores" runat="server" AutoGenerateColumns="false"
                        CssClass="table table-bordered table-condensed" GridLines="None">
                        <Columns>
                            <asp:BoundField DataField="Linea" HeaderText="Línea" ItemStyle-Width="80px" />
                            <asp:BoundField DataField="Error" HeaderText="Problema" />
                        </Columns>
                    </asp:GridView>
                </asp:Panel>
            </asp:Panel>
        </asp:View>

    </asp:MultiView>

    <asp:Literal ID="litIndicadores" runat="server" Mode="PassThrough"></asp:Literal>

    <h3 class="seccion-titulo">Cómo queda el stock</h3>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="Cargue las ventas para ver cuánto se descuenta de cada insumo.">
        <Columns>
            <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
            <asp:BoundField DataField="ConsumoUnitarioTexto" HeaderText="Consumo unitario" />
            <asp:BoundField DataField="ConsumoTotalTexto" HeaderText="Consumo total" />
            <asp:BoundField DataField="StockAnteriorTexto" HeaderText="Stock actual" />
            <asp:BoundField DataField="StockResultanteTexto" HeaderText="Stock resultante" ItemStyle-Font-Bold="true" />
            <asp:BoundField DataField="FaltanteTexto" HeaderText="Faltó" />
        </Columns>
    </asp:GridView>

    <div class="acciones">
        <asp:Button ID="btnAceptar" runat="server" Text="Descontar stock" CssClass="btn btn-primary"
            OnClick="btnAceptar_Click"
            OnClientClick="return confirm('Se van a registrar las ventas y descontar del stock los insumos de cada receta. ¿Continuar?');" ValidationGroup="Venta" />
        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
            OnClick="btnCancelar_Click" CausesValidation="false" />
    </div>

</asp:Content>
