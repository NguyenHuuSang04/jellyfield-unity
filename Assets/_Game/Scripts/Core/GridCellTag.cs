using UnityEngine;

namespace JellyField.Core
{
    public class GridCellTag : MonoBehaviour
    {
        [SerializeField] private Vector2Int coord;

        public Vector2Int Coord
        {
            get => coord;
            set => coord = value;
        }
    }
}