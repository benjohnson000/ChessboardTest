using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace benjohnson
{
    public static class Utilities
    {
        public static class Input
        {
            public static Vector2 MouseScreenPosition()
            {
                return Mouse.current.position.ReadValue();
            }

            public static Vector2 MouseWorldPosition()
            {
                return Camera.main.ScreenToWorldPoint(MouseScreenPosition());
            }
        }

        public static class Random
        {
            private static readonly System.Random _random = new System.Random();

            public static T RandomEnumValue<T>() where T : Enum
            {
                Array values = Enum.GetValues(typeof(T));
                int randomIndex = _random.Next(values.Length);
                return (T)values.GetValue(randomIndex);
            }
        }
    }
}