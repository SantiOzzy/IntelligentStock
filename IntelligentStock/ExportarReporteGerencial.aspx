<%@ Page Title="Exportar reporte gerencial" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ExportarReporteGerencial.aspx.cs" Inherits="ExportarReporteGerencial" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Exportar reporte gerencial</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <p class="help-block">El archivo se genera en el servidor (carpeta App_Data\Reportes) y se descarga al finalizar. El período no puede superar un año.</p>

    <div class="row">
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblReporte" runat="server" AssociatedControlID="ddlReporte"
                    CssClass="control-label" Text="Tipo de reporte (*)"></asp:Label>
                <asp:DropDownList ID="ddlReporte" runat="server" CssClass="form-control">
                    <asp:ListItem Text="Consumo por insumo" Value="Consumo por insumo" />
                    <asp:ListItem Text="Ventas por receta" Value="Ventas por receta" />
                    <asp:ListItem Text="Órdenes por proveedor" Value="Órdenes por proveedor" />
                    <asp:ListItem Text="Alertas térmicas" Value="Alertas térmicas" />
                    <asp:ListItem Text="Ajustes de stock" Value="Ajustes de stock" />
                </asp:DropDownList>
            </div>
            <div class="form-group">
                <asp:Label ID="lblDesde" runat="server" AssociatedControlID="txtDesde"
                    CssClass="control-label" Text="Fecha desde (*)"></asp:Label>
                <asp:TextBox ID="txtDesde" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-group">
                <asp:Label ID="lblHasta" runat="server" AssociatedControlID="txtHasta"
                    CssClass="control-label" Text="Fecha hasta (*)"></asp:Label>
                <asp:TextBox ID="txtHasta" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
            </div>
            <div class="form-group">
                <asp:Label ID="lblFormato" runat="server" AssociatedControlID="ddlFormato"
                    CssClass="control-label" Text="Formato"></asp:Label>
                <asp:DropDownList ID="ddlFormato" runat="server" CssClass="form-control">
                    <asp:ListItem Text="PDF" Value="PDF" />
                    <asp:ListItem Text="Excel" Value="Excel" />
                    <asp:ListItem Text="CSV" Value="CSV" />
                </asp:DropDownList>
            </div>
        </div>
    </div>

    <asp:Button ID="btnAceptar" runat="server" Text="Exportar" CssClass="btn btn-primary"
        OnClick="btnAceptar_Click" />
    <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
        OnClick="btnCancelar_Click" CausesValidation="false" />

</asp:Content>
