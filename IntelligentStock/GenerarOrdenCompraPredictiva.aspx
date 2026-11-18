<%@ Page Title="Generar orden de compra" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="GenerarOrdenCompraPredictiva.aspx.cs" Inherits="GenerarOrdenCompraPredictiva" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Generar orden de compra</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">El sistema estima con ML.NET cuánto se va a consumir de cada insumo en los próximos días, a partir de las ventas registradas, y propone qué pedir (los perecederos, solo para los días que duran) y a quién: para cada insumo sugiere el proveedor que lo facturó más barato en los últimos 60 días, según las facturas registradas. Puede desmarcar insumos, cambiar las cantidades o elegir otro proveedor. Se genera una orden por proveedor.</p>

    <asp:Literal ID="litIndicadores" runat="server" Mode="PassThrough"></asp:Literal>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblFecha" runat="server" AssociatedControlID="txtFecha"
                    CssClass="control-label" Text="Fecha de las órdenes (*)"></asp:Label>
                <asp:TextBox ID="txtFecha" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvFecha" runat="server" ControlToValidate="txtFecha"
                    ValidationGroup="Orden" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Ingrese la fecha de las órdenes."></asp:RequiredFieldValidator>
                <asp:RangeValidator ID="rvFecha" runat="server" ControlToValidate="txtFecha"
                    Type="Date" ValidationGroup="Orden" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La fecha de las órdenes tiene que estar entre hoy y 60 días adelante."></asp:RangeValidator>
                <span class="campo-ayuda">Desde hoy y hasta 60 días adelante.</span>
            </div>
        </div>
        <div class="col-md-6">
            <div class="checkbox" style="margin-top: 30px;">
                <asp:CheckBox ID="chkEnviarCorreo" runat="server" Checked="true"
                    Text="Enviarle a cada proveedor su orden por correo (con la orden en PDF)" />
            </div>
        </div>
    </div>

    <h3 class="seccion-titulo">Propuesta de compra <small><asp:Literal ID="litPeriodo" runat="server"></asp:Literal></small></h3>

    <asp:Label ID="lblAvisoPrediccion" runat="server" CssClass="alert alert-warning" Visible="false" style="display: block;"></asp:Label>

    <div class="table-responsive">
    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false" DataKeyNames="IdInsumo"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true" OnRowDataBound="gvDetalle_RowDataBound"
        EmptyDataText="No hay cantidades sugeridas para mostrar.">
        <Columns>
            <asp:TemplateField HeaderText="Pedir" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="50px">
                <ItemTemplate>
                    <asp:CheckBox ID="chkIncluir" runat="server" Checked='<%# Eval("Incluir") %>'
                        AutoPostBack="true" OnCheckedChanged="Grilla_Cambio" />
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
            <asp:TemplateField HeaderText="Stock actual" ItemStyle-Wrap="false">
                <ItemTemplate>
                    <%# HttpUtility.HtmlEncode(Eval("StockActualTexto")) %>
                    <span class="campo-ayuda"><%# HttpUtility.HtmlEncode(Eval("YaPedidoTexto")) %></span>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="ConsumoPrevistoPeriodoTexto" HeaderText="Consumo previsto" />
            <asp:TemplateField HeaderText="A pedir">
                <ItemTemplate>
                    <div class="input-group input-group-sm control-cantidad">
                        <asp:TextBox ID="txtCantidadPedir" runat="server" CssClass="form-control" TextMode="Number"
                            min="0.01" step="0.01" max="999999.99" AutoPostBack="true" OnTextChanged="Grilla_Cambio"
                            Text='<%# Numeros.ParaInput((decimal)Eval("CantidadAPedir")) %>'></asp:TextBox>
                        <span class="input-group-addon"><%# HttpUtility.HtmlEncode(Eval("UnidadDeMedida")) %></span>
                    </div>
                    <span class="campo-ayuda"><%# HttpUtility.HtmlEncode(Eval("SugeridoTexto")) %></span>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Proveedor">
                <ItemTemplate>
                    <asp:DropDownList ID="ddlProveedorFila" runat="server" CssClass="form-control control-lista"
                        AutoPostBack="true" OnSelectedIndexChanged="Grilla_Cambio"></asp:DropDownList>
                    <span class="campo-ayuda"><%# HttpUtility.HtmlEncode(Eval("MotivoProveedor")) %></span>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Precio unitario" ItemStyle-CssClass="celda-precio">
                <ItemTemplate>
                    <span style="white-space: nowrap;"><%# HttpUtility.HtmlEncode(Eval("PrecioUnitarioTexto")) %></span>
                    <span class="campo-ayuda"><%# HttpUtility.HtmlEncode(Eval("DetallePrecioTexto")) %></span>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="ImporteEstimadoTexto" HeaderText="Importe" ItemStyle-Wrap="false" />
        </Columns>
    </asp:GridView>
    </div>

    <asp:Label ID="lblPrediccion" runat="server" CssClass="help-block" Visible="false"></asp:Label>

    <h3 class="seccion-titulo">Órdenes que se van a generar</h3>

    <asp:Label ID="lblSinProveedor" runat="server" CssClass="alert alert-warning" Visible="false" style="display: block;"></asp:Label>

    <asp:GridView ID="gvResumen" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="Marque los insumos que quiera pedir.">
        <Columns>
            <asp:BoundField DataField="Proveedor" HeaderText="Proveedor" />
            <asp:BoundField DataField="Insumos" HeaderText="Insumos" />
            <asp:BoundField DataField="ImporteTexto" HeaderText="Importe estimado" />
            <asp:BoundField DataField="Correo" HeaderText="Correo del proveedor" />
        </Columns>
    </asp:GridView>

    <div class="acciones">
        <asp:Button ID="btnAceptar" runat="server" Text="Generar las órdenes" CssClass="btn btn-primary"
            OnClick="btnAceptar_Click"
            OnClientClick="return confirm('Se va a generar una orden de compra Pendiente por cada proveedor, con los insumos marcados. ¿Continuar?');" ValidationGroup="Orden" />
        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
            OnClick="btnCancelar_Click" CausesValidation="false" />
    </div>

</asp:Content>
