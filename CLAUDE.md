# GameIdle — leia antes de fazer qualquer coisa

**Esta `main` está abandonada.** Tem só o esqueleto inicial do projeto (abril),
sem a arte, o combate, a loja de gemas, conquistas e tudo que foi construído
depois. Não analise nem commite código aqui.

**O trabalho de verdade está na branch `claude/affectionate-hamilton-vJ6V4`.**
Troque para ela antes de fazer qualquer análise ou alteração:

```
git fetch origin claude/affectionate-hamilton-vJ6V4
git checkout claude/affectionate-hamilton-vJ6V4
```

Essa branch tem seu próprio `CLAUDE.md` na raiz com o contexto completo do
projeto (estrutura, convenções, armadilhas conhecidas). Leia-o assim que
trocar de branch.

Se essa branch não existir mais (foi mergeada ou renomeada), rode
`git for-each-ref --sort=-committerdate refs/remotes/ --format='%(committerdate:iso8601) %(refname:short)'`
para achar a branch `claude/*` com o commit mais recente antes de assumir que
a `main` é o lugar certo pra trabalhar.
