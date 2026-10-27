<%@ Page Title="Consulta de insumos" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ConsultaInsumo.aspx.cs" Inherits="ConsultaInsumo" %>
<%@ Register Src="~/Controles/IndicadorStock.ascx" TagPrefix="uc" TagName="IndicadorStock" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Consulta de insumos</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblBusqueda" runat="server" AssociatedControlID="txtBusqueda"
                    CssClass="control-label" Text="Código, nombre o descripción"></asp:Label>
                <asp:TextBox ID="txtBusqueda" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
            </div>
            <div class="form-group">
                <asp:Label ID="lblEstado" runat="server" AssociatedControlID="ddlEstado"
                    CssClass="control-label" Text="Estado"></asp:Label>
                <asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-control">
                    <asp:ListItem Text="(todos)" Value="" />
                    <asp:ListItem Text="Activo" Value="Activo" />
                    <asp:ListItem Text="Inactivo" Value="Inactivo" />
                </asp:DropDownList>
            </div>
        </div>
        <div class="col-md-6">
            <div class="checkbox">
                <label>
                    <asp:CheckBox ID="chkBajoMinimo" runat="server" Text="Solo insumos en su stock mínimo o por debajo" />
                </label>
            </div>
        </div>
    </div>

    <p><asp:Literal ID="litResumen" runat="server"></asp:Literal></p>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="No hay registros para mostrar.">
        <Columns>
            <asp:BoundField DataField="IdInsumo" HeaderText="Código" />
            <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
            <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
            <asp:BoundField DataField="UnidadDeMedida" HeaderText="Unidad" />
            <asp:TemplateField HeaderText="Stock">
                <ItemTemplate>
                    <uc:IndicadorStock ID="ucIndicadorStock" runat="server"
                        StockActual='<%# Eval("Stock") %>'
                        StockMinimo='<%# Eval("StockMinimo") %>'
                        UnidadMedida='<%# Eval("UnidadDeMedida") %>' />
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="StockMinimoValor" HeaderText="Stock mínimo" />
            <asp:BoundField DataField="PrecioTexto" HeaderText="Precio unitario" />
            <asp:BoundField DataField="VidaUtilTexto" HeaderText="Vida útil" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Buscar" CssClass="btn btn-primary"
        OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
