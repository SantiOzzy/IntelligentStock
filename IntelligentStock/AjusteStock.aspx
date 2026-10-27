<%@ Page Title="Registrar ajuste de stock" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="AjusteStock.aspx.cs" Inherits="AjusteStock" %>
<%@ Register Src="~/Controles/BuscadorInsumo.ascx" TagPrefix="uc" TagName="BuscadorInsumo" %>
<%@ Register Src="~/Controles/ImpactoEconomico.ascx" TagPrefix="uc" TagName="ImpactoEconomico" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Registrar ajuste de stock</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">El ajuste suma (incremento) o resta (decremento) la cantidad al stock físico del insumo.</p>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblInsumo" runat="server" CssClass="control-label" Text="Insumo (*)"></asp:Label>
                <uc:BuscadorInsumo ID="ucBuscadorInsumo" runat="server" />
            </div>
            <div class="form-group">
                <asp:Label ID="lblTipo" runat="server" AssociatedControlID="ddlTipo"
                    CssClass="control-label" Text="Tipo de ajuste (*)"></asp:Label>
                <asp:DropDownList ID="ddlTipo" runat="server" CssClass="form-control">
                    <asp:ListItem Text="Incremento" Value="Incremento" />
                    <asp:ListItem Text="Decremento" Value="Decremento" />
                </asp:DropDownList>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblCantidad" runat="server" AssociatedControlID="txtCantidad"
                    CssClass="control-label" Text="Cantidad (*)"></asp:Label>
                <asp:TextBox ID="txtCantidad" runat="server" CssClass="form-control" TextMode="Number" min="0.01" step="0.01"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvCantidad" runat="server" ControlToValidate="txtCantidad"
                    ValidationGroup="Ajuste" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="La cantidad es obligatoria."></asp:RequiredFieldValidator>
            </div>
            <div class="form-group">
                <asp:Label ID="lblMotivo" runat="server" AssociatedControlID="txtMotivo"
                    CssClass="control-label" Text="Motivo del ajuste (*)"></asp:Label>
                <asp:TextBox ID="txtMotivo" runat="server" CssClass="form-control"
                    TextMode="MultiLine" Rows="3" MaxLength="200"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvMotivo" runat="server" ControlToValidate="txtMotivo"
                    ValidationGroup="Ajuste" Display="Dynamic" CssClass="text-danger"
                    ErrorMessage="El motivo es obligatorio."></asp:RequiredFieldValidator>
            </div>
        </div>
    </div>

    <%-- Después de un decremento muestra la pérdida en pesos. --%>
    <uc:ImpactoEconomico ID="ucImpacto" runat="server" />

    <p><strong>Últimos ajustes registrados</strong></p>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="Todavía no se registraron ajustes.">
        <Columns>
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" />
            <asp:BoundField DataField="Insumo" HeaderText="Insumo" />
            <asp:BoundField DataField="Tipo" HeaderText="Tipo" />
            <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" />
            <asp:BoundField DataField="Usuario" HeaderText="Usuario" />
            <asp:BoundField DataField="Motivo" HeaderText="Motivo" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Registrar ajuste" CssClass="btn btn-primary"
        ValidationGroup="Ajuste" OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
