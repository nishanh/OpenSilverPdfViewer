using System;
using System.Windows;
using System.Windows.Media;

namespace OpenSilverPdfViewer.Utility
{
    public sealed class Matrix2D : ICloneable
    {
        #region Properties / Fields

        private Matrix _matrix;
        public Matrix Matrix => _matrix;

        public double M11
        {
            get => _matrix.M11;
            set => _matrix.M11 = value;
        }
        public double M12
        {
            get => _matrix.M12;
            set => _matrix.M12 = value;
        }
        public double M21
        {
            get => _matrix.M21;
            set => _matrix.M21 = value;
        }
        public double M22
        {
            get => _matrix.M22;
            set => _matrix.M22 = value;
        }
        public double OffsetX
        {
            get => _matrix.OffsetX;
            set => _matrix.OffsetX = value;
        }
        public double OffsetY
        {
            get => _matrix.OffsetY;
            set => _matrix.OffsetY = value;
        }
        public bool IsIdentity => Matrix.IsIdentity;

        #endregion Properties / Fields
        #region Construction / Initialization

        public Matrix2D()
        {
            _matrix = new Matrix();
        }
        public Matrix2D(double m11, double m12, double m21, double m22, double dx, double dy)
        {
            _matrix = new Matrix(m11, m12, m21, m22, dx, dy);
        }
        public Matrix2D(Matrix2D copy)
        {
            _matrix = new Matrix(copy._matrix.M11, copy._matrix.M12, copy._matrix.M21, copy._matrix.M22, copy._matrix.OffsetX, copy._matrix.OffsetY);
        }
        public object Clone()
        {
            return new Matrix2D(this);
        }
        public Matrix2D Copy()
        {
            return (Matrix2D)Clone();
        }

        #endregion Construction / Initialization
        #region Transformations

        public void Reset()
        {
            _matrix.SetIdentity();
        }
        public void Invert()
        {
            _matrix.Invert();
        }
        public void Scale(double scaleX, double scaleY)
        {
            _matrix.Scale(scaleX, scaleY);
        }
        public void Scale(double scaleXY)
        {
            Scale(scaleXY, scaleXY);
        }
        public void ScaleX(double scaleX)
        {
            Scale(scaleX, 1d);
        }
        public void ScaleY(double scaleY)
        {
            Scale(1d, scaleY);
        }
        public void Mirror(bool mirrorX, bool mirrorY)
        {
            Scale(mirrorX ? -1d : 1d, mirrorY ? -1d : 1d);
        }
        public void MirrorX()
        {
            Scale(-1d, 1d);
        }
        public void MirrorY()
        {
            Scale(1d, -1d);
        }
        public void Translate(Point toPoint)
        {
            _matrix.Translate(toPoint.X, toPoint.Y);
        }
        public void TranslateX(double offsetX)
        {
            _matrix.Translate(offsetX, 0d);
        }
        public void TranslateY(double offsetY)
        {
            _matrix.Translate(0d, offsetY);
        }
        public void Translate(double offsetX, double offsetY)
        {
            _matrix.Translate(offsetX, offsetY);
        }
        public void TranslateAbs(double offsetX, double offsetY)
        {
            _matrix.OffsetX = offsetX;
            _matrix.OffsetY = offsetY;
        }
        public void Rotate(double angle)
        {
            _matrix.Rotate(angle);
        }
        public void RotateAt(double angle, Point center)
        {
            _matrix.RotateAt(angle, center.X, center.Y);
        }
        public void RotateAt(double angle, double centerX, double centerY)
        {
            _matrix.RotateAt(angle, centerX, centerY);
        }

        #endregion Transformations
        #region Operations

