using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RIKA_TEXTURER
{
    public class TriangleInterpolator
    {
        private readonly float _aX, _aY, _valueA;
        private readonly float _bX, _bY, _valueB;
        private readonly float _cX, _cY, _valueC;
        private readonly float _detInv;
        private readonly float _vA, _vB;

        public TriangleInterpolator(Vector2 a, float valueA,
                                    Vector2 b, float valueB,
                                    Vector2 c, float valueC)
        {
            _aX = a.X; _aY = a.Y; _valueA = valueA;
            _bX = b.X; _bY = b.Y; _valueB = valueB;
            _cX = c.X; _cY = c.Y; _valueC = valueC;

            float det = (_bY - _cY) * (_aX - _cX) + (_cX - _bX) * (_aY - _cY);
            _detInv = 1f / det;

            _vA = _valueA - _valueC;
            _vB = _valueB - _valueC;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Interpolate(float x, float y)
        {
            float u = ((_bY - _cY) * (x - _cX) + (_cX - _bX) * (y - _cY)) * _detInv;
            float v = ((_cY - _aY) * (x - _cX) + (_aX - _cX) * (y - _cY)) * _detInv;
            float w = 1f - u - v;

            return u * _valueA + v * _valueB + w * _valueC;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float InterpolateOptimized(float x, float y)
        {
            float u = ((_bY - _cY) * (x - _cX) + (_cX - _bX) * (y - _cY)) * _detInv;
            float v = ((_cY - _aY) * (x - _cX) + (_aX - _cX) * (y - _cY)) * _detInv;

            // value = u*(vA-vC) + v*(vB-vC) + vC
            return _valueC + u * _vA + v * _vB;
        }
    }
}
