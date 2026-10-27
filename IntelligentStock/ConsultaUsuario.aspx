<%@ Page Title="Consulta de usuarios" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ConsultaUsuario.aspx.cs" Inherits="ConsultaUsuario" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Consulta de usuarios</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="panel panel-default">
        <div class="panel-heading"><strong>Filtros</strong></div>
        <div class="panel-body">
            <div class="row">
                <div class="col-md-4 form-group">
                    <asp:Label ID="lblBusqueda" runat="server" AssociatedControlID="txtBusqueda"
                        CssClass="control-label" Text="DNI, usuario, nombre o correo"></asp:Label>
                    <asp:TextBox ID="txtBusqueda" runat="server" CssClass="form-control"></asp:TextBox>
                </div>
                <div class="col-md-3 form-group">
                    <asp:Label ID="lblRol" runat="server" AssociatedControlID="ddlRol"
                        CssClass="control-label" Text="Rol"></asp:Label>
                    <asp:DropDownList ID="ddlRol" runat="server" CssClass="form-control"></asp:DropDownList>
                </div>
                <div class="col-md-3 form-group">
                    <asp:Label ID="lblEstado" runat="server" AssociatedControlID="ddlEstado"
                        CssClass="control-label" Text="Estado"></asp:Label>
                    <asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-control">
                        <asp:ListItem Text="(todos)" Value="" />
                        <asp:ListItem Text="Activo" Value="Activo" />
                        <asp:ListItem Text="Bloqueado" Value="Bloqueado" />
                        <asp:ListItem Text="Inactivo" Value="Inactivo" />
                    </asp:DropDownList>
                </div>
                <div class="col-md-2 form-group">
                    <label class="control-label">&nbsp;</label><br />
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar" CssClass="btn btn-primary"
                        OnClick="btnBuscar_Click" />
                    <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar" CssClass="btn btn-default"
                        OnClick="btnLimpiar_Click" CausesValidation="false" />
                </div>
            </div>
        </div>
    </div>

    <p><asp:Literal ID="litResumen" runat="server"></asp:Literal></p>

    <asp:GridView ID="gvUsuarios" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        EmptyDataText="No se encontraron usuarios.">
        <Columns>
            <asp:BoundField DataField="DNI" HeaderText="DNI" />
            <asp:BoundField DataField="NombreUsuario" HeaderText="Usuario" />
            <asp:BoundField DataField="NombreCompleto" HeaderText="Apellido y nombre" />
            <asp:BoundField DataField="CorreoElectronico" HeaderText="Correo" />
            <asp:BoundField DataField="NumeroTelefono" HeaderText="Teléfono" />
            <asp:BoundField DataField="NombreRol" HeaderText="Rol" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:BoundField DataField="IntentosFallidos" HeaderText="Intentos fallidos" />
        </Columns>
    </asp:GridView>

</asp:Content>
