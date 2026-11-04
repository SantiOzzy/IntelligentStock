<%@ Page Title="Inicio de sesión" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="IniciarSesion.aspx.cs" Inherits="IniciarSesion" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

    <asp:Label ID="lblMensaje" runat="server" Visible="false"></asp:Label>

    <%-- ------------------ Modo 1: todavía no hay sesión ------------------ --%>
    <asp:Panel ID="pnlLogin" runat="server">
        <div class="acceso">
                <div class="acceso-marca">
                    <span class="marca-icono marca-icono-grande" aria-hidden="true"><i></i><i></i><i></i></span>
                    <p class="acceso-nombre">IntelligentStock</p>
                    <p class="acceso-lema">Stock, compras, recetas y alertas de frío del local, en un solo lugar.</p>
                </div>
                <div class="acceso-formulario">
                    <h1 class="acceso-titulo">Iniciar sesión</h1>

                        <div class="form-group">
                            <asp:Label ID="lblUsuario" runat="server" AssociatedControlID="txtUsuario"
                                CssClass="control-label" Text="Nombre de usuario"></asp:Label>
                            <asp:TextBox ID="txtUsuario" runat="server" CssClass="form-control"
                                MaxLength="50"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="rfvUsuario" runat="server"
                                ControlToValidate="txtUsuario" ValidationGroup="Login" Display="Dynamic"
                                CssClass="text-danger" ErrorMessage="Debe ingresar el nombre de usuario."></asp:RequiredFieldValidator>
                        </div>

                        <div class="form-group">
                            <asp:Label ID="lblClave" runat="server" AssociatedControlID="txtClave"
                                CssClass="control-label" Text="Contraseña"></asp:Label>
                            <asp:TextBox ID="txtClave" runat="server" CssClass="form-control"
                                TextMode="Password" MaxLength="50"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="rfvClave" runat="server"
                                ControlToValidate="txtClave" ValidationGroup="Login" Display="Dynamic"
                                CssClass="text-danger" ErrorMessage="Debe ingresar la contraseña."></asp:RequiredFieldValidator>
                        </div>

                        <asp:Button ID="btnIngresar" runat="server" Text="Ingresar"
                            CssClass="btn btn-primary btn-block" ValidationGroup="Login"
                            OnClick="btnIngresar_Click" />

                        <p class="acceso-enlace">
                            <asp:LinkButton ID="lnkOlvideClave" runat="server" CausesValidation="false"
                                OnClick="lnkOlvideClave_Click" Text="¿Olvidaste tu contraseña?"></asp:LinkButton>
                        </p>
                </div>
        </div>
    </asp:Panel>

    <%-- --------- Modo 3: pedir el enlace de recuperación por correo --------- --%>
    <asp:Panel ID="pnlRecuperar" runat="server" Visible="false" DefaultButton="btnEnviarEnlace">
        <div class="acceso">
                <div class="acceso-marca">
                    <span class="marca-icono marca-icono-grande" aria-hidden="true"><i></i><i></i><i></i></span>
                    <p class="acceso-nombre">IntelligentStock</p>
                    <p class="acceso-lema">Stock, compras, recetas y alertas de frío del local, en un solo lugar.</p>
                </div>
                <div class="acceso-formulario">
                    <h1 class="acceso-titulo">Recuperar contraseña</h1>

                        <p class="help-block">
                            Ingrese el correo electrónico de su cuenta. Le vamos a enviar un enlace
                            para definir una contraseña nueva, sin necesidad de que se la restablezca
                            un administrador.
                        </p>

                        <div class="form-group">
                            <asp:Label ID="lblCorreoRecuperar" runat="server" AssociatedControlID="txtCorreoRecuperar"
                                CssClass="control-label" Text="Correo electrónico"></asp:Label>
                            <asp:TextBox ID="txtCorreoRecuperar" runat="server" CssClass="form-control"
                                TextMode="Email" MaxLength="100"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="rfvCorreoRecuperar" runat="server"
                                ControlToValidate="txtCorreoRecuperar" ValidationGroup="Recuperar" Display="Dynamic"
                                CssClass="text-danger" ErrorMessage="Debe ingresar el correo electrónico."></asp:RequiredFieldValidator>
                            <asp:RegularExpressionValidator ID="revCorreoRecuperar" runat="server" ControlToValidate="txtCorreoRecuperar"
                                ValidationExpression="[^@\s]+@[^@\s]+\.[^@\s]+" ValidationGroup="Recuperar"
                                Display="Dynamic" CssClass="text-danger"
                                ErrorMessage="El correo debe tener texto antes de la @, y texto con un punto (el dominio) después de la @."></asp:RegularExpressionValidator>
                        </div>

                        <asp:Button ID="btnEnviarEnlace" runat="server" Text="Enviar enlace"
                            CssClass="btn btn-primary btn-block" ValidationGroup="Recuperar"
                            OnClick="btnEnviarEnlace_Click" />

                        <p class="acceso-enlace">
                            <asp:LinkButton ID="lnkVolverAlLogin" runat="server" CausesValidation="false"
                                OnClick="lnkVolverAlLogin_Click" Text="Volver al inicio de sesión"></asp:LinkButton>
                        </p>
                </div>
        </div>
    </asp:Panel>

    <%-- ------ Modo 4: definir la contraseña nueva con el enlace recibido ------ --%>
    <asp:Panel ID="pnlNuevaClave" runat="server" Visible="false" DefaultButton="btnGuardarClaveNueva">
        <asp:HiddenField ID="hdnToken" runat="server" />
        <div class="acceso">
                <div class="acceso-marca">
                    <span class="marca-icono marca-icono-grande" aria-hidden="true"><i></i><i></i><i></i></span>
                    <p class="acceso-nombre">IntelligentStock</p>
                    <p class="acceso-lema">Stock, compras, recetas y alertas de frío del local, en un solo lugar.</p>
                </div>
                <div class="acceso-formulario">
                    <h1 class="acceso-titulo">Nueva contraseña</h1>

                        <p class="help-block">
                            <asp:Literal ID="litCuentaRecuperada" runat="server"></asp:Literal>
                            Debe tener al menos 8 caracteres, con una mayúscula, una minúscula, un número y un carácter especial (por ejemplo !, #, $, %, * o @).
                        </p>

                        <div class="form-group">
                            <asp:Label ID="lblClaveNueva" runat="server" AssociatedControlID="txtClaveNueva"
                                CssClass="control-label" Text="Nueva contraseña"></asp:Label>
                            <asp:TextBox ID="txtClaveNueva" runat="server" CssClass="form-control"
                                TextMode="Password" MaxLength="50"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="rfvClaveNueva" runat="server"
                                ControlToValidate="txtClaveNueva" ValidationGroup="NuevaClave" Display="Dynamic"
                                CssClass="text-danger" ErrorMessage="Debe ingresar la nueva contraseña."></asp:RequiredFieldValidator>
                            <asp:RegularExpressionValidator ID="revClaveNueva" runat="server"
                                ControlToValidate="txtClaveNueva" ValidationGroup="NuevaClave" Display="Dynamic"
                                ValidationExpression="^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9ÁÉÍÓÚÜÑáéíóúüñ\s]).{8,}$" CssClass="text-danger"
                                ErrorMessage="La contraseña debe tener al menos 8 caracteres, con una mayúscula, una minúscula, un número y un carácter especial (por ejemplo !, #, $, %, * o @)."></asp:RegularExpressionValidator>
                        </div>

                        <div class="form-group">
                            <asp:Label ID="lblRepetirClaveNueva" runat="server" AssociatedControlID="txtRepetirClaveNueva"
                                CssClass="control-label" Text="Repetir contraseña"></asp:Label>
                            <asp:TextBox ID="txtRepetirClaveNueva" runat="server" CssClass="form-control"
                                TextMode="Password" MaxLength="50"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="rfvRepetirClaveNueva" runat="server"
                                ControlToValidate="txtRepetirClaveNueva" ValidationGroup="NuevaClave" Display="Dynamic"
                                CssClass="text-danger" ErrorMessage="Debe repetir la nueva contraseña."></asp:RequiredFieldValidator>
                            <asp:CompareValidator ID="cmpClaveNueva" runat="server"
                                ControlToValidate="txtRepetirClaveNueva" ControlToCompare="txtClaveNueva"
                                ValidationGroup="NuevaClave" Display="Dynamic" CssClass="text-danger"
                                ErrorMessage="Las contraseñas no coinciden."></asp:CompareValidator>
                        </div>

                        <asp:Button ID="btnGuardarClaveNueva" runat="server" Text="Guardar contraseña"
                            CssClass="btn btn-primary btn-block" ValidationGroup="NuevaClave"
                            OnClick="btnGuardarClaveNueva_Click" />
                </div>
        </div>
    </asp:Panel>

    <%-- --------------- Modo 2: panel principal del usuario --------------- --%>
    <%--
        Página de bienvenida: presentación del negocio. Es contenido fijo, no
        consulta la base de datos ni toca el dígito verificador. Los textos se
        pueden cambiar acá directamente; los precios salen de la lista de planes
        de BLLSuscripcion, que es la misma que usa Alta de suscripción.
    --%>
    <asp:Panel ID="pnlPanel" runat="server" Visible="false">

        <p class="saludo">
            <strong><asp:Literal ID="litBienvenida" runat="server"></asp:Literal></strong>
            <asp:Literal ID="litSubtitulo" runat="server"></asp:Literal>
        </p>

        <asp:Literal ID="litOpciones" runat="server" Mode="PassThrough"></asp:Literal>

        <section class="portada" aria-labelledby="tituloPortada">
            <div class="portada-texto">
                <h1 id="tituloPortada">El stock de su cocina, bajo control</h1>
                <p>
                    IntelligentStock es un sistema para bares, restaurantes y casas de comida: lleva el
                    inventario de insumos, descuenta lo que se usa en cada plato vendido, sugiere qué
                    comprar y vigila la temperatura de heladeras y cámaras.
                </p>
            </div>
            <div class="portada-figura" aria-hidden="true">
                <span class="estante"><i></i><i></i><i></i><i></i><i></i></span>
            </div>
        </section>

        <section class="bienvenida-seccion" aria-labelledby="tituloQuienes">
            <h2 id="tituloQuienes">Quiénes somos</h2>
            <p class="bienvenida-lectura">
                Somos un equipo de desarrollo de Lomas de Zamora que conoce de cerca el trabajo en una
                cocina: el insumo que se termina en medio del servicio, la compra hecha a ojo y la cámara
                que se apagó de noche. IntelligentStock nació para resolver esos problemas con información
                confiable y fácil de usar, desde la computadora del local o el celular.
            </p>
        </section>

        <section class="bienvenida-seccion" aria-labelledby="tituloOfrecemos">
            <h2 id="tituloOfrecemos">Qué ofrecemos</h2>
            <ul class="servicios">
                <li>
                    <span class="glyphicon glyphicon-tasks" aria-hidden="true"></span>
                    <h3>Inventario de insumos</h3>
                    <p>Stock, stock mínimo, vida útil y ajustes con su motivo, siempre al día.</p>
                </li>
                <li>
                    <span class="glyphicon glyphicon-cutlery" aria-hidden="true"></span>
                    <h3>Recetas y ventas</h3>
                    <p>Cada plato vendido descuenta solo los insumos de su receta.</p>
                </li>
                <li>
                    <span class="glyphicon glyphicon-shopping-cart" aria-hidden="true"></span>
                    <h3>Compras inteligentes</h3>
                    <p>Órdenes de compra sugeridas según la demanda prevista con aprendizaje automático.</p>
                </li>
                <li>
                    <span class="glyphicon glyphicon-dashboard" aria-hidden="true"></span>
                    <h3>Cadena de frío</h3>
                    <p>Sensores de temperatura que avisan cuando una heladera o cámara se sale de rango.</p>
                </li>
                <li>
                    <span class="glyphicon glyphicon-stats" aria-hidden="true"></span>
                    <h3>Reportes gerenciales</h3>
                    <p>Consumo, ventas, compras por proveedor y alertas, en PDF, Excel o CSV.</p>
                </li>
                <li>
                    <span class="glyphicon glyphicon-lock" aria-hidden="true"></span>
                    <h3>Seguridad</h3>
                    <p>Usuarios con roles y permisos, bitácora de eventos y control de integridad de los datos.</p>
                </li>
            </ul>
        </section>

        <section class="bienvenida-seccion" aria-labelledby="tituloPlanes">
            <h2 id="tituloPlanes">Planes</h2>
            <p class="bienvenida-lectura">Abono mensual en pesos, con todas las actualizaciones incluidas.</p>
            <div class="planes">
                <article class="plan">
                    <h3>Básico</h3>
                    <p class="plan-precio"><%: PrecioDelPlan("Básico") %> <span>por mes</span></p>
                    <p class="plan-para">Para locales chicos que quieren ordenar su stock.</p>
                    <ul>
                        <li>Inventario de insumos y recetas</li>
                        <li>Descuento de stock por venta</li>
                        <li>Aviso de stock mínimo por correo</li>
                        <li>Soporte por correo electrónico</li>
                    </ul>
                </article>
                <article class="plan plan-destacado">
                    <p class="plan-marca">El más elegido</p>
                    <h3>Profesional</h3>
                    <p class="plan-precio"><%: PrecioDelPlan("Profesional") %> <span>por mes</span></p>
                    <p class="plan-para">Para restaurantes que compran seguido a sus proveedores.</p>
                    <ul>
                        <li>Todo lo del plan Básico</li>
                        <li>Proveedores y órdenes de compra</li>
                        <li>Compras sugeridas según la demanda</li>
                        <li>Reportes gerenciales</li>
                    </ul>
                </article>
                <article class="plan">
                    <h3>Empresarial</h3>
                    <p class="plan-precio"><%: PrecioDelPlan("Empresarial") %> <span>por mes</span></p>
                    <p class="plan-para">Para cocinas con cámaras de frío y varios turnos.</p>
                    <ul>
                        <li>Todo lo del plan Profesional</li>
                        <li>Sensores de temperatura y alertas de frío</li>
                        <li>Exportación de datos para auditoría</li>
                        <li>Soporte prioritario</li>
                    </ul>
                </article>
            </div>
        </section>

        <section class="bienvenida-seccion bienvenida-contacto" aria-labelledby="tituloContacto">
            <h2 id="tituloContacto">Contacto</h2>
            <p class="bienvenida-lectura">
                ¿Quiere sumar otro local o cambiar de plan? Escríbanos a
                <a href="mailto:contacto@intelligentstock.com.ar">contacto@intelligentstock.com.ar</a>
                y le respondemos en el día. Estamos en Lomas de Zamora, provincia de Buenos Aires.
            </p>
        </section>

    </asp:Panel>

</asp:Content>
