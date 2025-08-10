using UnityEngine;

/*
Copyright 2016 Max Kaufmann (max.kaufmann@gmail.com)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

// This file has been modified slightly, original: https://gist.github.com/maxattack/4c7b4de00f5c1b95a33b

namespace GooeyArtifacts.ThirdParty.Utils
{
    public static class QuaternionUtil
    {
        public static Quaternion AngularVelocityToDerivative(Quaternion current, Vector3 angularVelocity)
        {
            Quaternion spin = new Quaternion(angularVelocity.x, angularVelocity.y, angularVelocity.z, 0f);
            Quaternion result = spin * current;
            return new Quaternion(0.5f * result.x, 0.5f * result.y, 0.5f * result.z, 0.5f * result.w);
        }

        public static Vector3 DerivativeToAngularVelocity(Quaternion current, Quaternion derivative)
        {
            Quaternion result = derivative * Quaternion.Inverse(current);
            return new Vector3(2f * result.x, 2f * result.y, 2f * result.z);
        }

        public static Quaternion IntegrateRotation(Quaternion rotation, Vector3 angularVelocity, float deltaTime)
        {
            if (deltaTime < Mathf.Epsilon)
                return rotation;

            Quaternion derivative = AngularVelocityToDerivative(rotation, angularVelocity);

            Vector4 pred = new Vector4(rotation.x + (derivative.x * deltaTime),
                                       rotation.y + (derivative.y * deltaTime),
                                       rotation.z + (derivative.z * deltaTime),
                                       rotation.w + (derivative.w * deltaTime)).normalized;

            return new Quaternion(pred.x, pred.y, pred.z, pred.w);
        }

        public static Quaternion SmoothDamp(Quaternion current, Quaternion target, ref Vector4 velocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            if (deltaTime < Mathf.Epsilon)
                return current;

            // account for double-cover
            float dot = Quaternion.Dot(current, target);

            float multi = dot > 0f ? 1f : -1f;
            target.x *= multi;
            target.y *= multi;
            target.z *= multi;
            target.w *= multi;

            // smooth damp (nlerp approx)
            Vector4 result = new Vector4(Mathf.SmoothDamp(current.x, target.x, ref velocity.x, smoothTime, maxSpeed, deltaTime),
                                         Mathf.SmoothDamp(current.y, target.y, ref velocity.y, smoothTime, maxSpeed, deltaTime),
                                         Mathf.SmoothDamp(current.z, target.z, ref velocity.z, smoothTime, maxSpeed, deltaTime),
                                         Mathf.SmoothDamp(current.w, target.w, ref velocity.w, smoothTime, maxSpeed, deltaTime)).normalized;

            // ensure deriv is tangent
            Vector4 derivativeError = Vector4.Project(new Vector4(velocity.x, velocity.y, velocity.z, velocity.w), result);
            velocity.x -= derivativeError.x;
            velocity.y -= derivativeError.y;
            velocity.z -= derivativeError.z;
            velocity.w -= derivativeError.w;

            return new Quaternion(result.x, result.y, result.z, result.w);
        }
    }
}