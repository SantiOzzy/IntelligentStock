<%@ Control Language="C#" AutoEventWireup="true" CodeFile="IndicadorStock.ascx.cs" Inherits="Controles_IndicadorStock" %>
<asp:Panel ID="pnlFondo" runat="server" style="padding: 5px 10px; border-radius: 12px; display: inline-block; text-align: center; min-width: 120px;">
    <asp:Label ID="lblCantidad" runat="server" Font-Bold="true" Font-Size="Medium"></asp:Label>
    <asp:Label ID="lblUnidad" runat="server" Font-Size="Small"></asp:Label>
    <br />
    <asp:Label ID="lblEstado" runat="server" Font-Size="X-Small" Font-Bold="true"></asp:Label>
</asp:Panel>