        /// <summary>
        /// Transforms the <paramref name="point"/>
        /// </summary>
        /// <param name="point"></param>
        public void TransformPoint(ref Point point)
        {
            point = _matrix.Transform(point);
        }
        /// <summary>
        /// Transforms the <paramref name="point"/>
        /// </summary>
        /// <param name="point"></param>
        public Point TransformPoint(Point point)
        {
            return _matrix.Transform(point);
        }
        /// <summary>
        /// Transforms the specified x/y point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
		public void Transform(ref double x, ref double y)
        {
            var transPoint = _matrix.Transform(new Point(x, y));
            x = transPoint.X; y = transPoint.Y;
            /*
            var tmpx = x;
            var tmpy = y;
            x = (tmpx * _matrix.M11) + (tmpy * _matrix.M21) + _matrix.OffsetX;
            y = (tmpx * _matrix.M12) + (tmpy * _matrix.M22) + _matrix.OffsetY;
            */
        }
        public void Transform(Point[] points)
        {
            _matrix.Transform(points);
        }
        /// <summary>
        /// Transforms the <paramref name="rect"/> to an array of points
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public Point[] TransformRectToPoints(Rect rect)
        {
            var rectPoints = new Point[4];

            rectPoints[0].X = rect.Left; rectPoints[0].Y = rect.Top;
            rectPoints[1].X = rect.Right; rectPoints[1].Y = rect.Top;
            rectPoints[2].X = rect.Right; rectPoints[2].Y = rect.Bottom;
            rectPoints[3].X = rect.Left; rectPoints[3].Y = rect.Bottom;

            Transform(rectPoints);

            return rectPoints;
        }
        /// <summary>
        /// Transforms the <paramref name="rect"/> to another rectangle
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public Rect TransformRectToRect(Rect rect)
        {
            var rectPoints = TransformRectToPoints(rect);
            var transRect = GetBoundingRect(rectPoints);

            return transRect;
        }
        /// <summary>
        /// Transforms the <paramref name="size"/> to a rectangle
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public Rect TransformToBoundingRect(Size size)
        {
            return TransformToBoundingRect(new Rect(new Point(), size));
        }
        /// <summary>
        /// Same as TransformRectToRect
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public Rect TransformToBoundingRect(Rect rect)
        {
            var rectPoints = TransformRectToPoints(rect);
            return GetBoundingRect(rectPoints);
        }
        /// <summary>
        /// Returns the bounding rectangle of an array of <paramref name="points"/>
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public Rect GetBoundingRect(Point[] points)
        {
            const double BIG = double.MaxValue;
            double minX = BIG, minY = BIG, maxX = -BIG, maxY = -BIG;

            // Iterate through the point array
            for (var i = 1; i < points.Length; i++)
            {
                var prevPoint = points[i - 1];

                // Record minimum X
                minX = Math.Min(minX, Math.Min(prevPoint.X, points[i].X));
                // Record minimum Y
                minY = Math.Min(minY, Math.Min(prevPoint.Y, points[i].Y));
                // Record maximum X
                maxX = Math.Max(maxX, Math.Max(prevPoint.X, points[i].X));
                // Record maximum Y
                maxY = Math.Max(maxY, Math.Max(prevPoint.Y, points[i].Y));
            }
            // Return the bounding rectangle
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
        /// <summary>
        /// Multiplies <paramref name="transform"/> to this matrix
        /// </summary>
        /// <param name="transform"></param>
        public void Concatenate(Matrix2D transform)
        {
            _matrix.Append(transform._matrix);
        }
        public double[] Array
        {
            get => new[] { _matrix.M11, _matrix.M12, _matrix.M21, _matrix.M22, _matrix.OffsetX, _matrix.OffsetY };
            set { _matrix.M11 = value[0]; _matrix.M12 = value[1]; _matrix.M21 = value[2]; _matrix.M22 = value[3]; _matrix.OffsetX = value[4]; _matrix.OffsetY = value[5]; }
        }
        #endregion Operations

        public string GetHashKey()
        {
            const double valMult = 100000.0;

            var m11 = (int)(_matrix.M11 * valMult);
            var m12 = (int)(_matrix.M12 * valMult);
            var m21 = (int)(_matrix.M21 * valMult);
            var m22 = (int)(_matrix.M22 * valMult);

            return $"{m11}{m12}{m21}{m22}";
        }
    }
}
