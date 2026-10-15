# Preparación para Web

Unity instalado: 6000.6.3f1. En esta máquina no está instalado **Web Build Support**; solo se encontraron los módulos Android y Windows. No se ha producido ni probado una compilación Web.

1. En Unity Hub, añadir Web Build Support a **esa misma versión** cuando el equipo disponga del módulo.
2. Abrir NovelaVisual. Ejecutar Cali > Configurar escenas existentes y tema.
3. File > Build Profiles > Web, cambiar al perfil Web. Revisar que 01_MainMenu sea la primera escena y las cuatro zonas estén incluidas.
4. En Player Settings > Resolution and Presentation, elegir la plantilla **CaliNature** de Assets/WebGLTemplates. La plantilla carga el loader y los archivos reales que genera Unity; no imita el juego con JavaScript.
5. Para la primera prueba, usar Development Build y Build And Run. Una vez validado, desactivarlo y compilar a una carpeta de distribución fuera de Assets.
6. Servir la carpeta completa por HTTP/HTTPS, sin abrir index.html mediante file://. No cambiar nombres relativos de Build ni StreamingAssets.
7. Si se usa compresión, configurar el servidor para Content-Encoding gzip/br según la opción elegida y application/wasm para WebAssembly. Alternativamente habilitar Decompression Fallback en Publishing Settings y medir tamaño/tiempo de carga. No asumir que un alojamiento está configurado.

El audio comienza tras Comenzar/Continuar/Ir al mapa. El navegador puede suspenderlo por cambios de pestaña o sus políticas. Probar siempre desde una interacción real. PlayerPrefs depende del almacenamiento del navegador y del origen; limpiar datos o cambiar de dominio puede perder el progreso. No hay cuentas ni sincronización remota.

## Pruebas pendientes en navegador

Chrome y Edge de escritorio: carga fría, fallo de descarga, audio inicial, volumen/silencio, las cuatro zonas, persistencia tras recargar y controles WASD/E/Espacio. Comprobar que mantener Espacio y el foco de un botón no seleccionan respuestas. Probar zoom y arrastre sobre mapa y paneles. En móvil/táctil: botones mantenidos, cancelar toque, tamaño horizontal/vertical y rendimiento. No se afirma compatibilidad universal ni se han inventado resultados.

Documentación oficial: https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-building.html
Plantillas: https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-templates.html
