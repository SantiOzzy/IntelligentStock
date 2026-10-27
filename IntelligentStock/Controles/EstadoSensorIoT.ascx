<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EstadoSensorIoT.ascx.cs" Inherits="Controles_EstadoSensorIoT" %>
<div style="border: 2px solid #0056b3; border-radius: 8px; padding: 15px; width: 250px; background-color: #f4f8ff; margin-bottom: 15px;">
    <h4 style="margin-top: 0; color: #0056b3;">Sensor IoT</h4>
    <hr style="margin: 8px 0;" />
    <strong>Modelo:</strong> <asp:Label ID="lblModelo" runat="server"></asp:Label><br />
    <strong>MAC:</strong> <asp:Label ID="lblMacAddress" runat="server"></asp:Label><br />
    <strong>Temp. límite:</strong> <asp:Label ID="lblTempMaxima" runat="server" ForeColor="Red"></asp:Label><br />
    <strong>Estado:</strong> <asp:Label ID="lblEstado" runat="server"></asp:Label>
</div>
