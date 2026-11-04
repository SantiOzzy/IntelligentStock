<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EstadoSensorIoT.ascx.cs" Inherits="Controles_EstadoSensorIoT" %>
<div class="tarjeta-sensor">
    <div class="tarjeta-sensor-titulo">Sensor IoT</div>
    <dl class="tarjeta-sensor-datos">
        <dt>Modelo:</dt>
        <dd><asp:Label ID="lblModelo" runat="server"></asp:Label></dd>
        <dt>MAC:</dt>
        <dd><asp:Label ID="lblMacAddress" runat="server"></asp:Label></dd>
        <dt>Temp. límite:</dt>
        <dd><asp:Label ID="lblTempMaxima" runat="server" CssClass="tarjeta-sensor-limite"></asp:Label></dd>
        <dt>Estado:</dt>
        <dd><asp:Label ID="lblEstado" runat="server"></asp:Label></dd>
    </dl>
</div>
