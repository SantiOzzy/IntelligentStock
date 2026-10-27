<%@ Page Title="Consulta de familias" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ConsultaFamilia.aspx.cs" Inherits="ConsultaFamilia" %>
<%@ Import Namespace="BE" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Consulta de familias</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">

        <div class="col-md-6">
            <div class="panel panel-default">
                <div class="panel-heading"><strong>Estructura de permisos</strong></div>
                <div class="panel-body">
                    <p class="help-block">
                        Se muestran las familias y patentes que no dependen de ninguna otra,
                        con todo lo que contienen por debajo.
                    </p>
                    <asp:TreeView ID="tvFamilias" runat="server" ShowLines="true"
                        ExpandDepth="1" NodeIndent="18" SelectAction="None"></asp:TreeView>
                </div>
            </div>
        </div>

        <div class="col-md-6">
            <div class="panel panel-default">
                <div class="panel-heading"><strong>Permisos por rol</strong></div>
                <div class="panel-body">
                    <div class="form-group">
                        <asp:DropDownList ID="ddlRoles" runat="server" CssClass="form-control"
                            AutoPostBack="true" OnSelectedIndexChanged="ddlRoles_SelectedIndexChanged"></asp:DropDownList>
                    </div>
                    <p><strong>Estructura asignada</strong></p>
                    <asp:Literal ID="litArbolRol" runat="server" Mode="PassThrough"></asp:Literal>
                    <hr />
                    <p><strong>Patentes efectivas</strong></p>
                    <asp:Literal ID="litPatentesRol" runat="server" Mode="PassThrough"></asp:Literal>
                </div>
            </div>

            <div class="panel panel-default">
                <div class="panel-heading"><strong>Consultar una familia</strong></div>
                <div class="panel-body">
                    <div class="form-group">
                        <asp:DropDownList ID="ddlFamiliasConsulta" runat="server" CssClass="form-control"
                            AutoPostBack="true" OnSelectedIndexChanged="ddlFamiliasConsulta_SelectedIndexChanged"></asp:DropDownList>
                    </div>
                    <p><strong>Estructura de la familia</strong></p>
                    <asp:Literal ID="litArbolFamilia" runat="server" Mode="PassThrough"></asp:Literal>
                    <hr />
                    <p><strong>Patentes efectivas</strong></p>
                    <asp:Literal ID="litPatentesFamilia" runat="server" Mode="PassThrough"></asp:Literal>
                </div>
            </div>
        </div>

    </div>

    <div class="panel panel-default">
        <div class="panel-heading"><strong>Listado de componentes</strong></div>
        <div class="panel-body">
            <asp:GridView ID="gvComponentes" runat="server" AutoGenerateColumns="false"
                CssClass="table table-bordered table-condensed table-striped" GridLines="None"
                EmptyDataText="No hay componentes cargados.">
                <Columns>
                    <asp:BoundField DataField="Id" HeaderText="Id" ItemStyle-Width="60px" />
                    <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
                    <asp:TemplateField HeaderText="Tipo" ItemStyle-Width="80px">
                        <ItemTemplate><%# ((Componente)Container.DataItem).Tipo == 'F' ? "Familia" : "Patente" %></ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </div>

</asp:Content>
