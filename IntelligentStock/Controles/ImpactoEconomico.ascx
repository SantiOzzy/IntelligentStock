<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ImpactoEconomico.ascx.cs" Inherits="Controles_ImpactoEconomico" %>
<asp:Panel ID="pnlImpacto" runat="server" Visible="false" CssClass="impacto-economico">
    <asp:Label ID="lblTitulo" runat="server" CssClass="impacto-economico-titulo"></asp:Label>
    <asp:Label ID="lblMonto" runat="server" CssClass="impacto-economico-monto" Text="$0.00"></asp:Label>
    <asp:Label ID="lblDetalle" runat="server" CssClass="impacto-economico-detalle"></asp:Label>
</asp:Panel>
