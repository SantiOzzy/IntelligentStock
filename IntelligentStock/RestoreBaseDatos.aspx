<%@ Page Title="Restaurar base de datos" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="RestoreBaseDatos.aspx.cs" Inherits="RestoreBaseDatos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Restaurar base de datos</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <div class="row">
        <div class="col-md-8">
            <div class="panel panel-default">
                <div class="panel-body">

                    <div class="form-group">
                        <asp:Label ID="lblArchivo" runat="server" CssClass="control-label"
                            Text="Archivo de restauración (.bak o .sql) (*)"></asp:Label>
                        <%-- El selector de archivos del navegador queda oculto: lo abre el botón Examinar
                             y el nombre del archivo elegido se muestra en el cuadro de al lado. --%>
                        <div class="input-group">
                            <input type="text" id="txtArchivoElegido" class="form-control" readonly="readonly"
                                placeholder="Ningún archivo elegido" onclick="document.getElementById('fuArchivo').click();" />
                            <span class="input-group-btn">
                                <button type="button" class="btn btn-default"
                                    onclick="document.getElementById('fuArchivo').click();">Examinar…</button>
                            </span>
                        </div>
                        <asp:FileUpload ID="fuArchivo" runat="server" ClientIDMode="Static" accept=".bak,.sql"
                            style="display: none;"
                            onchange="document.getElementById('txtArchivoElegido').value = this.files.length ? this.files[0].name : '';" />
                        <span class="help-block">
                            Se admite una copia de seguridad de SQL Server (<code>.bak</code>)
                            o un script de recreación (<code>.sql</code>).
                        </span>
                    </div>

                    <asp:Button ID="btnRestaurar" runat="server" Text="Restaurar"
                        CssClass="btn btn-danger" OnClick="btnRestaurar_Click"
                        OnClientClick="return confirm('La restauración reemplaza los datos actuales y cierra la sesión. ¿Continuar?');" />

                    <asp:Button ID="btnVolver" runat="server" Text="Volver"
                        CssClass="btn btn-default" OnClick="btnVolver_Click" CausesValidation="false" />

                </div>
            </div>
        </div>
    </div>

</asp:Content>
