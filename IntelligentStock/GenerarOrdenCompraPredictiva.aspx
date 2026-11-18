<%@ Page Title="Generar orden de compra" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="GenerarOrdenCompraPredictiva.aspx.cs" Inherits="GenerarOrdenCompraPredictiva" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Generar orden de compra</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">El sistema estima con ML.NET cuánto se va a consumir de cada insumo en los próximos días, a partir de las ventas registradas, y propone lo que hace falta pedir (los perecederos, solo para los días que duran). Como cada proveedor vende lo suyo, se arma una orden por proveedor: al elegirlo se marcan los insumos que se le pidieron la última vez. Marque o desmarque los que quiera pedirle, ajuste las cantidades si hace falta y genere la orden; lo que no se pida sigue en la propuesta para el próximo proveedor.</p>

    <asp:Literal ID="litIndicadores" runat="server" Mode="PassThrough"></asp:Literal>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblProveedor" runat="server" AssociatedControlID="ddlProveedor"
                    CssClass="control-label" Text="Proveedor (*)"></asp:Label>
                <asp:DropDownList ID="ddlProveedor" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnSelectedIndexChanged="ddlProveedor_SelectedIndexChanged">
                    <asp:ListItem Text="(seleccione)" Value="" />
                </asp:DropDownList>
                <asp:RequiredFieldValidator ID="rfvProveedor" runat="server" ControlToValidate="ddlProveedor"
                    ValidationGroup="Orden" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Elija el proveedor."></asp:RequiredFieldValidator>
                <span class="campo-ayuda">Solo figuran los proveedores activos.</span>
            </div>
            <div class="checkbox">
                <asp:CheckBox ID="chkEnviarCorreo" runat="server" Checked="true"
                    Text="Enviarle la orden por correo al proveedor (con la orden en PDF)" />
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

    <asp:Label ID="lblAvisoPrediccion" runat="server" CssClass="alert alert-warning" Visible="false" style="display: block;"></asp:Label>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false" DataKeyNames="IdInsumo"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="No hay cantidades sugeridas para mostrar.">
        <Columns>
            <asp:TemplateField HeaderText="Pedir" ItemStyle-HorizontalAlign="Center" ItemStyle-Width="50px">
                <ItemTemplate>
                    <asp:CheckBox ID="chkIncluir" runat="server" />
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
            <asp:BoundField DataField="ProveedorHabitualTexto" HeaderText="Se le pidió a" />
            <asp:BoundField DataField="StockActualTexto" HeaderText="Stock actual" />
            <asp:BoundField DataField="StockMinimoTexto" HeaderText="Stock mínimo" />
            <asp:BoundField DataField="ConsumoPrevistoTexto" HeaderText="Consumo previsto" />
            <asp:BoundField DataField="DiasCubiertosTexto" HeaderText="Para" />
            <asp:BoundField DataField="CantidadPendienteTexto" HeaderText="Ya pedido" />
            <asp:BoundField DataField="CantidadSugeridaTexto" HeaderText="Sugerido" ItemStyle-Font-Bold="true" />
            <asp:TemplateField HeaderText="A pedir" ItemStyle-Width="150px">
                <ItemTemplate>
                    <div class="input-group input-group-sm">
                        <asp:TextBox ID="txtCantidadPedir" runat="server" CssClass="form-control" TextMode="Number"
                            min="0.01" step="0.01" max="999999.99"
                            Text='<%# Numeros.ParaInput((decimal)Eval("CantidadAPedir")) %>'></asp:TextBox>
                        <span class="input-group-addon"><%# HttpUtility.HtmlEncode(Eval("UnidadDeMedida")) %></span>
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="ImporteEstimadoTexto" HeaderText="Importe estimado" />
        </Columns>
    </asp:GridView>

    <asp:Label ID="lblPrediccion" runat="server" CssClass="help-block" Visible="false"></asp:Label>

    <div class="acciones">
        <asp:Button ID="btnAceptar" runat="server" Text="Generar orden" CssClass="btn btn-primary"
            OnClick="btnAceptar_Click"
            OnClientClick="return confirm('Se va a generar una orden de compra Pendiente para el proveedor elegido con los insumos marcados. ¿Continuar?');" ValidationGroup="Orden" />
        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
            OnClick="btnCancelar_Click" CausesValidation="false" />
    </div>

</asp:Content>
