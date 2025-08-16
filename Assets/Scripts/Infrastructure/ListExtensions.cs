using System.Collections.Generic;
using UnityEngine;
using FusionTask.Gameplay;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Extension methods for Lists.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Extension method to get the center point of a list of items that implement IPositionable.
        /// Call example:
        /// Vector3 centerPoint = selectedUnits.GetCenter();
        /// </summary>
        /// <param name="units"></param>
        /// <returns>Returns the center of the group of units</returns>
        public static Vector3 GetCenter<T>(this List<T> items) where T : IPositionable
        {
            if (items == null || items.Count == 0)
            {
                return Vector3.zero;
            }

            var center = Vector3.zero;
            foreach (var item in items)
            {
                if (item != null)
                {
                    center += item.Position;
                }
            }
            center /= items.Count;
            return center;
        }
    }
}
