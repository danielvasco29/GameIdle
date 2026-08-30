# GameIdle — Idle Startup Tycoon

Jogo idle em Unity. O jogador contrata uma equipe de startup (Developer, Designer,
Manager, Marketing, CTO, CEO, Investor, AI Engineer), cada um gerando renda por
segundo, com prestígio, loja de gemas, combate em arena, conquistas e missões.

Arte em pixel art isométrica, escritório visto de cima. Alvo: Android, orientação
paisagem travada.

---

## REGRA 1 — Branch

**Trabalhe sempre em `claude/affectionate-hamilton-vJ6V4`.**

Nunca crie branch nova por sessão. O repositório já tem 8 branches `claude/*`
órfãs porque sessões anteriores fizeram isso, e o trabalho ficou espalhado.

A `main` está **abandonada** (3 commits, esqueleto de abril, sem arte). Não
analise nem commite nela. As branches `gpt-work` e `gpt-import` já foram
mergeadas na branch de trabalho — ignore-as.

Antes de começar: `git fetch origin && git status`. Outras sessões podem ter
empurrado commits direto pro GitHub sem passar pela máquina local.

## REGRA 2 — Antes de afirmar que algo é bug

Leia o tipo declarado antes de concluir. Já houve diagnóstico errado por assumir
que `MonsterDef` era classe quando é `struct` — struct nunca é null, os campos
vêm zerados e não lançam NullReferenceException.

Não existe Unity no ambiente do agente. Nenhuma alteração é validada por
compilação — quem julga é o Console do Unity, na máquina do usuário. Diga isso
em vez de afirmar que o código está correto.

## REGRA 3 — Sobre o usuário

Não é desenvolvedor. É analista de infraestrutura sênior: entende terminal,
PowerShell e git operacional, mas não quer discutir arquitetura de C#.

Entregue comandos prontos para colar e explique o que mudou em português claro.
Evite jargão de gamedev sem explicar.

---

## Estrutura

```
Assets/Scripts/Core/     GameManager, CharacterManager, CombatManager,
                         GemShop, SaveSystem, OfflineProgress,
                         AchievementManager, DailyMissionSystem,
                         GameEventSystem, MonetizationManager, SoundManager
Assets/Scripts/Data/     CharacterData, EventData, SaveData, EventEffect,
                         CharacterInstance  (ScriptableObjects + modelos)
Assets/Scripts/UI/       UIManager (~2500 linhas, constrói a UI por código),
                         painéis, botões, camadas de FX
Assets/Scripts/Utilities/ NumberFormatter, UiSpriteFactory, SpriteBackgroundRemover
Assets/Resources/        Characters/, Monsters/, Props/, Backgrounds/, Icons/, FX/
Assets/Scenes/           MainScene.unity  (cena única)
```

Namespace único: `GameIdle`. Uma cena só. uGUI + TextMeshPro, sem render
pipeline custom.

## Convenções

- Singletons via `Instance` estático, com ordem explícita em
  `[DefaultExecutionOrder]`: GameManager `-100`, CharacterManager `-90`,
  GameEventSystem `-80`, MonetizationManager `-70`. **CombatManager não tem
  ordem** (roda em 0), então seu `Start` acontece depois do GameManager.
- Personagens e eventos são ScriptableObjects em `Resources/`, carregados por
  `Resources.LoadAll`.
- UI desacoplada por eventos C#: `OnMoneyChanged`, `OnStatsUpdated`, `OnTap`,
  `OnHire`, `OnPrestige`, `OnKill`.
- Save por `characterId`, nunca por índice.
- Números grandes sempre via `NumberFormatter.Format` (K, M, B, T...).
- Comentários em português, sem acento nos comentários de código novo.

## Armadilhas conhecidas

**Dois eventos diferentes.** `OnMoneyChanged` dispara quando o dinheiro muda;
`OnStatsUpdated` só em recálculo (contratar, efeito de evento, compra de gema,
prestígio). Qualquer UI que dependa do *saldo* precisa do tick periódico do
UIManager, não de `OnStatsUpdated` — foi assim que a barra de prestígio ficou
congelada.

**Prestígio compara com o SALDO**, não com o total ganho:
`CanPrestige() => Money >= GetPrestigeRequirement()`. A meta cresce 5x por
prestígio. O valor base vem do Inspector da cena, não do padrão do código.

**Custo por frame.** `Update` do GameManager credita renda todo frame. Não
adicione trabalho pesado nesse caminho — renda passiva usa `AddMoneyPassive`,
que não dispara evento a cada frame.

**Save.** JSON em `persistentDataPath` mais backup em PlayerPrefs.
`PlayerPrefs.Save()` trava a main thread — só no `flushPrefs` (pause/quit).
Autosave a cada 30s. Upgrades de combate migraram de PlayerPrefs para o JSON,
com fallback de leitura para saves antigos.

**`.utmp/`** é cache de build Android. Está no `.gitignore`; nunca versione.

## Git

Commits em português, imperativo, uma linha descrevendo o efeito:
`Corrige X`, `Ajusta Y`, `Adiciona Z`.

Nunca use `--force`. Se o push for rejeitado, `git pull --rebase`.
