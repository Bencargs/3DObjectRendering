using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Windows.Media.Media3D;
using ObjectRendering.Models;
using System.Threading.Tasks;
using ObjectRendering.Services;

namespace ObjectRendering
{
    public partial class MainForm : Form
    {
		private Scene Scene { get; set; }
		private bool _running = true;

		public MainForm()
        {
            InitializeComponent();
			CenterToScreen();
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			Scene = LoadScene();

			var currentContext = BufferedGraphicsManager.Current;
			_buffer = currentContext.Allocate(this.CreateGraphics(), this.DisplayRectangle);
			Task.Run(Render);
		}

		private Scene LoadScene()
		{
			var path = Path.Combine(Path.GetDirectoryName(Directory.GetCurrentDirectory()), "..", "..", "Resources", "yacht.obj");

			var scene = new Scene();
			foreach (var line in File.ReadLines(path))
			{
				var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length < 1) continue;

				switch (tokens[0])
				{
					//o  - object name?
					case "v":
						scene.Points.Add(new Vector3D(
							double.Parse(tokens[1]),
							double.Parse(tokens[2]),
							double.Parse(tokens[3])));
						break;

					case "vn":
						scene.Normals.Add(new Vector3D(
							double.Parse(tokens[1]),
							double.Parse(tokens[2]),
							double.Parse(tokens[3])));
						break;

					case "f":
						if (tokens.Length < 5)
							// dunno why some faces are 3 points instead of 4 - skip them
							break;

						// each face vertex is in format - VertexIndex/TextureIndex/[NormalIndex]
						// index starts at 1
						var fv1 = tokens[1].Split('/', StringSplitOptions.RemoveEmptyEntries);
						var fv2 = tokens[2].Split('/', StringSplitOptions.RemoveEmptyEntries);
						var fv3 = tokens[3].Split('/', StringSplitOptions.RemoveEmptyEntries);
						var fv4 = tokens[4].Split('/', StringSplitOptions.RemoveEmptyEntries);
						scene.Faces.Add(new Face
						{
							VectorA = int.Parse(fv1[0]) - 1,
							VectorB = int.Parse(fv2[0]) - 1,
							VectorC = int.Parse(fv3[0]) - 1,
							VectorD = int.Parse(fv4[0]) - 1,

							//// Normals are not mandatory, use them if they exist
							//Normal = int.Parse(fv1[2]) - 1 // this assumes all face vertexes share the same normal as the first vertex
						});
						break;
				}
			}
			if (!scene.Normals.Any())
				scene.Normals = CalculateNormals(scene.Points, scene.Faces);

			return scene;
		}

		private List<Vector3D> CalculateNormals(
			List<Vector3D> points,
			List<Face> faces)
		{
			var normals = new List<Vector3D>();
			foreach (var face in faces)
			{
				var normal = Vector3D.CrossProduct(
					(points[face.VectorD] - points[face.VectorC]),
					(points[face.VectorD] - points[face.VectorA]));
				normal.Normalize();

				normals.Add(normal);
				face.Normal = normals.Count - 1;
			}
			return normals;
		}

		private void Render()
        {
			var renderer = new GifRenderService();

			var transPoints = Scene.Points.ToArray();
			var transformGroup = new Transform3DGroup();
			var transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 1, 1), 1));
			transformGroup.Children.Add(transform);

			var i = 0;
			while (_running)
			{
				_buffer.Graphics.Clear(Color.AliceBlue);

				i += i % 360; // object rotation

				transformGroup.Transform(transPoints);

				// z-buffering & shading
				var sortedFaces = Scene.Faces.Select(f => new
				{
					Face = f,
					CameraAngle = Vector3D.DotProduct(Scene.Camera, Scene.Normals[f.Normal]),

					// Janky z buffer face sorting - this seems to work, may be edge cases
					Depth = new[] { transPoints[f.VectorA].Z, transPoints[f.VectorB].Z, transPoints[f.VectorC].Z, transPoints[f.VectorD].Z }.Min()
				})
				//.Where(f => f.CameraAngle >= 0) // backface culling - if the normal is pointing away from camera, dont draw it
				.OrderBy(f => f.Depth); // painters algorithm, should make everything look 3d
				var farthest = sortedFaces.Min(f => f.Depth);

				foreach (var sf in sortedFaces)
				{
					// This colour stuff is just make believe
					//var reflection = sf.CameraAngle * 127;
					//var absorbtion = Math.Abs( sf.Depth * 127 );
					//var colour = Color.FromArgb(0, 0, (short) Math.Min(255, Math.Max(1, reflection + absorbtion)));
					var reflection = (short)(Math.Max(0, sf.CameraAngle) * 127);
					var absorbtion = (short)Math.Min(127, (Math.Abs(sf.Depth) / Math.Abs(farthest)) * 127);
					var colour = Color.FromArgb(225, absorbtion + reflection, absorbtion + reflection, absorbtion + reflection);

					var height = this.DisplayRectangle.Height; // windows drawing coordinate system is reversed, this prevents image apprearing upside down
					_buffer.Graphics.FillPolygon(
					new SolidBrush(colour),
					new[]
					{
						new Point((int) ((transPoints[sf.Face.VectorA].X + 100) * 1), (int) (height-((transPoints[sf.Face.VectorA].Y + 100) * 1))),
						new Point((int) ((transPoints[sf.Face.VectorB].X + 100) * 1), (int) (height-((transPoints[sf.Face.VectorB].Y + 100) * 1))),
						new Point((int) ((transPoints[sf.Face.VectorC].X + 100) * 1), (int) (height-((transPoints[sf.Face.VectorC].Y + 100) * 1))),
						new Point((int) ((transPoints[sf.Face.VectorD].X + 100) * 1), (int) (height-((transPoints[sf.Face.VectorD].Y + 100) * 1))),
					});
				}

				_buffer.Render();
				renderer.Update(_buffer.Graphics, this.DisplayRectangle);
			}

			renderer.Save();
		}

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
			if (e.KeyCode == Keys.Escape)
				_running = false;
		}
    }
}
