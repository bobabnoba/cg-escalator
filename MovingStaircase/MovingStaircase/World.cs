// -----------------------------------------------------------------------
// <file>World.cs</file>
// <copyright>Grupa za Grafiku, Interakciju i Multimediju 2013.</copyright>
// <author>Srđan Mihić</author>
// <author>Aleksandar Josić</author>
// <summary>Klasa koja enkapsulira OpenGL programski kod.</summary>
// -----------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Shapes;
using SharpGL.SceneGraph.Primitives;
using SharpGL.SceneGraph.Quadrics;
using SharpGL;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Threading;
using Rectangle = System.Drawing.Rectangle;

namespace MovingStaircase
{


    /// <summary>
    ///  Klasa enkapsulira OpenGL kod i omogucava njegovo iscrtavanje i azuriranje.
    /// </summary>
    public class World : IDisposable
    {
        #region Atributi

        /// <summary>
        ///	 Scena koja se prikazuje.
        /// </summary>
        private AssimpScene m_scene;

        /// <summary>
        ///	 Ugao rotacije sveta oko X ose.
        /// </summary>
        private float m_xRotation = 1.0f;

        /// <summary>
        ///	 Ugao rotacije sveta oko Y ose.
        /// </summary>
        private float m_yRotation = -1.0f; 

        /// <summary>
        ///	 Udaljenost scene od kamere.
        /// </summary>
        //private float m_sceneDistance = 23.0f;
        private float m_sceneDistance = 12.0f;

        /// <summary>
        ///	 Sirina OpenGL kontrole u pikselima.
        /// </summary>
        private int m_width;

        /// <summary>
        ///	 Visina OpenGL kontrole u pikselima.
        /// </summary>
        private int m_height;

        private float cc = MainWindow.ambientPointLightValue;

        private enum TextureObjects { Stairs = 0, Surface, Skin };
        private readonly int m_textureCount = Enum.GetNames(typeof(TextureObjects)).Length;
        private uint[] m_textures = null;
        private string[] m_textureFiles = { "..//..//images//metal.jpg", "..//..//images//ceramic_tiles2.jpg", "..//..//images//human-skin.jpg" };

        private DispatcherTimer timer;
        private bool animationInProgress = false;
        private int iteration;
        private float human_rotateX = 0.0f;
        private float human_rotateY = 180.0f;
        private float human_rotateZ = 0.0f;
        private float human_coordinateX = 2.5f;
        private float human_coordinateY = 0.5f;
        private float human_coordinateZ = 0.5f;

        #endregion Atributi

        #region Properties

        /// <summary>
        ///	 Scena koja se prikazuje.
        /// </summary>
        public AssimpScene Scene
        {
            get { return m_scene; }
            set { m_scene = value; }
        }

        /// <summary>
        ///	 Ugao rotacije sveta oko X ose.
        /// </summary>
        public float RotationX
        {
            get { return m_xRotation; }
            set { m_xRotation = value; }
        }

        /// <summary>
        ///	 Ugao rotacije sveta oko Y ose.
        /// </summary>
        public float RotationY
        {
            get { return m_yRotation; }
            set { m_yRotation = value; }
        }

        /// <summary>
        ///	 Udaljenost scene od kamere.
        /// </summary>
        public float SceneDistance
        {
            get { return m_sceneDistance; }
            set { m_sceneDistance = value; }
        }

        /// <summary>
        ///	 Sirina OpenGL kontrole u pikselima.
        /// </summary>
        public int Width
        {
            get { return m_width; }
            set { m_width = value; }
        }

        /// <summary>
        ///	 Visina OpenGL kontrole u pikselima.
        /// </summary>
        public int Height
        {
            get { return m_height; }
            set { m_height = value; }
        }

        public bool AnimationInProgress
        {
            get { return animationInProgress; }
        }
        #endregion Properties

        #region Konstruktori

        /// <summary>
        ///  Konstruktor klase World.
        /// </summary>
        public World(String scenePath, String sceneFileName, int width, int height, OpenGL gl)
        {
            this.m_scene = new AssimpScene(scenePath, sceneFileName, gl);
            this.m_width = width;
            this.m_height = height;
            this.m_textures = new uint[m_textureCount];

        }

        /// <summary>
        ///  Destruktor klase World.
        /// </summary>
        ~World()
        {
            this.Dispose(false);
        }

