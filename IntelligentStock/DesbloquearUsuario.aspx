<%@ Page Title="Desbloquear usuario" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="DesbloquearUsuario.aspx.cs" Inherits="DesbloquearUsuario" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Desbloquear usuario</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">Al desbloquear también se pone en cero el contador de intentos fallidos, para que el usuario no vuelva a quedar bloqueado al primer error.</p>

    <asp:GridView ID="gvUsuarios" runat="server" AutoGenerateColumns="false" DataKeyNames="DNI"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true" SelectedRowStyle-CssClass="info"
        OnSelectedIndexChanged="gvUsuarios_SelectedIndexChanged"
        EmptyDataText="No hay usuarios bloqueados.">
        <Columns>
            <asp:BoundField DataField="DNI" HeaderText="DNI" />
            <asp:BoundField DataField="NombreUsuario" HeaderText="Usuario" />
            <asp:BoundField DataField="NombreCompleto" HeaderText="Apellido y nombre" />
            <asp:BoundField DataField="NombreRol" HeaderText="Rol" />
            <asp:BoundField DataField="MotivoBloqueo" HeaderText="Motivo del bloqueo" />
            <asp:CommandField ShowSelectButton="true" SelectText="Seleccionar" ButtonType="Button"
                ControlStyle-CssClass="btn btn-xs btn-default" ItemStyle-Width="100px" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnDesbloquear" runat="server" Text="Desbloquear" CssClass="btn btn-success"
        OnClick="btnDesbloquear_Click" CausesValidation="false"
        OnClientClick="return confirm('¿Confirma el desbloqueo del usuario seleccionado?');" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
