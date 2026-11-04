<%@ Control Language="C#" AutoEventWireup="true" CodeFile="IndicadorStock.ascx.cs" Inherits="Controles_IndicadorStock" %>
<asp:Panel ID="pnlFondo" runat="server" CssClass="indicador-stock">
    <span class="indicador-stock-cantidad"><asp:Label ID="lblCantidad" runat="server"></asp:Label><asp:Label ID="lblUnidad" runat="server" CssClass="indicador-stock-unidad"></asp:Label></span>
    <asp:Label ID="lblEstado" runat="server" CssClass="indicador-stock-estado"></asp:Label>
</asp:Panel>
