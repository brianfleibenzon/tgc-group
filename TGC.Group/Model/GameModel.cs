using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;
using TGC.Core.Utils;
using TGC.Core.Camara;
using TGC.Group.Camara;
using TGC.Core.Collision;
using TGC.Core.Shaders;
using TGC.Core.Fog;
using TGC.Core.Sound;
using System;
using System.Globalization;
using Microsoft.DirectX.DirectInput;

namespace TGC.Group.Model
{
    /// <summary>
    ///     Ejemplo para implementar el TP.
    ///     Inicialmente puede ser renombrado o copiado para hacer m�s ejemplos chicos, en el caso de copiar para que se
    ///     ejecute el nuevo ejemplo deben cambiar el modelo que instancia GameForm <see cref="Form.GameForm.InitGraphics()" />
    ///     line 97.
    /// </summary>
    public class GameModel : TgcExample
    {
        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;
        }

        private TgcFog fog;

        private TgcMp3Player sonidoEntorno;

        private Tgc3dSound sonidoPisadas;

        private Tgc3dSound sonidoLinterna;

        public TgcScene scene;

        private TgcPickingRay pickingRay;

        private Puerta[] puertas = new Puerta[8];

        private Interruptor[] interruptores = new Interruptor[3];

        private Iluminacion[] iluminaciones = new Iluminacion[3];

        private Enemigo[] enemigos = new Enemigo[2];

        private Vector3 collisionPoint;

        private float mostrarBloqueado = 0;

        TgcMesh bloqueado;

        private Iluminacion iluminacionEnMano;

        private Microsoft.DirectX.Direct3D.Effect effect;

        private bool luzActivada = true;

        //VARIABLES DE SONIDO
        int seg = DateTime.Now.Second;
        int aux2 = DateTime.Now.Second;

        //VARIABLES DE BATERIA

        Size resolucionPantalla = System.Windows.Forms.SystemInformation.PrimaryMonitorSize;
        float contador = 0;


        /// <summary>
        ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
        ///     Escribir aqu� todo el c�digo de inicializaci�n: cargar modelos, texturas, estructuras de optimizaci�n, todo
        ///     procesamiento que podemos pre calcular para nuestro juego.
        ///     Borrar el codigo ejemplo no utilizado.
        /// </summary>
        public override void Init()
        {
            //Device de DirectX para crear primitivas.
            var d3dDevice = D3DDevice.Instance.Device;

            var loader = new TgcSceneLoader();
            scene = loader.loadSceneFromFile(MediaDir + "Escenario\\Escenario-TgcScene.xml");

            effect = TgcShaders.loadEffect(ShadersDir + "MultiDiffuseLights.fx");

            Camara = new TgcFpsCamera(this, new Vector3(128f, 90f, 51f), Input);

            pickingRay = new TgcPickingRay(Input);

            InicializarEnemigos();
            InicializarPuertas();
            InicializarInterruptores();
            InicializarIluminaciones();

            bloqueado = loader.loadSceneFromFile(MediaDir + "Bloqueado\\locked-TgcScene.xml").Meshes[0];
            bloqueado.Scale = new Vector3(0.004f, 0.004f, 0.004f);
            bloqueado.Position = new Vector3(0.65f, -0.38f, 1f);

            fog = new TgcFog();

            sonidoPisadas = new Tgc3dSound(MediaDir + "Sonidos\\pasos.wav", Camara.Position, DirectSound.DsDevice);
            sonidoPisadas.MinDistance = 30f;

            sonidoEntorno = new TgcMp3Player();
            sonidoEntorno.FileName = MediaDir + "Sonidos\\entorno.mp3";
            sonidoEntorno.play(true);
        }

        void InicializarPuertas()
        {
            for (int i = 0; i < 8; i++)
            {
                puertas[i] = new Puerta();
                puertas[i].mesh = scene.getMeshByName("Puerta" + (i + 1));
            }

            puertas[2].estado = Puerta.Estado.BLOQUEADA;
            puertas[3].estado = Puerta.Estado.BLOQUEADA;
            puertas[4].funcion = () => { enemigos[1].desactivar(); enemigos[1].activar(); };
            puertas[5].funcion = () => { enemigos[0].desactivar(); enemigos[0].activar(); };
        }

        void InicializarEnemigos()
        {
            for (int i = 0; i < 2; i++)
            {
                enemigos[i] = new Enemigo();
                enemigos[i].mesh = scene.getMeshByName("Enemigo" + (i + 1));
            }

        }

