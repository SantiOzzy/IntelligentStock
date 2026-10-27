<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ImpactoEconomico.ascx.cs" Inherits="Controles_ImpactoEconomico" %>
<asp:Panel ID="pnlImpacto" runat="server" Visible="false" style="border-radius: 5px; padding: 15px; text-align: center; max-width: 250px; border: 1px solid; margin-bottom: 15px;">
    <asp:Label ID="lblTitulo" runat="server" Font-Bold="true" Font-Size="14px"></asp:Label>
    <br />
    <asp:Label ID="lblMonto" runat="server" Font-Size="X-Large" Font-Bold="true" Text="$0.00"></asp:Label>
    <br />
    <asp:Label ID="lblDetalle" runat="server" Font-Size="X-Small"></asp:Label>
</asp:Panel>
