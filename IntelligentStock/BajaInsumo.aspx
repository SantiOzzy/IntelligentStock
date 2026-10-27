<%@ Page Title="Baja de insumo" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="BajaInsumo.aspx.cs" Inherits="BajaInsumo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Baja de insumo</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">La baja es lógica. No se puede dar de baja un insumo que forme parte de una receta activa o de una orden de compra pendiente.</p>


    <div class="checkbox">
        <asp:CheckBox ID="chkMostrarInactivos" runat="server" AutoPostBack="true"
            Text="Mostrar también los dados de baja (para reactivarlos)"
            OnCheckedChanged="chkMostrarInactivos_CheckedChanged" />
    </div>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false" DataKeyNames="IdInsumo,Activo"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true" SelectedRowStyle-CssClass="info"
        OnSelectedIndexChanged="gvDetalle_SelectedIndexChanged"
        EmptyDataText="No hay insumos activos para dar de baja.">
        <Columns>
            <asp:BoundField DataField="IdInsumo" HeaderText="Código" />
            <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
            <asp:BoundField DataField="UnidadDeMedida" HeaderText="Unidad" />
            <asp:BoundField DataField="StockValor" HeaderText="Stock" />
            <asp:BoundField DataField="StockMinimoValor" HeaderText="Stock mínimo" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:CommandField ShowSelectButton="true" SelectText="Seleccionar" ButtonType="Button"
                ControlStyle-CssClass="btn btn-xs btn-default" ItemStyle-Width="100px" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Dar de baja" CssClass="btn btn-danger"
        OnClick="btnAceptar_Click" CausesValidation="false"
        OnClientClick="return confirm('¿Confirma la baja del insumo seleccionado?');" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