        void InicializarInterruptores()
        {
            for (int i = 0; i < 3; i++)
            {
                interruptores[i] = new Interruptor();
                interruptores[i].mesh = scene.getMeshByName("Interruptor" + (i + 1));
            }

            interruptores[0].funcion = () => { puertas[2].estado = Puerta.Estado.CERRADA; puertas[3].estado = Puerta.Estado.CERRADA; };
            interruptores[1].funcion = () => { puertas[4].estado = Puerta.Estado.CERRADA; };

        }

        void InicializarIluminaciones()
        {
            iluminaciones[0] = new Iluminacion();
            iluminaciones[0].mesh = scene.getMeshByName("Vela");
            iluminaciones[0].posicionarEnMano = () =>
            {
                iluminacionEnMano.mesh.Scale = new Vector3(0.008f, 0.008f, 0.008f);
                iluminacionEnMano.mesh.Position = -iluminacionEnMano.mesh.BoundingBox.Position;
                iluminacionEnMano.mesh.Position += new Vector3(-0.8f, -0.38f, 1f);
            };
            iluminaciones[0].lightColors = Color.Orange;
            iluminaciones[0].pointLightPosition = iluminaciones[0].mesh.BoundingBox.Position + new Vector3(0f, 25f, 0f);
            iluminaciones[0].pointLightIntensityAgarrada = (float)68;
            iluminaciones[0].pointLightAttenuationAgarrada = (float)0.25;
            iluminaciones[0].pointLightIntensity = (float)38;
            iluminaciones[0].pointLightAttenuation = (float)0.5;
            iluminaciones[0].variarLuzEnable = true;
            iluminaciones[0].duracion = 20f;

            iluminaciones[1] = new Iluminacion();
            iluminaciones[1].mesh = scene.getMeshByName("Linterna");
            iluminaciones[1].posicionarEnMano = () =>
            {

                iluminacionEnMano.mesh.Scale = new Vector3(0.005f, 0.005f, 0.005f);
                iluminacionEnMano.mesh.Position = -iluminacionEnMano.mesh.BoundingBox.Position;
                iluminacionEnMano.mesh.Position += new Vector3(-0.8f, -0.38f, 1f);
            };
            iluminaciones[1].lightColors = Color.White;
            iluminaciones[1].pointLightPosition = iluminaciones[1].mesh.BoundingBox.Position + new Vector3(30f, 10f, 40f);
            iluminaciones[1].pointLightIntensityAgarrada = (float)108;
            iluminaciones[1].pointLightAttenuationAgarrada = (float)0.25;
            iluminaciones[1].pointLightIntensity = (float)38;
            iluminaciones[1].pointLightAttenuation = (float)0.5;
            iluminaciones[1].puedeApagarse = true;
            iluminaciones[1].duracion = 30f;

            iluminaciones[2] = new Iluminacion();
            iluminaciones[2].mesh = scene.getMeshByName("Farol");
            iluminaciones[2].posicionarEnMano = () =>
            {
                iluminacionEnMano.mesh.Scale = new Vector3(0.005f, 0.005f, 0.005f);
                iluminacionEnMano.mesh.Position = -iluminacionEnMano.mesh.BoundingBox.Position;
                iluminacionEnMano.mesh.Position += new Vector3(-0.8f, -0.38f, 1f);
            };
            iluminaciones[2].lightColors = Color.Yellow;
            iluminaciones[2].pointLightPosition = iluminaciones[2].mesh.BoundingBox.Position + new Vector3(0f, 25f, 0f);
            iluminaciones[2].pointLightIntensityAgarrada = (float)108;
            iluminaciones[2].pointLightAttenuationAgarrada = (float)0.15;
            iluminaciones[2].pointLightIntensity = (float)38;
            iluminaciones[2].pointLightAttenuation = (float)0.5;
            iluminaciones[2].puedeApagarse = true;
            iluminaciones[2].duracion = 50f;

        }

        void ActualizarEstadoPuertas()
        {
            foreach (var puerta in puertas)
            {
                puerta.actualizarEstado(Camara, ElapsedTime);

            }
        }

        void ActualizarEstadoEnemigos()
        {
            foreach (var enemigo in enemigos)
            {
                enemigo.actualizarEstado(Camara, ElapsedTime, scene);

            }
        }

        void ActualizarEstadoLuces()
        {
            foreach (var iluminacion in iluminaciones)
            {
                iluminacion.variarLuz(ElapsedTime);

            }
        }

        public bool VerificarSiMeshEsIluminacion(TgcMesh mesh)
        {
            foreach (var ilum in iluminaciones)
            {
                if (ilum.mesh == mesh)
                {
                    mesh.Enabled = false;
                    iluminacionEnMano = ilum;
                    contador = 0;
                    luzActivada = true;
                    iluminacionEnMano.posicionarEnMano();
                    return true;
                }
            }
            return false;
        }

