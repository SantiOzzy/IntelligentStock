<%@ Page Title="Consulta de recetas" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ConsultaReceta.aspx.cs" Inherits="ConsultaReceta" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Consulta de recetas</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblBusqueda" runat="server" AssociatedControlID="txtBusqueda"
                    CssClass="control-label" Text="Código o nombre de la receta"></asp:Label>
                <asp:TextBox ID="txtBusqueda" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblEstado" runat="server" AssociatedControlID="ddlEstado"
                    CssClass="control-label" Text="Estado"></asp:Label>
                <asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-control">
                    <asp:ListItem Text="(todos)" Value="" />
                    <asp:ListItem Text="Activa" Value="Activa" />
                    <asp:ListItem Text="Inactiva" Value="Inactiva" />
                </asp:DropDownList>
            </div>
        </div>
    </div>

    <p><asp:Literal ID="litResumen" runat="server"></asp:Literal></p>

    <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        ShowHeaderWhenEmpty="true"
        EmptyDataText="No hay registros para mostrar.">
        <Columns>
            <asp:BoundField DataField="IdReceta" HeaderText="Código" />
            <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
            <asp:BoundField DataField="CantidadInsumos" HeaderText="Cantidad de insumos" />
            <asp:TemplateField HeaderText="Insumos">
                <ItemTemplate><%# InsumosDeLaReceta(Eval("IdReceta")) %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
        </Columns>
    </asp:GridView>

    <asp:Button ID="btnAceptar" runat="server" Text="Buscar" CssClass="btn btn-primary"
        OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
