# Kitawo — movimentação aérea

Entrega das alterações de movimentação horizontal no ar para revisão e integração pelo desenvolvedor responsável.

## Origem e escopo

O repositório original não estava disponível. Estes arquivos foram recuperados com ILSpy 11 de `Kitawo.Core.dll`, da distribuição `kitawo-win-x64`, e posteriormente alterados. Portanto, são C# reconstruído: organização, formatação e algumas expressões podem diferir do código-fonte original.

Este repositório não é um jogo completo nem um projeto compilável isoladamente. Não contém executáveis, DLLs, recursos do jogo, dependências, configurações locais do VS Code ou o projeto gerado pela descompilação. Não tem histórico Git compartilhado com o projeto original: não mesclar esta branch diretamente naquele repositório nem substituir arquivos originais sem revisão.

## Arquivos entregues

Na branch `feature/air-movement`:

| Arquivo | Alteração |
| --- | --- |
| `Kitawo.Core/Player.cs` | Separa `Walking` de `Airborne`; adiciona `ApplyAirMovement`; mantém resistência aérea quando um ataque ocorre fora do chão. |
| `Kitawo.Core/PlayerTuning.cs` | Adiciona três propriedades independentes para o movimento aéreo. |
| `Kitawo.Core.Screens/DebugOverlayImGui.cs` | Adiciona três controles ao painel F2 e remove o tamanho fixo da lista de controles. |

## Comportamento implementado

Antes, chão e ar chamavam `ApplyHorizontalMovement`, usando `GroundAcceleration` e `GroundDeceleration`.

Agora:

- Com comando na direção da velocidade horizontal, aplica `AirAcceleration` até a velocidade desejada, sem reduzir uma velocidade que já esteja acima dela.
- Com comando contrário à velocidade horizontal, aplica `AirDeceleration` em direção a zero. Se o comando continuar, depois de parar o personagem pode acelerar na direção contrária em outra atualização.
- Aplica `AirResistance` em direção a zero em toda atualização de movimento aéreo, com ou sem comando. A resistência sozinha nunca inverte a direção.
- Em repouso horizontal, uma direção pressionada inicia a aceleração.
- As mudanças de velocidade são multiplicadas por `dt`.
- O movimento no chão, a altura do salto e a gravidade mantêm a implementação anterior.
- O estado `Attacking` tem prioridade na resolução dos estados. Nesse caso, fora do chão ou subindo, chama o movimento aéreo com comando zero, preservando o bloqueio de controle durante ataques e aplicando resistência.

“Para frente” e “para trás” foram interpretados em relação à velocidade horizontal, e não à orientação visual do personagem. Essa interpretação deve ser validada pelo responsável pelo design.

## Valores iniciais no código

| Propriedade | Valor | Unidade |
| --- | ---: | --- |
| `AirAcceleration` | 1800 | px/s² |
| `AirDeceleration` | 2200 | px/s² |
| `AirResistance` | 150 | px/s² |

O painel F2 permite ajustar cada valor de 0 a 5000. Alterações feitas durante a execução não são persistidas no código. A resistência é aplicada após a aceleração/frenagem e também atua enquanto uma direção é pressionada; portanto, reduz o ganho líquido de velocidade. Os valores iniciais ainda precisam de ajuste de sensação pelo desenvolvedor/design.

## Como baixar a branch

É necessário estar autenticado em uma conta com acesso ao repositório privado.

```bash
git clone --branch feature/air-movement --single-branch https://github.com/Correiaglm/kitawo-air-movement.git
cd kitawo-air-movement
```

## Como integrar ao projeto original

1. No repositório original, parta de um estado de trabalho limpo e crie uma branch própria, por exemplo `feature/air-movement`.
2. Compare os arquivos desta entrega com o código original. A descompilação separou tipos que podem estar juntos no `Player.cs` original.
3. Transfira as três propriedades para `PlayerTuning`, a função `ApplyAirMovement` e as mudanças no `switch` de estados para `Player`.
4. Transfira os três controles do painel de movimento e deixe o tamanho do array ser inferido.
5. Revise `PlayerTuningSnapshot`, persistência e consumidores de tuning no projeto original. Nesta entrega, `Snapshot()` mantém os campos antigos: o consumidor encontrado reconstruía o cenário de teste; as novas propriedades são lidas diretamente pelo movimento e painel. Não foi adicionada persistência dos parâmetros.
6. Compile e execute o projeto original; valide os cenários abaixo antes de fazer commit e abrir o PR de integração.

## Validação realizada

- O projeto completo recuperado compilou com zero erros, usando .NET SDK 10.0.401.
- Para compilar a recuperação local, foi necessário adicionar a referência a `GumCommon` e habilitar anotações nullable no `.csproj`. Essas configurações locais não fazem parte da entrega.
- Permaneceram dois avisos no código recuperado: uso de uma sobrecarga obsoleta de `DrawIndexedPrimitives` e evento `AttackHitbox2D.Hit` não utilizado.
- A DLL compilada foi carregada numa cópia separada do jogo.
- O usuário confirmou visualmente a diferença de resistência ao comparar 0 e 2000 no painel.
- Não há confirmação de teste completo de aceleração, frenagem, ataque em queda ou independência da taxa de quadros. Não foram criados testes automatizados nesta entrega.

## Testes de aceitação sugeridos

Controles encontrados: A/D ou setas para mover, Espaço para pular e F2 para o painel.

1. **Aceleração:** pular parado e pressionar uma direção no ar; comparar 300 e 1800 com a mesma resistência.
2. **Frenagem:** pular andando para a direita e pressionar esquerda; comparar 300 e 2200, repetir na direção oposta e manter o comando até inverter.
3. **Resistência:** pular em movimento e soltar a direção; comparar 0 e 2000. Sem comando, a resistência não deve produzir movimento inverso.
4. **Chão:** caminhar, correr e frear; confirmar que o comportamento anterior foi preservado.
5. **Ataque:** sair de uma plataforma durante um ataque e confirmar a aplicação da resistência.
6. **Limites:** valores zero, velocidades acima do limite e diferentes taxas de quadros.

Os valores 300, 800 e 2000 sugeridos durante a demonstração não foram adotados como padrões permanentes.
