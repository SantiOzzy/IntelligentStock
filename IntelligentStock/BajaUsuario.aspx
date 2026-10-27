<%@ Page Title="Baja de usuario" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="BajaUsuario.aspx.cs" Inherits="BajaUsuario" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Baja de usuario</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">
        La baja es lógica: el usuario deja de poder iniciar sesión pero el registro
        se conserva para no perder la trazabilidad de la bitácora.
    </p>

    <div class="checkbox">
        <asp:CheckBox ID="chkMostrarInactivos" runat="server" AutoPostBack="true"
            Text="Mostrar también los usuarios dados de baja (para reactivarlos)"
            OnCheckedChanged="chkMostrarInactivos_CheckedChanged" />
    </div>

    <asp:GridView ID="gvUsuarios" runat="server" AutoGenerateColumns="false" DataKeyNames="DNI,Activo"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true" SelectedRowStyle-CssClass="info"
        OnSelectedIndexChanged="gvUsuarios_SelectedIndexChanged"
        EmptyDataText="No hay usuarios en condiciones de ser dados de baja.">
        <Columns>
            <asp:BoundField DataField="DNI" HeaderText="DNI" />
            <asp:BoundField DataField="NombreUsuario" HeaderText="Usuario" />
            <asp:BoundField DataField="NombreCompleto" HeaderText="Apellido y nombre" />
            <asp:BoundField DataField="NombreRol" HeaderText="Rol" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:CommandField ShowSelectButton="true" SelectText="Seleccionar" ButtonType="Button"
                ControlStyle-CssClass="btn btn-xs btn-default" ItemStyle-Width="100px" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnDarBaja" runat="server" Text="Dar de baja" CssClass="btn btn-danger"
        OnClick="btnDarBaja_Click" CausesValidation="false"
        OnClientClick="return confirm('¿Confirma la baja del usuario seleccionado?');" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
