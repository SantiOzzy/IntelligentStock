<%@ Page Title="Anular orden de compra" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="AnularOrdenCompra.aspx.cs" Inherits="AnularOrdenCompra" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Anular orden de compra</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">Solo se pueden anular órdenes en estado Pendiente.</p>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblOrden" runat="server" AssociatedControlID="ddlOrden"
                    CssClass="control-label" Text="Orden de compra (*)"></asp:Label>
                <asp:DropDownList ID="ddlOrden" runat="server" CssClass="form-control"
                    AutoPostBack="true" OnSelectedIndexChanged="ddlOrden_SelectedIndexChanged">
                    <asp:ListItem Text="(seleccione)" Value="" />
                </asp:DropDownList>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblMotivo" runat="server" AssociatedControlID="txtMotivo"
                    CssClass="control-label" Text="Motivo de la anulación (*)"></asp:Label>
                <asp:TextBox ID="txtMotivo" runat="server" CssClass="form-control"
                    TextMode="MultiLine" Rows="3" MaxLength="200"></asp:TextBox>
            </div>
        </div>
    </div>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="Elija una orden para ver sus insumos.">
        <Columns>
            <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
            <asp:BoundField DataField="CantidadTexto" HeaderText="Cantidad" />
            <asp:BoundField DataField="ImporteTexto" HeaderText="Importe estimado" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Anular orden" CssClass="btn btn-primary"
        OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
