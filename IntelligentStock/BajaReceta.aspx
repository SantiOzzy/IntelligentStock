<%@ Page Title="Baja de receta" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="BajaReceta.aspx.cs" Inherits="BajaReceta" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Baja de receta</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">La baja es lógica: la receta deja de poder venderse pero se conserva su detalle.</p>

    <div class="checkbox">
        <asp:CheckBox ID="chkMostrarInactivos" runat="server" AutoPostBack="true"
            Text="Mostrar también las dadas de baja (para reactivarlas)"
            OnCheckedChanged="chkMostrarInactivos_CheckedChanged" />
    </div>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false" DataKeyNames="IdReceta,Activo"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true" SelectedRowStyle-CssClass="info"
        OnSelectedIndexChanged="gvDetalle_SelectedIndexChanged"
        EmptyDataText="No hay recetas activas para dar de baja.">
        <Columns>
            <asp:BoundField DataField="IdReceta" HeaderText="Código" />
            <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
            <asp:BoundField DataField="CantidadInsumos" HeaderText="Cantidad de insumos" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:CommandField ShowSelectButton="true" SelectText="Seleccionar" ButtonType="Button"
                ControlStyle-CssClass="btn btn-xs btn-default" ItemStyle-Width="100px" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Dar de baja" CssClass="btn btn-danger"
        OnClick="btnAceptar_Click" CausesValidation="false"
        OnClientClick="return confirm('¿Confirma la baja de la receta seleccionada?');" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
