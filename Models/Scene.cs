using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace ObjectRendering.Models
{
	public class Scene
	{
		public Vector3D Camera { get; } = new Vector3D(0, 1, 0);
		public List<Face> Faces { get; } = new List<Face>();
		public List<Vector3D> Points { get; } = new List<Vector3D>();
		public List<Vector3D> Normals { get; set; } = new List<Vector3D>();
	}
}
