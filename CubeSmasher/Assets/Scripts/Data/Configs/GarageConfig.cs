using UnityEngine;

namespace CubeSmasher.Data.Configs
{
    /// <summary>
    /// Состав гаража: какие машины открываются на каких стадиях
    /// и сколько пассивного дохода каждая приносит.
    /// </summary>
    [CreateAssetMenu(fileName = "GarageConfig", menuName = "CubeSmasher/Garage Config")]
    public class GarageConfig : ScriptableObject
    {
        [Header("Машины гаража")]
        [Tooltip("Порядок важен: индекс машины используется как её идентификатор")]
        public GarageCarData[] GarageCars;
    }
}
