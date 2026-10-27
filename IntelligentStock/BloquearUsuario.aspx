<%@ Page Title="Bloquear usuario" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="BloquearUsuario.aspx.cs" Inherits="BloquearUsuario" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Bloquear usuario</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">Se listan los usuarios activos que no bloqueó un administrador (salvo la propia cuenta). Elija uno y presione Bloquear. Si el usuario tiene la sesión abierta, se le cierra. Un bloqueo hecho desde acá no se puede levantar con "¿Olvidaste tu contraseña?": solo lo levanta un administrador desde Desbloquear usuario. Por eso también aparecen las cuentas bloqueadas por intentos fallidos: al bloquearlas acá, el bloqueo pasa a ser manual.</p>

    <asp:GridView ID="gvUsuarios" runat="server" AutoGenerateColumns="false" DataKeyNames="DNI"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true" SelectedRowStyle-CssClass="info"
        OnSelectedIndexChanged="gvUsuarios_SelectedIndexChanged"
        EmptyDataText="No hay usuarios en condiciones de ser bloqueados.">
        <Columns>
            <asp:BoundField DataField="DNI" HeaderText="DNI" />
            <asp:BoundField DataField="NombreUsuario" HeaderText="Usuario" />
            <asp:BoundField DataField="NombreCompleto" HeaderText="Apellido y nombre" />
            <asp:BoundField DataField="NombreRol" HeaderText="Rol" />
            <asp:BoundField DataField="IntentosFallidos" HeaderText="Intentos fallidos" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:CommandField ShowSelectButton="true" SelectText="Seleccionar" ButtonType="Button"
                ControlStyle-CssClass="btn btn-xs btn-default" ItemStyle-Width="100px" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnBloquear" runat="server" Text="Bloquear" CssClass="btn btn-warning"
        OnClick="btnBloquear_Click" CausesValidation="false"
        OnClientClick="return confirm('¿Confirma el bloqueo del usuario seleccionado?');" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
