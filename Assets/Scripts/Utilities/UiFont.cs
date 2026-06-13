using TMPro;
using UnityEngine;

namespace GameIdle
{
    // Fonte central de toda a UI. Permite trocar a familia da fonte em um unico
    // ponto: basta gerar um TMP Font Asset (SDF) chamado "GameFont SDF" e colocar
    // em Assets/Resources/Fonts/ — todos os menus passam a usa-lo automaticamente.
    // Se nao existir, cai na fonte default do TextMeshPro (comportamento antigo).
    public static class UiFont
    {
        private const string CustomFontPath = "Fonts/GameFont SDF";
        private static TMP_FontAsset _cached;
        private static bool _resolved;

        public static TMP_FontAsset Get()
        {
            if (_resolved) return _cached;
            _resolved = true;
            _cached = Resources.Load<TMP_FontAsset>(CustomFontPath);
            if (_cached == null) _cached = TMP_Settings.defaultFontAsset;
            return _cached;
        }
    }
}