        void VerificarColisionConClick()
        {
            if (Input.buttonPressed(TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                //Actualizar Ray de colision en base a posicion del mouse
                pickingRay.updateRay();
                //Testear Ray contra el AABB de todos los meshes
                foreach (var puerta in puertas)
                {
                    var aabb = puerta.mesh.BoundingBox;

                    //Ejecutar test, si devuelve true se carga el punto de colision collisionPoint

                    if (TgcCollisionUtils.intersectRayAABB(pickingRay.Ray, aabb, out collisionPoint))
                    {
                        if (TgcCollisionUtils.sqDistPointAABB(Camara.Position, puerta.mesh.BoundingBox) < 15000f)
                        {
                            switch (puerta.estado)
                            {
                                case (Puerta.Estado.BLOQUEADA):
                                    mostrarBloqueado = 3f;
                                    break;
                                case (Puerta.Estado.CERRADA):
                                    puerta.estado = Puerta.Estado.ABRIENDO;
                                    break;
                            }
                        }
                        break;
                    }
                }


                foreach (var interruptor in interruptores)
                {
                    var aabb = interruptor.mesh.BoundingBox;

                    //Ejecutar test, si devuelve true se carga el punto de colision collisionPoint

                    if (interruptor.estado == Interruptor.Estado.DESACTIVADO && TgcCollisionUtils.intersectRayAABB(pickingRay.Ray, aabb, out collisionPoint))
                    {
                        if (TgcCollisionUtils.sqDistPointAABB(Camara.Position, interruptor.mesh.BoundingBox) < 15000f)
                        {
                            interruptor.activar(puertas, MediaDir);
                        }
                        break;
                    }
                }
            }
        }


        //Intervalo: Cantidad de bateria que se pierde por intervalo
        //Porciento: Cantidad de bateria que se pierde por intervalo
        void reducirBateria()
        {
            if (iluminacionEnMano!=null && luzActivada)
            {
                contador += ElapsedTime;

                if (contador > iluminacionEnMano.duracion)
                {
                    iluminacionEnMano = null;
                    contador = 0;                 
                }              
                
            }
        }
        /// <summary>
        ///     Se llama en cada frame.
        ///     Se debe escribir toda la l�gica de computo del modelo, as� como tambi�n verificar entradas del usuario y reacciones
        ///     ante ellas.
        /// </summary>
        public override void Update()
        {
            PreUpdate();

            escucharTeclas();

            reducirBateria();

            ActualizarEstadoPuertas();

            ActualizarEstadoEnemigos();

            ActualizarEstadoLuces();


        }

        /// <summary>
        ///     Se llama cada vez que hay que refrescar la pantalla.
        ///     Escribir aqu� todo el c�digo referido al renderizado.
        ///     Borrar todo lo que no haga falta.
        /// </summary>
        public override void Render()
        {
            //Inicio el render de la escena, para ejemplos simples. Cuando tenemos postprocesado o shaders es mejor realizar las operaciones seg�n nuestra conveniencia.
            PreRender();


            var lightColors = new ColorValue[iluminaciones.Length];
            var pointLightPositions = new Vector4[iluminaciones.Length];
            var pointLightIntensity = new float[iluminaciones.Length];
            var pointLightAttenuation = new float[iluminaciones.Length];

            foreach (var mesh in scene.Meshes)
            {
                mesh.Effect = effect;
                mesh.Technique = "MultiDiffuseLightsTechnique";
            }


            for (var i = 0; i < iluminaciones.Length; i++)
            {

                lightColors[i] = ColorValue.FromColor(iluminaciones[i].lightColors);


               if (iluminacionEnMano == iluminaciones[i])
               {                   
                    
                    if (luzActivada)
                    {
                        pointLightPositions[i] = TgcParserUtils.vector3ToVector4(Camara.Position);
                        pointLightIntensity[i] = iluminaciones[i].pointLightIntensityAgarrada;
                        pointLightAttenuation[i] = iluminaciones[i].pointLightAttenuationAgarrada;
                    }

                    iluminaciones[i].mesh.Effect = TgcShaders.Instance.TgcMeshShader;
                    iluminaciones[i].mesh.Technique = TgcShaders.Instance.getTgcMeshTechnique(TgcMesh.MeshRenderType.DIFFUSE_MAP);


                }
                else if(iluminaciones[i].mesh.Enabled == false)
                {

                    pointLightPositions[i] = TgcParserUtils.vector3ToVector4(iluminaciones[i].pointLightPosition);

                    pointLightIntensity[i] = (float)0;

                    pointLightAttenuation[i] = (float)0;
                }
                else
                {
                    
                    pointLightPositions[i] = TgcParserUtils.vector3ToVector4(iluminaciones[i].pointLightPosition);

                    pointLightIntensity[i] = iluminaciones[i].pointLightIntensity;
                    pointLightAttenuation[i] = iluminaciones[i].pointLightAttenuation;
                }

            }



            //Renderizar meshes
            foreach (var mesh in scene.Meshes)
            {
                if (iluminacionEnMano == null || mesh != iluminacionEnMano.mesh)
                {

                    mesh.UpdateMeshTransform();

                    //Cargar variables de shader
                    mesh.Effect.SetValue("lightColor", lightColors);
                    mesh.Effect.SetValue("lightPosition", pointLightPositions);
                    mesh.Effect.SetValue("lightIntensity", pointLightIntensity);
                    mesh.Effect.SetValue("lightAttenuation", pointLightAttenuation);
                    mesh.Effect.SetValue("materialEmissiveColor",
                        ColorValue.FromColor((Color.Black)));
                    mesh.Effect.SetValue("materialDiffuseColor",
                        ColorValue.FromColor(Color.White));
                }
                //Renderizar modelo
            }

            //--------Nuebla---------//
            fog.Enabled = true;
            fog.StartDistance = 50f;
            fog.EndDistance = 1000f;
            fog.Density = 0.0015f;
            fog.Color = Color.Black;

            if (fog.Enabled)
            {
                fog.updateValues();
            }

           

            VerificarColisionConClick();


            //Dibuja un texto por pantalla
            DrawText.drawText(
                "Con clic izquierdo subimos la camara [Actual]: " + TgcParserUtils.printVector3(Camara.Position) + " - LookAt: " + TgcParserUtils.printVector3(Camara.LookAt), 0, 20,
                Color.OrangeRed);

            if (((TgcFpsCamera)Camara).colisiones)

                DrawText.drawText(
                    "Colisiones activadas (C para desactivar)", 0, 50,
                    Color.OrangeRed);
            else
                DrawText.drawText(
                   "Colisiones desactivadas (C para activar)", 0, 50,
                   Color.OrangeRed);

            if (iluminacionEnMano != null)
                DrawText.drawText(
                   "BATERIA: " + getBateria() + "%", resolucionPantalla.Width - 175, 30, Color.OrangeRed);

            if (luzActivada && iluminacionEnMano!=null && iluminacionEnMano.puedeApagarse)
                DrawText.drawText(
          "Precionar F pare apagar", 0, 70, Color.OrangeRed);
            else if (!luzActivada && iluminacionEnMano != null && iluminacionEnMano.puedeApagarse)
                DrawText.drawText(
          "Precionar F pare encender", 0, 70, Color.OrangeRed);


            if (mostrarBloqueado > 0)
            {


                var matrizView = D3DDevice.Instance.Device.Transform.View;
                D3DDevice.Instance.Device.Transform.View = Matrix.Identity;
                bloqueado.render();
                D3DDevice.Instance.Device.Transform.View = matrizView;
                mostrarBloqueado -= ElapsedTime;

            }
            else if (mostrarBloqueado < 0)
            {
                mostrarBloqueado = 0;
            }

          
            if (iluminacionEnMano != null)
            {
                var matrizView = D3DDevice.Instance.Device.Transform.View;
                D3DDevice.Instance.Device.Transform.View = Matrix.Identity;
                iluminacionEnMano.mesh.Enabled = true;
                iluminacionEnMano.mesh.render();
                iluminacionEnMano.mesh.Enabled = false;
                D3DDevice.Instance.Device.Transform.View = matrizView;

            }


            scene.renderAll();

            //Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            PostRender();
        }

        private int getBateria()
        {
            return 100 - (int)Math.Ceiling((contador / iluminacionEnMano.duracion) * 100);
        }

        private void escucharTeclas()
        {
            if (Input.keyPressed(Key.F) && iluminacionEnMano!=null && iluminacionEnMano.puedeApagarse)
            {
                luzActivada = !luzActivada;
            }
            if (Input.keyDown(Key.W) || Input.keyDown(Key.S) || Input.keyDown(Key.A) || Input.keyDown(Key.D))
            {

                sonidoPisadas.play(false);
            }

        }


        /// <summary>
        ///     Se llama cuando termina la ejecuci�n del ejemplo.
        ///     Hacer Dispose() de todos los objetos creados.
        ///     Es muy importante liberar los recursos, sobretodo los gr�ficos ya que quedan bloqueados en el device de video.
        /// </summary>
        public override void Dispose()
        {
            bloqueado.dispose();
            scene.disposeAll();
        }
    }
}