        #endregion Konstruktori

        #region Metode

        /// <summary>
        ///  Korisnicka inicijalizacija i podesavanje OpenGL parametara.
        /// </summary>
        public void Initialize(OpenGL gl)
        {
            gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            gl.Color(1.0f, 0.0f, 0.0f);
            gl.ShadeModel(OpenGL.GL_FLAT);
            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.Enable(OpenGL.GL_CULL_FACE);
            gl.FrontFace(OpenGL.GL_CCW);

            gl.Enable(OpenGL.GL_COLOR_MATERIAL);
            gl.ColorMaterial(OpenGL.GL_FRONT, OpenGL.GL_AMBIENT_AND_DIFFUSE);

            gl.Enable(OpenGL.GL_TEXTURE_2D);
            gl.TexEnv(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, OpenGL.GL_BLEND);

            gl.GenTextures(m_textureCount, m_textures);
            for (int i = 0; i < m_textureCount; ++i)
            {
                // Pridruzi teksturu odgovarajucem identifikatoru
                gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[i]);

                // Ucitaj sliku i podesi parametre teksture
                Bitmap image = new Bitmap(m_textureFiles[i]);
                // rotiramo sliku zbog koordinantog sistema opengl-a
                image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
                // RGBA format (dozvoljena providnost slike tj. alfa kanal)
                BitmapData imageData = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                gl.Build2DMipmaps(OpenGL.GL_TEXTURE_2D, (int)OpenGL.GL_RGBA8, image.Width, image.Height, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, imageData.Scan0);
                // kako se mapiraju teksture ako s i t izadju van opsega 0,1
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_REPEAT);
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_REPEAT);
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_NEAREST);
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_NEAREST);

                image.UnlockBits(imageData);
                image.Dispose();
            }
            
            m_scene.LoadScene();
            m_scene.Initialize();
        }

        /// <summary>
        ///  Iscrtavanje OpenGL kontrole.
        /// </summary>
        public void Draw(OpenGL gl)
        {
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            gl.Enable(OpenGL.GL_AUTO_NORMAL);
            gl.Viewport(0, 0, m_width, m_height);
            gl.MatrixMode(OpenGL.GL_PROJECTION);      
            gl.LoadIdentity();
            gl.Perspective(45.0, (double)m_width / (double)m_height, 0.5, 20000.0);
             gl.LookAt(3f, 12.5f, 4f, 3f, 11f, 1f, 0.0f, 1.0f, 0.0f);

            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            gl.LoadIdentity();

            gl.PushMatrix();
            gl.Translate(0.0f, 0.0f, -m_sceneDistance);
            gl.Rotate(-m_xRotation, 1.0f, 0.0f, 0.0f);
            gl.Rotate(m_yRotation, 0.0f, 1.0f, 0.0f);

            SetupLighting(gl);

            DrawCylinder(gl);
            DrawPerson(gl);
            DrawStairs(gl);
            DrawBase(gl);

            DrawText(gl);
            gl.PopMatrix();

            gl.Flush();
        }


        /// <summary>
        /// Podesava viewport i projekciju za OpenGL kontrolu.
        /// </summary>
        public void Resize(OpenGL gl, int width, int height)
        {
            m_width = width;
            m_height = height;
            gl.Viewport(0, 0, m_width, m_height); 
            gl.MatrixMode(OpenGL.GL_PROJECTION);      
            gl.LoadIdentity();
            gl.Perspective(45.0, (double)width / (double)height, 0.5, 20000.0);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            gl.LoadIdentity();
        }

        private void SetupLighting(OpenGL gl)
        {
            gl.Enable(OpenGL.GL_NORMALIZE);
            gl.ShadeModel(OpenGL.GL_SMOOTH);

            float[] global_ambient = new float[] { 0.2f, 0.2f, 0.2f, 1.0f };
            gl.LightModel(OpenGL.GL_LIGHT_MODEL_AMBIENT, global_ambient);

            float[] position = new float[] { 10.0f, -5.0f, 0.0f, 1.0f };
            float[] ambient = new float[] { MainWindow.ambientPointLightValue, MainWindow.ambientPointLightValue, MainWindow.ambientPointLightValue, 1.0f };
            float[] diffuse = new float[] { 1f, 1f, 1f, 1.0f };    
            float[] specular = new float[] { 0.5f, 0.5f, 0.5f, 1.0f };

            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_POSITION, position);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_AMBIENT, ambient);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_DIFFUSE, diffuse);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_SPECULAR, specular);

            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_SPOT_CUTOFF, 180.0f); 

            float[] positionRef = new float[] { 3.0f, -1.0f, 1.0f, 1.0f };
            float[] ambientRef = new float[] { 0.2f, 0.2f, 0.2f, 1.0f };
            float[] diffuseRef = new float[] { 0f, 0f, 1f, 1.0f };       
            float[] specularRef = new float[] { 0.8f, 0.8f, 0.8f, 1.0f };

            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_POSITION, positionRef);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_AMBIENT, ambientRef);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_DIFFUSE, diffuseRef);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPECULAR, specularRef);

            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPOT_CUTOFF, 35.0f);

            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPOT_DIRECTION, new float[] { 0.0f, 1.0f, 0.0f });

            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPOT_EXPONENT, 2.0f);

            gl.Enable(OpenGL.GL_LIGHTING);
            gl.Enable(OpenGL.GL_LIGHT0);
            gl.Enable(OpenGL.GL_LIGHT1);
        }

        private void DrawCylinder(OpenGL gl)
        {
            gl.Disable(OpenGL.GL_AUTO_NORMAL);
            gl.PushMatrix();
            gl.Color(.7f, .7f, .7f);
            gl.Translate(0f, -1f, 0f);
            gl.Rotate(-90f, 0f, 0f);
            gl.Color(.7f, .7f, .7f);
            Cylinder cil = new Cylinder
            {
                Height = 10,
                BaseRadius = 1.5,
                TopRadius = 1.5
            };
            cil.NormalGeneration = Normals.Smooth;
            cil.NormalOrientation = Orientation.Outside;
            cil.CreateInContext(gl);
            cil.Render(gl, SharpGL.SceneGraph.Core.RenderMode.Render);
            gl.PopMatrix();
            gl.Enable(OpenGL.GL_AUTO_NORMAL);
        }

      
        private void DrawStairs(OpenGL gl)
        {
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[(int)TextureObjects.Stairs]);

            gl.MatrixMode(OpenGL.GL_TEXTURE);
            gl.LoadIdentity();
            gl.Scale(1f, 1f, 1f);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);


            Cube cube = new Cube();
            for (int i = 0; i < 20; i++)
            {
                gl.PushMatrix();
                
                gl.Rotate(0f, i * 20f, 0f);
                gl.Translate(2.1f, -0.7f + i * 0.5, 0f);
                gl.Scale(1.5*0.8f, 0.2f, 0.2f);
                cube.Render(gl, SharpGL.SceneGraph.Core.RenderMode.Render);
                gl.PopMatrix();
            }
        }

        private void DrawPerson(OpenGL gl)
        {
            gl.PushMatrix();
            gl.Translate(human_coordinateX, human_coordinateY - 1.5f, human_coordinateZ);
            gl.Scale(MainWindow.bodyWeight * 0.03, 1f * 0.03, 1f * 0.03);
            gl.Rotate(human_rotateX, human_rotateY, human_rotateZ);
            m_scene.Draw();
            gl.PopMatrix();

        }

        private void DrawText(OpenGL gl)
        {
            gl.Viewport(0, m_width / 2, m_width / 2, m_height / 2);

            gl.PushMatrix();
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.LoadIdentity();
            gl.Ortho2D(-15.0f, 15.0f, -12.0f, 12.0f);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            gl.LoadIdentity();
            gl.Color(.6f, .6f, .6f);
            gl.Translate(1.5f, -7.0f, 0.0f);

            gl.PushMatrix();
            gl.DrawText3D("Arial Bold", 12.0f, 0f, 0f, "Predmet: Racunarska grafika");
            gl.PopMatrix();
            gl.PushMatrix();
            gl.Translate(0.0f, -1.0f, 0.0f);
            gl.DrawText3D("Arial Bold", 12.0f, 0f, 0f, "Sk.god: 2021/22");
            gl.PopMatrix();
            gl.PushMatrix();
            gl.Translate(0.0f, -2.0f, 0.0f);
            gl.DrawText3D("Arial Bold", 12.0f, 0f, 0f, "Ime: Bozana");
            gl.PopMatrix();
            gl.PushMatrix();
            gl.Translate(0.0f, -3.0f, 0.0f);
            gl.DrawText3D("Arial Bold", 12.0f, 0f, 0f, "Prezime: Ruljic");
            gl.PopMatrix();
            gl.PushMatrix();
            gl.Translate(0.0f, -4.0f, 0.0f);
            gl.DrawText3D("Arial Bold", 12.0f, 0f, 0f, "Sifra: 16.2");
            gl.PopMatrix();

            gl.PopMatrix();
            gl.Viewport(0, 0, m_width, m_height);

        }

        private void DrawBase(OpenGL gl)
        {
            gl.Disable(OpenGL.GL_AUTO_NORMAL);
            gl.PushMatrix();
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[(int)TextureObjects.Surface]);

            gl.MatrixMode(OpenGL.GL_TEXTURE);
            gl.LoadIdentity();
            gl.Scale(15f, 15f, 15f);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);



            gl.Begin(OpenGL.GL_QUADS);
            gl.Normal(0f, -1f, 0f);
            gl.Color(0.4f, 0.3f, 0.5f);

            gl.TexCoord(0.0f, 0.0f);
            gl.Vertex(-10f, -1f, -17.0f);

            gl.TexCoord(1.0f, 0.0f);
            gl.Vertex(-10f, -1f, 17.0f);

            gl.TexCoord(1.0f, 1.0f);
            gl.Vertex(10f, -1f, 17f);

            gl.TexCoord(0.0f, 1.0f);
            gl.Vertex(10f, -1f, -17f);

            gl.End();
            gl.PopMatrix();
            gl.Enable(OpenGL.GL_AUTO_NORMAL);
        }

        public void Animation()
        {
            animationInProgress = true;
            iteration = 0;
            human_coordinateX = 2.5f;
            human_coordinateY = 0.7f;
            human_coordinateZ = 0.5f;
            human_rotateY = 180f;
            timer = new DispatcherTimer();
            var sec = MainWindow.bodyWeight / 10;
            timer.Interval = TimeSpan.FromSeconds(sec);
            timer.Tick += new EventHandler(StartAnimation);
            timer.Start();
        }

        public void StartAnimation(object sender, EventArgs e)
        {
            if (iteration < 10)
                UpTo3rd();
            else if (iteration >= 10 && iteration < 20)
                UpTo6th();
            else if (iteration >= 20 && iteration < 30)
                UpTo9th();
            else if (iteration >= 30 && iteration < 40)
                UpTo14th();
            else if (iteration >= 40 && iteration < 50)
                UpTo17th();
            else if (iteration >= 50 && iteration < 60)
                UpToTheTop();
            else
            {
                timer.Stop();
                animationInProgress = false;
                human_coordinateX = 2.5f;
                human_coordinateY = 0.5f;
                human_coordinateZ = 0.5f;
                human_rotateY = 180f;
            }
        }

        private void UpToTheTop()
        {
            iteration++;
            human_coordinateX -= 0.02f;
            human_coordinateY += 0.15f;
            human_coordinateZ -= 0.18f;
            human_rotateY += 7f;
        }

        private void UpTo17th()
        {
            iteration++;
            human_coordinateX += 0.30f;
            human_coordinateY += 0.17f;
            human_coordinateZ -= 0.12f;
            human_rotateY += 7f;
        }

        private void UpTo14th()
        {
            iteration++;
            human_coordinateX += 0.12f;
            human_coordinateY += 0.19f;
            human_coordinateZ += 0.25f;
            human_rotateY += 9f;
        }

        private void UpTo9th()
        {
            iteration++;
            human_coordinateX -= 0.15f;
            human_coordinateY += 0.18f;
            human_coordinateZ += 0.20f;
            human_rotateY += 5f;
        }

        private void UpTo6th()
        {
            iteration++;
            human_coordinateX -= 0.26f;
            human_coordinateY += 0.16f;
            human_coordinateZ -= 0.07f;
            human_rotateY += 7f;
        }

        private void UpTo3rd()
        {
            iteration++;
            human_coordinateX -= 0.06f;
            human_coordinateY += 0.15f;
            human_coordinateZ -= 0.20f;
            human_rotateY += 3f;
        }

        /// <summary>
        ///  Implementacija IDisposable interfejsa.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_scene.Dispose();
            }
        }

        #endregion Metode

        #region IDisposable metode

        /// <summary>
        ///  Dispose metoda.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable metode
    }
}
