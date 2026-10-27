<%@ Page Title="Consulta de patentes" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ConsultaPatente.aspx.cs" Inherits="ConsultaPatente" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Consulta de patentes</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p>
        Cada patente habilita un formulario del sistema. La columna
        <em>Familias</em> muestra en qué agrupaciones está incluida y la columna
        <em>Roles</em> qué perfiles terminan teniéndola.
    </p>

    <div class="row">
        <div class="col-md-4 form-group">
            <asp:Label ID="lblBusqueda" runat="server" AssociatedControlID="txtBusqueda"
                CssClass="control-label" Text="Buscar por nombre o Id"></asp:Label>
            <div class="input-group">
                <asp:TextBox ID="txtBusqueda" runat="server" CssClass="form-control"></asp:TextBox>
                <span class="input-group-btn">
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar" CssClass="btn btn-primary"
                        OnClick="btnBuscar_Click" />
                </span>
            </div>
        </div>
    </div>

    <asp:GridView ID="gvPatentes" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        EmptyDataText="No se encontraron patentes.">
        <Columns>
            <asp:BoundField DataField="Id" HeaderText="Id" ItemStyle-Width="60px" />
            <asp:BoundField DataField="Nombre" HeaderText="Patente" />
            <asp:BoundField DataField="Descripcion" HeaderText="Funcionalidad" />
            <asp:BoundField DataField="Familias" HeaderText="Familias que la contienen" />
            <asp:BoundField DataField="Roles" HeaderText="Roles que la tienen" />
        </Columns>
    </asp:GridView>

</asp:Content>
