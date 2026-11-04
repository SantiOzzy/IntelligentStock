<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PermisosDelPerfil.ascx.cs" Inherits="Controles_PermisosDelPerfil" %>
<asp:Label ID="lblAviso" runat="server" CssClass="perfil-aviso" Visible="false"></asp:Label>
<asp:Panel ID="pnlPerfil" runat="server" CssClass="perfil-permisos" Visible="false">

    <h4 class="perfil-titulo">Familias del perfil <span class="badge"><asp:Literal ID="litCantidadFamilias" runat="server"></asp:Literal></span></h4>
    <asp:PlaceHolder ID="phSinFamilias" runat="server">
        <p class="perfil-aviso">El perfil no pertenece a ninguna familia.</p>
    </asp:PlaceHolder>
    <asp:Repeater ID="rptFamilias" runat="server" OnItemDataBound="rptFamilias_ItemDataBound">
        <ItemTemplate>
            <div class="perfil-familia">
                <div class="perfil-familia-nombre">
                    <span class="label label-primary">Familia</span>
                    <strong><%#: Eval("Nombre") %></strong>
                </div>
                <asp:PlaceHolder ID="phSubfamilias" runat="server">
                    <div class="campo-ayuda">Incluye las familias: <span><asp:Literal ID="litSubfamilias" runat="server"></asp:Literal></span></div>
                </asp:PlaceHolder>
                <div class="perfil-familia-patentes">
                    <asp:Repeater ID="rptPatentes" runat="server">
                        <ItemTemplate><span class="label label-default perfil-patente"><%#: Eval("Nombre") %></span></ItemTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </ItemTemplate>
    </asp:Repeater>

    <h4 class="perfil-titulo">Permisos particulares <span class="badge"><asp:Literal ID="litCantidadParticulares" runat="server"></asp:Literal></span></h4>
    <p class="campo-ayuda">Patentes asignadas directamente al perfil, fuera de cualquier familia.</p>
    <asp:PlaceHolder ID="phSinParticulares" runat="server">
        <p class="perfil-aviso">Ninguno: todos los permisos del perfil vienen de sus familias.</p>
    </asp:PlaceHolder>
    <div class="perfil-familia-patentes">
        <asp:Repeater ID="rptParticulares" runat="server">
            <ItemTemplate><span class="label label-default perfil-patente"><%#: Eval("Nombre") %></span></ItemTemplate>
        </asp:Repeater>
    </div>

    <p class="perfil-total">Patentes que habilita el perfil en total: <strong><asp:Literal ID="litTotal" runat="server"></asp:Literal></strong></p>
</asp:Panel>
