using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>Adapts mover and ant path semantics to one canvas-editing surface.</summary>
    internal sealed class EditablePath
    {
        private readonly LevelObject _object;
        private readonly bool _ants;

        private EditablePath(LevelObject obj, bool ants)
        {
            _object = obj;
            _ants = ants;
        }

        /// <summary>Returns an adapter for an ant conveyor or real mover polyline, otherwise null.</summary>
        public static EditablePath? For(LevelObject obj)
        {
            return AntPath.IsAnts(obj.Type)
                ? new EditablePath(obj, ants: true)
                : MoverPath.IsPolylineMovement(obj.GetAttr("path"))
                ? new EditablePath(obj, ants: false)
                : null;
        }

        /// <summary>Unique editable points including the object anchor at index zero.</summary>
        public Vec2[] Points => _ants
            ? AntPath.Points(_object)
            : MoverPath.CanonicalPoints(Anchor, _object.GetAttr("path"));

        /// <summary>Whether another point can be inserted or appended without truncation.</summary>
        public bool CanAdd => _ants
            ? AntPath.CanAddPoint(_object)
            : MoverPath.CanAddCanonicalPoint(Anchor, _object.GetAttr("path"));

        /// <summary>Whether the final unique point has an explicit closing segment back to the anchor.</summary>
        public bool Closed => _ants && AntPath.IsClosed(_object.GetAttr("path"));

        /// <summary>Number of segments exposed to midpoint insertion handles.</summary>
        public int SegmentCount
        {
            get
            {
                int pointCount = Points.Length;
                return pointCount < 2 ? 0 : Closed ? pointCount : pointCount - 1;
            }
        }

        /// <summary>Returns the editable point under a level coordinate, or -1.</summary>
        public int HitPoint(Vec2 point, double tolerance)
        {
            return _ants
                ? AntPath.HitPoint(_object, point, tolerance)
                : MoverPath.HitCanonicalPoint(Anchor, _object.GetAttr("path"), point, tolerance);
        }

        /// <summary>Moves one editable vertex.</summary>
        public void MovePoint(int index, Vec2 point)
        {
            if (_ants)
            {
                AntPath.MovePoint(_object, index, point);
            }
            else
            {
                _object.SetAttr("path", MoverPath.MoveCanonicalPoint(
                    Anchor, _object.GetAttr("path"), index, point));
            }
        }

        /// <summary>Inserts a vertex after the supplied segment start.</summary>
        public void InsertPoint(int segmentIndex, Vec2 point)
        {
            if (_ants)
            {
                AntPath.InsertPoint(_object, segmentIndex, point);
            }
            else
            {
                _object.SetAttr("path", MoverPath.InsertCanonicalPoint(
                    Anchor, _object.GetAttr("path"), segmentIndex, point));
            }
        }

        /// <summary>Appends a vertex, before explicit ant closure when present.</summary>
        public void AppendPoint(Vec2 point)
        {
            if (_ants)
            {
                AntPath.AppendPoint(_object, point);
            }
            else
            {
                _object.SetAttr("path", MoverPath.AppendCanonicalPoint(
                    Anchor, _object.GetAttr("path"), point));
            }
        }

        /// <summary>Deletes one editable vertex according to the path type's minimum rules.</summary>
        public void DeletePoint(int index)
        {
            if (_ants)
            {
                AntPath.DeletePoint(_object, index);
            }
            else
            {
                _object.SetAttr("path", MoverPath.DeleteCanonicalPoint(
                    Anchor, _object.GetAttr("path"), index));
            }
        }

        private Vec2 Anchor => new(_object.X, _object.Y);
    }
}
