<%@ Page Title="Baja de proveedor" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="BajaProveedor.aspx.cs" Inherits="BajaProveedor" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Baja de proveedor</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">La baja es lógica: el proveedor deja de estar disponible pero se conservan sus órdenes de compra.</p>


    <div class="checkbox">
        <asp:CheckBox ID="chkMostrarInactivos" runat="server" AutoPostBack="true"
            Text="Mostrar también los dados de baja (para reactivarlos)"
            OnCheckedChanged="chkMostrarInactivos_CheckedChanged" />
    </div>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false" DataKeyNames="Cuit,Activo"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true" SelectedRowStyle-CssClass="info"
        OnSelectedIndexChanged="gvDetalle_SelectedIndexChanged"
        EmptyDataText="No hay proveedores activos para dar de baja.">
        <Columns>
            <asp:BoundField DataField="Cuit" HeaderText="CUIT" />
            <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
            <asp:BoundField DataField="RazonSocial" HeaderText="Razón social" />
            <asp:BoundField DataField="Correo" HeaderText="Correo" />
            <asp:BoundField DataField="Numero" HeaderText="Teléfono" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:CommandField ShowSelectButton="true" SelectText="Seleccionar" ButtonType="Button"
                ControlStyle-CssClass="btn btn-xs btn-default" ItemStyle-Width="100px" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Dar de baja" CssClass="btn btn-danger"
        OnClick="btnAceptar_Click" CausesValidation="false"
        OnClientClick="return confirm('¿Confirma la baja del proveedor seleccionado?');" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
