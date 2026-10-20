using UnityEngine;

namespace ReactorDeCaos
{
    [CreateAssetMenu(menuName = "Reactor de Caos/Economy Settings")]
    public sealed class EconomySettings : ScriptableObject
    {
        public EconomyConfig values = new EconomyConfig();
    }
}
