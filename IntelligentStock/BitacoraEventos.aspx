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
                    <asp:RangeValidator ID="rvDesde" runat="server" ControlToValidate="txtDesde"
                        Type="Date" MinimumValue="2000-01-01" MaximumValue="2099-12-31" ValidationGroup="Filtro"
                        Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="Ingrese una fecha entre el 01/01/2000 y el 31/12/2099."></asp:RangeValidator>
                </div>
                <div class="col-md-2 form-group">
                    <asp:Label ID="lblHasta" runat="server" AssociatedControlID="txtHasta"
                        CssClass="control-label" Text="Fecha hasta"></asp:Label>
                    <asp:TextBox ID="txtHasta" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                    <asp:CompareValidator ID="cvHasta" runat="server" ControlToValidate="txtHasta"
                        ControlToCompare="txtDesde" Operator="GreaterThanEqual" Type="Date"
                        ValidationGroup="Filtro" Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="La fecha hasta no puede ser anterior a la fecha desde."></asp:CompareValidator>
                    <asp:RangeValidator ID="rvHasta" runat="server" ControlToValidate="txtHasta"
                        Type="Date" MinimumValue="2000-01-01" MaximumValue="2099-12-31" ValidationGroup="Filtro"
                        Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="Ingrese una fecha entre el 01/01/2000 y el 31/12/2099."></asp:RangeValidator>
                </div>
                <div class="col-md-3 form-group">
                    <asp:Label ID="lblUsuario" runat="server" AssociatedControlID="ddlUsuario"
                        CssClass="control-label" Text="Usuario"></asp:Label>
                    <asp:DropDownList ID="ddlUsuario" runat="server" CssClass="form-control"></asp:DropDownList>
                </div>
                <div class="col-md-3 form-group">
                    <asp:Label ID="lblTexto" runat="server" AssociatedControlID="txtTexto"
                        CssClass="control-label" Text="Acción o descripción"></asp:Label>
                    <asp:TextBox ID="txtTexto" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                </div>
                <div class="col-md-2 form-group">
                    <label class="control-label">&nbsp;</label><br />
                    <asp:Button ID="btnFiltrar" runat="server" Text="Filtrar"
                        CssClass="btn btn-primary" OnClick="btnFiltrar_Click" ValidationGroup="Filtro" />
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
                <div class="col-md-6 form-group">
                    <label class="control-label">&nbsp;</label>
                    <div class="checkbox">
                        <label>
                            <asp:CheckBox ID="chkIncluirEliminados" runat="server" Text="Incluir eventos eliminados" />
                        </label>
                    </div>
                </div>
            </div>
            <p class="help-block"><asp:Literal ID="litPolitica" runat="server"></asp:Literal></p>
        </div>
    </div>

    <p><asp:Literal ID="litResumen" runat="server"></asp:Literal></p>

    <asp:GridView ID="gvEventos" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
        AllowPaging="true" PageSize="25" OnPageIndexChanging="gvEventos_PageIndexChanging"
        OnRowDataBound="gvEventos_RowDataBound"
        EmptyDataText="No se registraron eventos para los filtros aplicados.">
        <Columns>
            <asp:BoundField DataField="IdEvento" HeaderText="Código" ItemStyle-Width="70px" />
            <asp:BoundField DataField="Usuario" HeaderText="Usuario" ItemStyle-Width="140px" />
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" ItemStyle-Width="90px" />
            <asp:BoundField DataField="Hora" HeaderText="Hora" ItemStyle-Width="80px" />
            <asp:BoundField DataField="Accion" HeaderText="Acción" ItemStyle-Width="200px" />
            <asp:BoundField DataField="Criticidad" HeaderText="Criticidad" ItemStyle-Width="110px" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" ItemStyle-Width="90px" Visible="false" />
            <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
        </Columns>
        <PagerStyle CssClass="text-center" />
    </asp:GridView>

    <div class="panel panel-default">
        <div class="panel-heading"><strong>Archivo histórico (XML)</strong></div>
        <div class="panel-body">
            <p>Antes de pasar a eliminados, los eventos se archivan en un archivo XML por mes. Cada mes se puede
                descargar o filtrar con una consulta XPath sobre el archivo, sin pasar por la base de datos.</p>
            <asp:Panel ID="pnlArchivos" runat="server">
                <div class="row">
                    <div class="col-md-4 form-group">
                        <asp:Label ID="lblArchivo" runat="server" AssociatedControlID="ddlArchivo"
                            CssClass="control-label" Text="Mes archivado"></asp:Label>
                        <asp:DropDownList ID="ddlArchivo" runat="server" CssClass="form-control"></asp:DropDownList>
                    </div>
                    <div class="col-md-3 form-group">
                        <label class="control-label">&nbsp;</label><br />
                        <asp:Button ID="btnDescargarArchivo" runat="server" Text="Descargar XML"
                            CssClass="btn btn-default" OnClick="btnDescargarArchivo_Click" CausesValidation="false" />
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-3 form-group">
                        <asp:Label ID="lblUsuarioArchivo" runat="server" AssociatedControlID="ddlUsuarioArchivo"
                            CssClass="control-label" Text="Usuario"></asp:Label>
                        <asp:DropDownList ID="ddlUsuarioArchivo" runat="server" CssClass="form-control"></asp:DropDownList>
                    </div>
                    <div class="col-md-2 form-group">
                        <asp:Label ID="lblCriticidadArchivo" runat="server" AssociatedControlID="ddlCriticidadArchivo"
                            CssClass="control-label" Text="Criticidad"></asp:Label>
                        <asp:DropDownList ID="ddlCriticidadArchivo" runat="server" CssClass="form-control">
                            <asp:ListItem Text="(todas)" Value=""></asp:ListItem>
                            <asp:ListItem Text="Informativa" Value="Informativa"></asp:ListItem>
                            <asp:ListItem Text="Advertencia" Value="Advertencia"></asp:ListItem>
                            <asp:ListItem Text="Grave" Value="Grave"></asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-4 form-group">
                        <asp:Label ID="lblTextoArchivo" runat="server" AssociatedControlID="txtTextoArchivo"
                            CssClass="control-label" Text="Acción o descripción"></asp:Label>
                        <asp:TextBox ID="txtTextoArchivo" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                    </div>
                    <div class="col-md-3 form-group">
                        <label class="control-label">&nbsp;</label><br />
                        <asp:Button ID="btnFiltrarArchivo" runat="server" Text="Filtrar el mes"
                            CssClass="btn btn-primary" OnClick="btnFiltrarArchivo_Click" CausesValidation="false" />
                    </div>
                </div>
                <asp:PlaceHolder ID="phResultadoArchivo" runat="server" Visible="false">
                    <p class="campo-ayuda">Consulta XPath: <span translate="no" class="consulta-xpath"><asp:Literal ID="litConsultaXPath" runat="server"></asp:Literal></span></p>
                    <p><asp:Literal ID="litResumenArchivo" runat="server"></asp:Literal></p>
                    <asp:GridView ID="gvArchivo" runat="server" AutoGenerateColumns="false"
                        CssClass="table table-bordered table-condensed table-striped" GridLines="None"
                        AllowPaging="true" PageSize="25" OnPageIndexChanging="gvArchivo_PageIndexChanging"
                        EmptyDataText="No hay eventos archivados que cumplan la consulta.">
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
                </asp:PlaceHolder>
            </asp:Panel>
            <asp:Label ID="lblSinArchivos" runat="server" CssClass="text-muted" Visible="false"
                Text="Todavía no se archivó ningún mes."></asp:Label>
        </div>
    </div>

</asp:Content>
