<%@ Page Title="Anular orden de compra" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="AnularOrdenCompra.aspx.cs" Inherits="AnularOrdenCompra" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Anular orden de compra</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">Solo se pueden anular órdenes en estado Pendiente. La orden anulada deja de contar como pedido en camino para la próxima orden predictiva.</p>

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
                    ValidationGroup="Anular" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Elija la orden de compra."></asp:RequiredFieldValidator>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblMotivo" runat="server" AssociatedControlID="txtMotivo"
                    CssClass="control-label" Text="Motivo de la anulación (*)"></asp:Label>
                <asp:TextBox ID="txtMotivo" runat="server" CssClass="form-control"
                    TextMode="MultiLine" Rows="3" MaxLength="200"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvMotivo" runat="server" ControlToValidate="txtMotivo"
                    ValidationGroup="Anular" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="Ingrese el motivo de la anulación."></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="revLargoMotivo" runat="server" ControlToValidate="txtMotivo"
                    ValidationExpression="^[\s\S]{5,200}$" ValidationGroup="Anular" Display="Dynamic"
                    CssClass="text-danger" ErrorMessage="El motivo tiene que tener entre 5 y 200 caracteres."></asp:RegularExpressionValidator>
                <span class="campo-ayuda">Entre 5 y 200 caracteres. Queda registrado en la orden y en la bitácora.</span>
            </div>
        </div>
    </div>

    <asp:Literal ID="litIndicadores" runat="server" Mode="PassThrough"></asp:Literal>

    <h3 class="seccion-titulo">Insumos de la orden</h3>

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

    <div class="acciones">
        <asp:Button ID="btnAceptar" runat="server" Text="Anular orden" CssClass="btn btn-danger"
            OnClick="btnAceptar_Click"
            OnClientClick="return confirm('La orden elegida va a quedar Anulada y ya no se va a poder recibir. ¿Continuar?');" ValidationGroup="Anular" />
        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
            OnClick="btnCancelar_Click" CausesValidation="false" />
    </div>

</asp:Content>
