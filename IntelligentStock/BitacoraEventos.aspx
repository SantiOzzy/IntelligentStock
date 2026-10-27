<%@ Page Title="Bitácora de eventos" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="BitacoraEventos.aspx.cs" Inherits="BitacoraEventos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Bitácora de eventos</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="panel panel-default">
        <div class="panel-heading"><strong>Filtros</strong></div>
        <div class="panel-body">
            <div class="row">
                <div class="col-md-2 form-group">
                    <asp:Label ID="lblDesde" runat="server" AssociatedControlID="txtDesde"
                        CssClass="control-label" Text="Fecha desde"></asp:Label>
                    <asp:TextBox ID="txtDesde" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                </div>
                <div class="col-md-2 form-group">
                    <asp:Label ID="lblHasta" runat="server" AssociatedControlID="txtHasta"
                        CssClass="control-label" Text="Fecha hasta"></asp:Label>
                    <asp:TextBox ID="txtHasta" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                </div>
                <div class="col-md-3 form-group">
                    <asp:Label ID="lblUsuario" runat="server" AssociatedControlID="ddlUsuario"
                        CssClass="control-label" Text="Usuario"></asp:Label>
                    <asp:DropDownList ID="ddlUsuario" runat="server" CssClass="form-control"></asp:DropDownList>
                </div>
                <div class="col-md-3 form-group">
                    <asp:Label ID="lblTexto" runat="server" AssociatedControlID="txtTexto"
                        CssClass="control-label" Text="Acción o descripción"></asp:Label>
                    <asp:TextBox ID="txtTexto" runat="server" CssClass="form-control"></asp:TextBox>
                </div>
                <div class="col-md-2 form-group">
                    <label class="control-label">&nbsp;</label><br />
                    <asp:Button ID="btnFiltrar" runat="server" Text="Filtrar"
                        CssClass="btn btn-primary" OnClick="btnFiltrar_Click" />
                    <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar"
                        CssClass="btn btn-default" OnClick="btnLimpiar_Click" CausesValidation="false" />
                </div>
            </div>
            <div class="row">
                <div class="col-md-2 form-group">
                    <asp:Label ID="lblCriticidad" runat="server" AssociatedControlID="ddlCriticidad"
                        CssClass="control-label" Text="Criticidad"></asp:Label>
                    <asp:DropDownList ID="ddlCriticidad" runat="server" CssClass="form-control">
                        <asp:ListItem Text="(todas)" Value=""></asp:ListItem>
                        <asp:ListItem Text="Informativa" Value="Informativa"></asp:ListItem>
                        <asp:ListItem Text="Advertencia" Value="Advertencia"></asp:ListItem>
                        <asp:ListItem Text="Grave" Value="Grave"></asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>
        </div>
    </div>

    <p><asp:Literal ID="litResumen" runat="server"></asp:Literal></p>

    <asp:GridView ID="gvEventos" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        AllowPaging="true" PageSize="25" OnPageIndexChanging="gvEventos_PageIndexChanging"
        EmptyDataText="No se registraron eventos para los filtros aplicados.">
        <Columns>
            <asp:BoundField DataField="IdEvento" HeaderText="Código" ItemStyle-Width="70px" />
            <asp:BoundField DataField="Usuario" HeaderText="Usuario" ItemStyle-Width="140px" />
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" ItemStyle-Width="90px" />
            <asp:BoundField DataField="Hora" HeaderText="Hora" ItemStyle-Width="80px" />
            <asp:BoundField DataField="Accion" HeaderText="Acción" ItemStyle-Width="200px" />
            <asp:BoundField DataField="Criticidad" HeaderText="Criticidad" ItemStyle-Width="110px" />
            <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
        </Columns>
        <PagerStyle CssClass="text-center" />
    </asp:GridView>

</asp:Content>
