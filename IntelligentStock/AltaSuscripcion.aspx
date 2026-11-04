<%@ Page Title="Alta de suscripción" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="AltaSuscripcion.aspx.cs" Inherits="AltaSuscripcion" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <div class="page-header">
        <h2>Alta de suscripción</h2>
    </div>

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <asp:Panel ID="pnlCompra" runat="server">

        <p class="help-block">Compra de un plan de IntelligentStock. Los precios están en pesos argentinos (ARS) y no incluyen IVA.</p>

        <h3 class="seccion-titulo">1. Plan</h3>
        <div class="row">
            <div class="col-md-5">
                <div class="form-group">
                    <asp:Label ID="lblPlan" runat="server" AssociatedControlID="ddlPlan"
                        CssClass="control-label" Text="Tipo de plan (*)"></asp:Label>
                    <asp:DropDownList ID="ddlPlan" runat="server" CssClass="form-control"
                        AutoPostBack="true" OnSelectedIndexChanged="Recalcular"></asp:DropDownList>
                </div>
            </div>
            <div class="col-md-4">
                <div class="form-group">
                    <asp:Label ID="lblMeses" runat="server" AssociatedControlID="ddlMeses"
                        CssClass="control-label" Text="Meses que paga por adelantado (*)"></asp:Label>
                    <asp:DropDownList ID="ddlMeses" runat="server" CssClass="form-control"
                        AutoPostBack="true" OnSelectedIndexChanged="Recalcular"></asp:DropDownList>
                    <span class="campo-ayuda">Pagando 3, 6 o 12 meses juntos hay descuento.</span>
                </div>
            </div>
            <div class="col-md-3">
                <div class="form-group">
                    <asp:Label ID="lblFechaInicio" runat="server" AssociatedControlID="txtFechaInicio"
                        CssClass="control-label" Text="Fecha de inicio (*)"></asp:Label>
                    <asp:TextBox ID="txtFechaInicio" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="rfvFechaInicio" runat="server" ControlToValidate="txtFechaInicio"
                        ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="Ingrese la fecha de inicio."></asp:RequiredFieldValidator>
                    <asp:RangeValidator ID="rvFechaInicio" runat="server" ControlToValidate="txtFechaInicio"
                        Type="Date" ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="La fecha de inicio tiene que estar entre hoy y un año adelante."></asp:RangeValidator>
                </div>
            </div>
        </div>

        <h3 class="seccion-titulo">2. Datos de facturación</h3>
        <div class="row">
            <div class="col-md-6">
                <div class="form-group">
                    <asp:Label ID="lblRazonSocial" runat="server" AssociatedControlID="txtRazonSocial"
                        CssClass="control-label" Text="Razón social o apellido y nombre (*)"></asp:Label>
                    <asp:TextBox ID="txtRazonSocial" runat="server" CssClass="form-control" MaxLength="100"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="rfvRazonSocial" runat="server" ControlToValidate="txtRazonSocial"
                        ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="Ingrese la razón social o el apellido y nombre."></asp:RequiredFieldValidator>
                </div>
                <div class="form-group">
                    <asp:Label ID="lblCondicionIva" runat="server" AssociatedControlID="ddlCondicionIva"
                        CssClass="control-label" Text="Condición frente al IVA (*)"></asp:Label>
                    <asp:DropDownList ID="ddlCondicionIva" runat="server" CssClass="form-control"
                        AutoPostBack="true" OnSelectedIndexChanged="Recalcular"></asp:DropDownList>
                    <span class="campo-ayuda">Responsable Inscripto y Monotributista reciben factura A; Exento y Consumidor Final, factura B.</span>
                </div>
                <div class="form-group">
                    <asp:Label ID="lblDocumento" runat="server" AssociatedControlID="txtDocumento"
                        CssClass="control-label" Text="CUIT (o DNI si es consumidor final) (*)"></asp:Label>
                    <asp:TextBox ID="txtDocumento" runat="server" CssClass="form-control" MaxLength="13"
                        placeholder="20-12345678-6"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="rfvDocumento" runat="server" ControlToValidate="txtDocumento"
                        ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="Ingrese el CUIT o el DNI."></asp:RequiredFieldValidator>
                    <asp:RegularExpressionValidator ID="revDocumento" runat="server" ControlToValidate="txtDocumento"
                        ValidationExpression="^(\d{2}-?\d{8}-?\d|\d{7,8})$" ValidationGroup="Alta" Display="Dynamic"
                        CssClass="text-danger"
                        ErrorMessage="Escriba un CUIT de 11 números (con o sin guiones) o un DNI de 7 u 8 números."></asp:RegularExpressionValidator>
                </div>
            </div>
            <div class="col-md-6">
                <div class="form-group">
                    <asp:Label ID="lblDomicilio" runat="server" AssociatedControlID="txtDomicilio"
                        CssClass="control-label" Text="Domicilio"></asp:Label>
                    <asp:TextBox ID="txtDomicilio" runat="server" CssClass="form-control" MaxLength="150"></asp:TextBox>
                </div>
                <div class="form-group">
                    <asp:Label ID="lblCorreo" runat="server" AssociatedControlID="txtCorreo"
                        CssClass="control-label" Text="Correo para recibir la factura (*)"></asp:Label>
                    <asp:TextBox ID="txtCorreo" runat="server" CssClass="form-control" TextMode="Email" MaxLength="100"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="rfvCorreo" runat="server" ControlToValidate="txtCorreo"
                        ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                        ErrorMessage="El correo electrónico es obligatorio."></asp:RequiredFieldValidator>
                    <asp:RegularExpressionValidator ID="revCorreo" runat="server" ControlToValidate="txtCorreo"
                        ValidationExpression="[^@\s]+@[^@\s]+\.[^@\s]+" ValidationGroup="Alta" Display="Dynamic"
                        CssClass="text-danger"
                        ErrorMessage="El correo debe tener texto antes de la @, y texto con un punto (el dominio) después de la @."></asp:RegularExpressionValidator>
                </div>
            </div>
        </div>

        <h3 class="seccion-titulo">3. Medio de pago</h3>
        <div class="form-group">
            <asp:RadioButtonList ID="rblMedioPago" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow"
                CssClass="opciones-pago" AutoPostBack="true" OnSelectedIndexChanged="Recalcular">
            </asp:RadioButtonList>
        </div>

        <asp:Panel ID="pnlContado" runat="server">
            <p class="campo-ayuda">Pago en efectivo o por transferencia. Al contado no hay cobros automáticos: el plan cubre los meses pagados.</p>
        </asp:Panel>

        <asp:Panel ID="pnlTarjeta" runat="server" Visible="false">
            <div class="row">
                <div class="col-md-6">
                    <div class="form-group">
                        <asp:Label ID="lblNumeroTarjeta" runat="server" AssociatedControlID="txtNumeroTarjeta"
                            CssClass="control-label" Text="Número de tarjeta (*)"></asp:Label>
                        <asp:TextBox ID="txtNumeroTarjeta" runat="server" CssClass="form-control" MaxLength="23"
                            EnableViewState="false" autocomplete="off" inputmode="numeric" placeholder="4111 1111 1111 1111"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvNumeroTarjeta" runat="server" ControlToValidate="txtNumeroTarjeta"
                            ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                            ErrorMessage="Ingrese el número de la tarjeta."></asp:RequiredFieldValidator>
                        <asp:RegularExpressionValidator ID="revNumeroTarjeta" runat="server" ControlToValidate="txtNumeroTarjeta"
                            ValidationExpression="^[\d -]{13,23}$" ValidationGroup="Alta" Display="Dynamic"
                            CssClass="text-danger"
                            ErrorMessage="El número de tarjeta tiene entre 13 y 19 números (puede escribirlo con espacios)."></asp:RegularExpressionValidator>
                        <span class="campo-ayuda">Visa, Mastercard o American Express. Se controla el dígito de control y no se guarda.</span>
                    </div>
                    <div class="form-group">
                        <asp:Label ID="lblTitular" runat="server" AssociatedControlID="txtTitular"
                            CssClass="control-label" Text="Nombre del titular, como figura en la tarjeta (*)"></asp:Label>
                        <asp:TextBox ID="txtTitular" runat="server" CssClass="form-control" MaxLength="50"
                            EnableViewState="false" autocomplete="off"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvTitular" runat="server" ControlToValidate="txtTitular"
                            ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                            ErrorMessage="Ingrese el nombre del titular."></asp:RequiredFieldValidator>
                        <asp:RegularExpressionValidator ID="revTitular" runat="server" ControlToValidate="txtTitular"
                            ValidationExpression="^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ' .\-]+$" ValidationGroup="Alta" Display="Dynamic"
                            CssClass="text-danger"
                            ErrorMessage="El nombre del titular solo puede tener letras, espacios, apóstrofos y guiones."></asp:RegularExpressionValidator>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="row">
                        <div class="col-xs-6">
                            <div class="form-group">
                                <asp:Label ID="lblVencimiento" runat="server" AssociatedControlID="txtVencimiento"
                                    CssClass="control-label" Text="Vencimiento (*)"></asp:Label>
                                <asp:TextBox ID="txtVencimiento" runat="server" CssClass="form-control" MaxLength="7"
                                    EnableViewState="false" autocomplete="off" placeholder="MM/AA"></asp:TextBox>
                                <asp:RequiredFieldValidator ID="rfvVencimiento" runat="server" ControlToValidate="txtVencimiento"
                                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                                    ErrorMessage="Ingrese el vencimiento."></asp:RequiredFieldValidator>
                                <asp:RegularExpressionValidator ID="revVencimiento" runat="server" ControlToValidate="txtVencimiento"
                                    ValidationExpression="^(0[1-9]|1[0-2])/(\d{2}|\d{4})$" ValidationGroup="Alta" Display="Dynamic"
                                    CssClass="text-danger" ErrorMessage="Escriba el mes y el año, por ejemplo 08/29."></asp:RegularExpressionValidator>
                            </div>
                        </div>
                        <div class="col-xs-6">
                            <div class="form-group">
                                <asp:Label ID="lblCodigo" runat="server" AssociatedControlID="txtCodigo"
                                    CssClass="control-label" Text="Código de seguridad (*)"></asp:Label>
                                <asp:TextBox ID="txtCodigo" runat="server" CssClass="form-control" MaxLength="4"
                                    EnableViewState="false" autocomplete="off" inputmode="numeric"></asp:TextBox>
                                <asp:RequiredFieldValidator ID="rfvCodigo" runat="server" ControlToValidate="txtCodigo"
                                    ValidationGroup="Alta" Display="Dynamic" CssClass="text-danger"
                                    ErrorMessage="Ingrese el código de seguridad."></asp:RequiredFieldValidator>
                                <asp:RegularExpressionValidator ID="revCodigo" runat="server" ControlToValidate="txtCodigo"
                                    ValidationExpression="^\d{3,4}$" ValidationGroup="Alta" Display="Dynamic"
                                    CssClass="text-danger" ErrorMessage="El código de seguridad tiene 3 o 4 números."></asp:RegularExpressionValidator>
                            </div>
                        </div>
                    </div>
                    <div class="form-group">
                        <asp:Label ID="lblCuotas" runat="server" AssociatedControlID="ddlCuotas"
                            CssClass="control-label" Text="Cuotas sin interés (*)"></asp:Label>
                        <asp:DropDownList ID="ddlCuotas" runat="server" CssClass="form-control"
                            AutoPostBack="true" OnSelectedIndexChanged="Recalcular"></asp:DropDownList>
                    </div>
                </div>
            </div>
            <p class="campo-ayuda">Pasarela de cobros simulada. Tarjetas de prueba: 4111 1111 1111 1111 (se aprueba) y
                4000 0000 0000 0002 (la rechaza la entidad emisora). Con tarjeta, al terminar los meses pagados la
                pasarela sigue cobrando el abono todos los meses hasta que se dé de baja la suscripción.</p>
        </asp:Panel>

        <h3 class="seccion-titulo">Resumen de la compra</h3>
        <div class="resumen-caja">
            <asp:Literal ID="litResumen" runat="server" Mode="PassThrough"></asp:Literal>
        </div>

        <div class="acciones">
            <asp:Button ID="btnAceptar" runat="server" Text="Confirmar compra" CssClass="btn btn-primary"
                ValidationGroup="Alta" OnClick="btnAceptar_Click"
                OnClientClick="return confirm('Se va a registrar el pago, activar la suscripción y emitir la factura. ¿Continuar?');" />
            <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" CssClass="btn btn-default"
                OnClick="btnCancelar_Click" CausesValidation="false" />
        </div>

    </asp:Panel>

    <asp:Panel ID="pnlResultado" runat="server" Visible="false">
        <h3 class="seccion-titulo">Compra realizada</h3>
        <asp:Literal ID="litResultado" runat="server" Mode="PassThrough"></asp:Literal>
        <div class="acciones">
            <asp:Button ID="btnDescargar" runat="server" Text="Descargar factura (PDF)" CssClass="btn btn-primary"
                OnClick="btnDescargar_Click" CausesValidation="false" />
            <asp:Button ID="btnReenviar" runat="server" Text="Reenviar por correo" CssClass="btn btn-default"
                OnClick="btnReenviar_Click" CausesValidation="false" />
            <asp:Button ID="btnVolver" runat="server" Text="Volver al panel" CssClass="btn btn-default"
                OnClick="btnCancelar_Click" CausesValidation="false" />
        </div>
    </asp:Panel>

</asp:Content>
