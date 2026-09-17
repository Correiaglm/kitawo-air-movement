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
- Com comando contrário à velocidade horizontal, aplica `AirDeceleration` junto com a resistência em direção a zero. Se parar antes do fim da atualização, usa o tempo restante para acelerar na direção do comando.
- Aplica `AirResistance` em direção a zero em toda atualização de movimento aéreo, com ou sem comando. A resistência sozinha nunca inverte a direção.
- Em repouso horizontal, uma direção pressionada inicia a aceleração.
- Integra a velocidade usando `dt`, descontando o tempo gasto para parar ou desacelerar de uma velocidade acima do limite. Aceleração e resistência são combinadas antes de aplicar o limite de velocidade.
- O movimento no chão, a altura do salto e a gravidade mantêm a implementação anterior.
- O estado `Attacking` tem prioridade na resolução dos estados. Nesse caso, fora do chão ou subindo, chama o movimento aéreo com comando zero, preservando o bloqueio de controle durante ataques e aplicando resistência.

“Para frente” e “para trás” foram interpretados em relação à velocidade horizontal, e não à orientação visual do personagem. Essa interpretação deve ser validada pelo responsável pelo design.

## Valores iniciais no código

| Propriedade | Valor | Unidade |
| --- | ---: | --- |
| `AirAcceleration` | 1800 | px/s² |
| `AirDeceleration` | 2200 | px/s² |
| `AirResistance` | 150 | px/s² |

O painel F2 permite ajustar cada valor de 0 a 5000. Alterações feitas durante a execução não são persistidas no código. A resistência também atua enquanto uma direção é pressionada: o ganho líquido é `AirAcceleration - AirResistance`, e a frenagem total é `AirDeceleration + AirResistance`. Resistência maior que a aceleração pode impedir a saída do repouso e reduzir a velocidade mesmo com comando. Os parâmetros negativos de aceleração, frenagem e resistência são tratados como zero. Os valores iniciais ainda precisam de ajuste de sensação pelo desenvolvedor/design.

## Como baixar a branch

O repositório é público e pode ser clonado sem convite.

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
- O usuário confirmou visualmente a diferença de resistência ao comparar 0 e 2000 no painel na primeira versão. A revisão atual acrescenta a correção de integração descrita abaixo.
- **251 verificações isoladas aprovadas**, chamando os métodos reais da DLL por reflexão: aceleração nas duas direções, limites de caminhada/corrida, ausência de comando, frenagem, parada, inversão usando o tempo restante, resistência com e sem comando, parâmetros zero, velocidade acima do limite, `dt` zero e preservação dos parâmetros do chão. A velocidade vertical permaneceu intacta em todas as chamadas.
- A matriz de velocidade compara passos de 30, 60, 120 e 240 FPS, seis velocidades iniciais, três comandos e três conjuntos de parâmetros. Inclui resistência superior à aceleração e parâmetros zerados; compara um segundo em vários passos com um passo de um segundo, com tolerância de 0,02 px/s. Testes abaixo do limite também passam com tolerância de 0,005 px/s.
- **64 verificações de integração aprovadas**, com instâncias reais de `Player`, `CollisionWorld2D`, `EcbController2D`, `AttackComponent2D` e painel: estados Idle/Walking/Airborne/Attacking; corrida; salto e aterrissagem em quatro taxas de atualização; frenagem em queda; ataque iniciado no chão seguido de saída de plataforma; preservação do bloqueio de comando durante ataque; colisão com parede e teto; descida através de plataforma one-way; leitura/escrita e limites dos três controles de tuning.
- Os cenários de integração executam `Player.Update` e colisões sem janela ou renderização. Não avaliam aparência, animações, áudio ou sensação subjetiva dos valores.
- Os verificadores e relatórios ficam locais, fora deste repositório, para preservar a entrega de apenas três arquivos de código e este README. Não há CI configurada neste repositório parcial; o projeto original deve incorporar testes equivalentes.
- DLL testada (SHA-256): `4169377EC2E16CF49237F93C81D6CC299D70EDC3691D5CF9DB1C0F36B788BBA1`.
- A cópia do jogo com essa DLL iniciou, criou a janela e permaneceu respondendo após cinco segundos, sem saída de erro no teste de inicialização. Isso não substitui revisão visual prolongada.

### Correção da variação de velocidade com a taxa de quadros

Na primeira versão, aplicar a resistência depois de limitar a aceleração provocava perda extra a cada quadro. A implementação atual combina as taxas antes de limitar a velocidade, e considera o tempo exato gasto para parar ou retornar ao limite. Com os padrões, após dois segundos de comando contínuo:

| Taxa de atualização | Primeira versão | Versão atual |
| --- | ---: | ---: |
| 30 FPS | 235 px/s | 240 px/s |
| 60 FPS | 237,5 px/s | 240 px/s |
| 120 FPS | 238,75 px/s | 240 px/s |
| 240 FPS | 239,375 px/s | 240 px/s |

Essa correção foi validada para velocidade horizontal com comando e parâmetros constantes entre atualizações. Não significa independência completa de FPS para todo o jogo: o controlador de colisões continua integrando deslocamento com a velocidade do quadro, e ataques usam duração em quadros. Nenhuma dessas estruturas preexistentes foi modificada. A sensação final de controle e a integração no código-fonte original continuam a cargo da revisão do desenvolvedor responsável.

## Testes de aceitação sugeridos

Controles encontrados: A/D ou setas para mover, Espaço para pular e F2 para o painel.

1. **Aceleração:** pular parado e pressionar uma direção no ar; comparar 300 e 1800 com a mesma resistência.
2. **Frenagem:** pular andando para a direita e pressionar esquerda; comparar 300 e 2200, repetir na direção oposta e manter o comando até inverter.
3. **Resistência:** pular em movimento e soltar a direção; comparar 0 e 2000. Sem comando, a resistência não deve produzir movimento inverso.
4. **Chão:** caminhar, correr e frear; confirmar que o comportamento anterior foi preservado.
5. **Ataque:** sair de uma plataforma durante um ataque e confirmar a aplicação da resistência.
6. **Limites:** valores zero, velocidades acima do limite e diferentes taxas de quadros.

Os valores 300, 800 e 2000 sugeridos durante a demonstração não foram adotados como padrões permanentes.
