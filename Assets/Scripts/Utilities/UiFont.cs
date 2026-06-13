using TMPro;
using UnityEngine;

namespace GameIdle
{
    // Fonte central de toda a UI. Permite trocar a familia da fonte em um unico
    // ponto: basta gerar um TMP Font Asset (SDF) chamado "GameFont SDF" (ou
    // "GameFont_SDF") e colocar em Assets/Resources/Fonts/ — todos os menus
    // passam a usa-lo automaticamente. Se nao existir, cai na fonte default.
    public static class UiFont
    {
        // Aceita variacoes de nome do arquivo gerado (espaco/underline).
        private static readonly string[] CustomFontPaths =
        {
            "Fonts/GameFont SDF",
            "Fonts/GameFont_SDF",
            "Fonts/GameFont",
        };
        private static TMP_FontAsset _cached;
        private static bool _resolved;

        public static TMP_FontAsset Get()
        {
            if (_resolved && _cached != null) return _cached;
            _resolved = true;

            // Fonte de referencia que JA funciona (default do TMP ou a da cena),
            // usada para emprestar o shader correto à fonte custom.
            TMP_FontAsset reference = TMP_Settings.defaultFontAsset;
            if (reference == null)
            {
                var existing = Object.FindAnyObjectByType<TextMeshProUGUI>();
                if (existing != null) reference = existing.font;
            }

            // 1) Fonte custom gerada (se existir), tentando as variacoes de nome
            TMP_FontAsset custom = null;
            foreach (var p in CustomFontPaths)
            {
                custom = Resources.Load<TMP_FontAsset>(p);
                if (custom != null) break;
            }

            if (custom != null)
            {
                // O material gerado pode vir com um shader que nao renderiza o SDF
                // neste projeto (texto em quadrados). Empresta o shader da fonte de
                // referencia, que comprovadamente funciona.
                if (reference != null && reference.material != null
                    && reference.material.shader != null
                    && custom.material != null && reference != custom)
                {
                    custom.material.shader = reference.material.shader;
                }
                _cached = custom;
                return _cached;
            }

            // 2) Sem fonte custom: usa a de referencia (comportamento antigo)
            _cached = reference;
            return _cached;
        }
    }
}
